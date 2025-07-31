import app, { noises } from "./src/app.js";
import { addBookmarks, proxio, log } from "./src/services/index.js";

const checkForUpdates = async () => {
  const response = await fetch(chrome.runtime.getURL("manifest.json"));
  const data = await response.json();
  const { version } = await chrome.storage.local.get(["version"]);
  if (version === data.version) return; // No update needed
  await chrome.storage.local.set({ version: data.version });
  return true; // Indicate that an update was found
};

// Startup
const resetConfig = async () => {
  // Discard all inactive tabs
  await chrome.tabs.query({ active: false }, async (tabs) => {
    if (tabs.length <= 1) return; // Skip if no inactive tabs
    for (const tab of tabs) {
      if (tab.url?.startsWith("http://127.0.0.1")) continue;
      await chrome.tabs.discard(tab.id).catch(() => false); // Discard inactive tabs
    }
  });

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
  await resetConfig();
  await app.discoverServer();
  while (!app.state.tabId) {
    await new Promise((resolve) => setTimeout(resolve, 369)); // Wait for 0.369 seconds
    await chrome.tabs.query({ }, async (tabs) => {
      for (let i = 0; i < tabs.length; i++) {
        const tab = tabs[i];
        log.info("Processing tab:", tab);
        if (!tab.url) break; // Skip if no URL or ID
        else if(!tab.url.startsWith("http://127.0.0.1")) continue; // Skip if not a local URL
        const url = new URL(tab.url);
        const sessionId = url.searchParams.get("sessionId");
        const instanceId = url.searchParams.get("instanceId");
        const { config, port } = await app
          .sendData({ type: "init" }, { instanceId, sessionId })
          .catch((error) => {
            log.error("Error sending data:", error);
            return {}; // Return an empty object on error
          });
        if (!config || !port) continue;

        app.session = { sessionId, instanceId };
        app.state.port = port;
        app.state.tabId = tab.id;
        if (app.config.sync) {
          app.config = { ...app.config, ...config };
          await chrome.storage.local.set({ session: app.session, config: app.config });
        }
        app.config.proxy = config.proxy;
        await proxio(app.config.proxy);
        await addBookmarks(app.name, app.config.urls.bookmarks);
        await chrome.tabs.update(tab.id, { url: app.config.urls.start });
        break; // Exit after processing the first active tab
      }
    });
  }
  
  await chrome.tabs.query({ active: false }, async (tabs) => {
    if (tabs.length <= 1) return; // Skip if no inactive tabs
    for (const tab of tabs) {
      if (tab.id === app.state.tabId || !tab.url?.startsWith("http://127.0.0.1")) continue;
      await chrome.tabs.remove(tab.id).catch(() => false);
    }
  });
};

const onInstalledOrStartup = async (type = "onyx") => {
  if(app.name === "Chromeleon" && await checkForUpdates()) chrome.runtime.reload();
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
