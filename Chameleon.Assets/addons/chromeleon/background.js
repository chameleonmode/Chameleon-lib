import app, { noises } from "./src/app.js";
import { log } from "./src/services/logger.js";
import { addUrlsAsBookmarks } from "./src/services/bookmarks.js";
import "./src/services/webrtc.js";
import "./src/services/debugger.js";

// Startup
const proxio = (port = 33333) =>
  chrome.proxy.settings.set({
    value: {
      mode: "fixed_servers",
      rules: { singleProxy: { scheme: "http", host: "127.0.0.1", port } },
    },
  });

const startup = async () => {
  const { config = {}, noise, hash } = await chrome.storage.local.get(["config", "noise", "hash"]);
  for (const [key, value] of Object.entries(config)) {
    app.config[key] =
      typeof value === "object" && !Array.isArray(value) ? { ...app.config[key], ...value } : value;
  }
  if (!noise || !hash) {
    app.config.noise = noises[Math.floor(Math.random() * noises.length)];
    app.config.hash = Math.random() * (100 - 1.5) + 1.5; // Random number between 1.5 and 100
    await chrome.storage.local.set({ config: app.config });
    await chrome.storage.local.set({ noise: app.config.noise, hash: app.config.hash });
  } else {
    app.config.noise = noise;
    app.config.hash = hash;
  }
};
const on = async () => {
  // Reset the state
  app.state.loaded = false;
  await new Promise((resolve) => setTimeout(resolve, 300)); // Wait for 0.3 second
  const manifestResponse = await fetch(chrome.runtime.getURL('manifest.json'));
  const manifestData = await manifestResponse.json();
  const newVersion = manifestData.version;
  const { currentVersion } = await chrome.storage.local.get(["currentVersion"]);
  if (currentVersion && newVersion !== currentVersion) {
     await chrome.storage.local.set({ currentVersion: newVersion });
     chrome.runtime.reload();
     return;
  }

  // First load the config from sync storage
  const { config = {} } = await chrome.storage.sync.get(["config"]);
  app.config = { ...app.config, ...config };

  // Common startup operations
  await startup();
  if (config) await chrome.storage.sync.set({ config: app.config });

  await app.discoverServer();
  const tabs = await chrome.tabs.query({});
  let found = false;
  for (const tab of tabs) {
    if (tab.url.startsWith("http://127.0.0.1") === false) {
      await chrome.tabs.remove(tab.id);
      continue;
    }
    if (found) {
      await chrome.tabs.remove(tab.id);
      continue;
    }

    const url = new URL(tab.url);
    const sessionId = url.searchParams.get("sessionId");
    const instanceId = url.searchParams.get("instanceId");
    const proxioPort = Number(url.searchParams.get("proxio"));
    if (!proxioPort) {
      await chrome.tabs.remove(tab.id);
      continue;
    } 
    proxio(proxioPort);
    
    const init = await app.sendData({ type: "init" }, { instanceId, sessionId });
    if (!init || !init.config || !init.port) {
      await chrome.tabs.remove(tab.id);
      continue;
    }
    app.session = { sessionId, instanceId };
    app.state.port = init.port;
    app.state.tabId = tab.id;

    for (const [key, value] of Object.entries(init.config)) {
      app.config[key] =
        app.config.sync || key === "proxy"
          ? { ...app.config[key], ...value }
          : { ...value, ...app.config[key] };
    }

    await chrome.storage.local.set({ session: app.session, config: app.config });
    await addUrlsAsBookmarks(app.name, app.config.urls.homePages);
    await chrome.tabs.update(tab.id, { url: app.config.urls.start });
    found = true;
  }
  await new Promise((resolve) => setTimeout(resolve, 300)); // Wait for .3 second before removing tabs
  app.state.loaded = true;
  log.info("started successfully");
  chrome.tabs.onUpdated.addListener((_, __, tab) => {
    const remove =
      app.state.loaded === false ||
      app.state.tabId === tab.id ||
      tab.url.startsWith("http://127.0.0.1") === false;
    if (remove) return;
    else chrome.tabs.remove(tab.id);
  });
};

chrome.runtime.onInstalled.addListener(() => {
  log.info("installed or updated");
  on();
});
chrome.runtime.onStartup.addListener(() => {
  log.info("startup");
  on();
});
// Listen for messages from popup or content scripts
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  log.info("Received message:", message);
  switch (message.action) {
    case "getConfig":
      sendResponse({ success: true, config: app.config });
      break;
    case "updateConfig":
      app.config = { ...app.config, ...message.config };
      app.notify("configUpdated");

      // Save the updated config to both local and sync storage in parallel
      Promise.all([
        chrome.storage.local.set({ config: app.config }),
        chrome.storage.sync.set({ config: app.config }),
      ])
        .then(() => {
          log.info("Config saved to both local and sync storage");
          sendResponse({ success: true });
        })
        .catch((error) => {
          log.error("Error saving config to storage", error);
          sendResponse({ success: false });
        });
      break;
    default:
      log.warn("Unknown message action:", message.action);
      sendResponse({ success: false, error: "Unknown message type" });
      break;
  }

  // Return true to indicate that sendResponse will be called asynchronously and keep channel open
  return true;
});
