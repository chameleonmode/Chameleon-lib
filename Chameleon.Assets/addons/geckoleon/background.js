import app, { noises } from "./src/app.js";
import { log } from "./src/services/logger.js";
import { addUrlsAsBookmarks } from "./src/services/bookmarks.js";
import "./src/services/executions.js";

const proxio = (port) =>
  browser.proxy.onRequest.addListener(
    () => (port ? { type: "http", host: "127.0.0.1", port } : { type: "direct" }),
    { urls: ["<all_urls>"] }
  );

// Startup
const startup = async () => {
  // Then load the config from local storage, merging with syncConfig
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

const on = async () => {
  await new Promise((resolve) => setTimeout(resolve, 369)); // Wait for 0.369 second
  // Reset the state
  app.state.loaded = false;
  await startup();

  // Check for existing tabs
  const allTabs = await chrome.tabs.query({});
  const localTabs = allTabs.filter(
    (tab) => tab.url.startsWith("http://127.0.0.1") && new URL(tab.url).searchParams.has("proxio")
  );
  if (!localTabs.length) return;
  allTabs.filter((tab) => !localTabs.includes(tab)).forEach((tab) => chrome.tabs.remove(tab.id));
  for (const tab of await chrome.tabs.query({ url: "http://127.0.0.1/*" })) {
    if (app.state.loaded) {
      chrome.tabs.remove(tab.id);
      continue;
    }

    const url = new URL(tab.url);
    const sessionId = url.searchParams.get("sessionId");
    const instanceId = url.searchParams.get("instanceId");
    const proxioPort = url.searchParams.get("proxio");
    await app.discoverServer();
    const init = await app.sendData({ type: "init" }, { instanceId, sessionId });
    if (!init || !init.config || !init.port) {
      chrome.tabs.remove(tab.id);
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
    proxio(app.config.proxy.enabled ? proxioPort : null);
    await chrome.storage.local.set({ session: app.session, config: app.config });
    await addUrlsAsBookmarks(app.name, app.config.urls.homePages);
    await chrome.tabs.update(tab.id, { url: app.config.urls.start });
    app.state.loaded = true;
  }
};

const onInstalledOrStartup = async (type = "onyx") => {
  log.info(`${type} event`);
  await on()
    .then(() => {
      log.info("Extension started successfully");
    })
    .catch((error) => {
      log.error("Error during startup:", error);
    });

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
