import app, { noises } from "./src/app.js";
import { log } from "./src/services/logger.js";
import { addUrlsAsBookmarks } from "./src/services/bookmarks.js";
import "./src/services/executions.js";

const proxio = async ({
  scheme = "http",
  host = "127.0.0.1",
  port = 33333,
  username = null,
  password = null,
} = {}) => {
  if (app.config.proxy.enabled) {
    if (username && password)
      chrome.webRequest.onAuthRequired.addListener(
        (_) => ({ authCredentials: { username, password } }),
        { urls: ["<all_urls>"] },
        ["blocking"]
      );
    await browser.proxy.onRequest.addListener((_) => ({ type: scheme, host, port, username, password }), {
      urls: ["<all_urls>"],
    });
  } else {
    await browser.proxy.onRequest.addListener((_) => ({ type: "direct" }), { urls: ["<all_urls>"] });
  }
};
const checkForUpdates = async () => {
  const response = await fetch(chrome.runtime.getURL("manifest.json"));
  const data = await response.json();
  const { version } = await chrome.storage.local.get(["version"]);
  if (version === data.version) return; // No update needed
  log.info(`Extension updated from version ${version} to ${data.version}`);
  await chrome.storage.local.set({ version: data.version });
  chrome.runtime.reload();
  return true; // Indicate that an update was found
};

// Startup
const resetConfig = async () => {
  // Then load the config from local storage
  const { config: local = {}, noise, hash } = await chrome.storage.local.get(["config", "noise", "hash"]);
  app.config = { ...app.config, ...local };

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

const startup = async () => {
  if (app.name === "Chromeleon") {
    await checkForUpdates(); // Update check. Skip if using Firefox!
    // Discard all inactive tabs
    await chrome.tabs.query({ active: false }, async (tabs) => {
      for (let i = tabs.length - 1; i >= 0; i--) {
        await chrome.tabs.remove(tabs[i].id);
      }
    });
  }
  await resetConfig();
  await app.discoverServer();
  await new Promise((resolve) => setTimeout(resolve, 369)); // Wait for 0.369 seconds

  for (const tab of await chrome.tabs.query({ url: "http://127.0.0.1/*" })) {
    const url = new URL(tab.url);
    const sessionId = url.searchParams.get("sessionId");
    const instanceId = url.searchParams.get("instanceId");
    const { config, port } = await app.sendData({ type: "init" }, { instanceId, sessionId });
    if (!config || !port) continue;

    app.session = { sessionId, instanceId };
    app.state.port = port;
    app.state.tabId = tab.id;
    if (app.config.sync) {
      app.config = { ...app.config, ...config };
      await chrome.storage.local.set({ session: app.session, config: app.config });
    }
    app.config.proxy = config.proxy;
    await proxio({
      scheme: app.config.proxy.scheme,
      host: app.config.proxy.host,
      port: app.config.proxy.port,
      username: app.config.proxy.username,
      password: app.config.proxy.password,
    });
    await addUrlsAsBookmarks(app.name, config.urls.homePages);
    await chrome.tabs.update(tab.id, { url: config.urls.start });
  }
};

const onInstalledOrStartup = async (type = "onyx") => {
  log.info(`${type} event start`, app.state);
  app.state.loaded = false;
  await startup().catch((error) => {
    log.error("Error during startup:", error);
  });
  app.state.loaded = true;
  log.info(`${type} event end`, app.state);
};
chrome.runtime.onInstalled.addListener(() => onInstalledOrStartup("onInstalled"));
chrome.runtime.onStartup.addListener(() => onInstalledOrStartup("onStartup"));
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
      chrome.storage.local
        .set({ config: app.config })
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

chrome.tabs.onCreated.addListener(async (tab) => {
  if (tab.index === 0) chrome.tabs.update(tab.id, { url: "about:blank" });
  else if (tab.title.startsWith("127.0.0.1") && tab.title.endsWith("foreground"))
    chrome.tabs.remove(tab.id);
});
