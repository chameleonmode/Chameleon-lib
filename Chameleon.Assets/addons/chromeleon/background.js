import app, { noises } from "./src/app.js";
import proxy from "./src/services/proxy.js";
import { log } from "./src/services/logger.js";
import { addUrlsAsBookmarks } from "./src/services/bookmarks.js";
import { checkForExtensionUpdate } from "./src/lib/util.js";
import "./src/services/webrtc.js";
import "./src/services/debugger.js";

// Startup
const startup = async () => {
  const { config = {} } = await chrome.storage.local.get(["config"]);
  for (const [key, value] of Object.entries(config)) {
    app.config[key] =
      typeof value === "object" && !Array.isArray(value) ? { ...app.config[key], ...value } : value;
  }

  const { noise, hash } = await chrome.storage.local.get(["noise", "hash"]);
  if (!noise || !hash) {
    app.config.noise = noises[Math.floor(Math.random() * noises.length)];
    app.config.hash = Math.random() * (100 - 1.5) + 1.5; // Random number between 1.5 and 100
    await chrome.storage.local.set({ config: app.config });
    const sync = await chrome.storage.sync.get(["config"]);
    if (sync.config) {
      sync.config.noise = app.config.noise;
      sync.config.hash = app.config.hash;
      await chrome.storage.sync.set({ config: sync.config });
    }
    await chrome.storage.local.set({ noise: app.config.noise, hash: app.config.hash });
  } else {
    app.config.noise = noise;
    app.config.hash = hash;
  }
};
const on = async () => {
  log.info("On installed or started");

  // Reset the state
  app.state.loaded = false;
  await new Promise((resolve) => setTimeout(resolve, 300)); // Wait for 0.3 second
  await checkForExtensionUpdate();

  // First load the config from sync storage
  const { config = {} } = await chrome.storage.sync.get(["config"]);
  app.config = { ...app.config, ...config };

  // Common startup operations
  await startup();
  await proxy(app.config.proxy);
  app.discoverServer().then(async () => {
    const tabs = await chrome.tabs.query({});
    for (const tab of tabs) {
      if (!tab.url.startsWith("http://127.0.0.1")) continue;

      const url = new URL(tab.url);
      const sessionId = url.searchParams.get("sessionId");
      const instanceId = url.searchParams.get("instanceId");
      const init = await app.sendData({ type: "init" }, { instanceId, sessionId });
      if (!init || !init.config || !init.port) {
        chrome.tabs.remove(tab.id);
        continue;
      }
      app.session = { sessionId, instanceId };
      app.state.port = init.port;

      for (const [key, value] of Object.entries(init.config)) {
        app.config[key] =
          app.config.sync || key === "proxy"
            ? { ...app.config[key], ...value }
            : { ...value, ...app.config[key] };
      }
      await proxy(app.config.proxy);

      await chrome.storage.local.set({ session: app.session, config: app.config });
      await addUrlsAsBookmarks("Chromeleon", app.config.urls.homePages);
      await chrome.tabs.update(tab.id, { url: app.config.urls.start });
      // @TODO: Get page content ?
      // const results = await chrome.scripting.executeScript({
      // 	target: { tabId: tab.id },
      // 	func: () => document.body.textContent,
      // });
      break;
    }

    app.state.loaded = true;
    log.info("Geckoleon started successfully");
  });
};

chrome.runtime.onInstalled.addListener(on);
chrome.runtime.onStartup.addListener(on);

chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
  if (
    app.server === null ||
    app.state.loaded !== true ||
    changeInfo.status !== "complete" ||
    tab.url.startsWith("http://127.0.0.1") === false
  )
    return;

  const tabs = await chrome.tabs.query({});
  if (tabs.length == 1) await chrome.tabs.update(tabId, { url: app.config.urls.start });
  else chrome.tabs.remove(tabId);
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
