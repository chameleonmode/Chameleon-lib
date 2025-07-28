import app, { noises } from "./src/app.js";
import { log } from "./src/services/logger.js";
import { addUrlsAsBookmarks } from "./src/services/bookmarks.js";
import "./src/services/executions.js";

// Update check. Skip if using Firefox!
const checkForUpdates = async () => {
  if (app.name === "Geckoleon") return; // Skip if using Geckoleon

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
  await new Promise((resolve) => setTimeout(resolve, 369)); // Wait for 0.369 seconds
  app.state.loaded = false;
  // Check for existing tabs
  for (const tab of await chrome.tabs.query({})) {
    if (new URL(tab.url).searchParams.has("proxio")) continue;
    else await chrome.tabs.remove(tab.id);
  }
  await checkForUpdates();
  await resetConfig();
  await app.discoverServer();

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
    await addUrlsAsBookmarks(app.name, config.urls.homePages);
    await chrome.tabs.update(tab.id, { url: config.urls.start });
    break; // Only handle the first tab
  }
  app.state.loaded = true;
  await new Promise((resolve) => setTimeout(resolve, 369)); // Wait for 0.369 seconds
};

const onInstalledOrStartup = async (type = "onyx") => {
  log.info(`${type} event`);
  await startup()
    .then(async () => {
      log.info("Extension started successfully");
      for (const tab of await chrome.tabs.query({ url: "http://127.0.0.1/*" })) {
        if (tab.id === app.state.tabId) continue;
        else await chrome.tabs.remove(tab.id);
      }
      chrome.tabs.onUpdated.addListener(async (_, status, tab) => {
        const remove =
          app.state.loaded &&
          app.state.tabId !== tab.id &&
          tab.url.startsWith("http://127.0.0.1") &&
          status.status === "loading";
        if (remove) {
          // Don't remove if there are no other tabs open
          await chrome.tabs.query({}).then(async (tabs) => {
            if (tabs.length === 1) await chrome.tabs.update(tab.id, { url: "about:blank" });
            else await chrome.tabs.remove(tab.id);
          });
        }
      });
    })
    .catch((error) => {
      log.error("Error during startup:", error);
    });
};
chrome.runtime.onInstalled.addListener(() => {
  onInstalledOrStartup("onInstalled");
});
chrome.runtime.onStartup.addListener(() => {
  onInstalledOrStartup("onStartup");
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
