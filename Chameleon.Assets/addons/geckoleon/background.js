import App from "./src/app.js";
import { log } from "./src/services/logger.js";
import { addUrlsAsBookmarks } from "./src/services/bookmarks.js";
import "./src/services/proxy.js";
import "./src/services/executions.js";

const startup = async () => {
  // Restore session from storage
  await App.startup();
  log.info("App initialized with session:", App.session);
  log.info("App initialized with config:", App.config);
  log.info("App initialized with launchedSessions:", App.launchedSessions);

  const tabs = await chrome.tabs.query({});
  await Promise.all(tabs.map((tab) => chrome.tabs.reload(tab.id, { bypassCache: true })));

  // Add bookmarks for home pages
  await addUrlsAsBookmarks("Home Pages", App.config.urls.homePages);
};

// Fix the incomplete runtime event listener
browser.runtime.onInstalled.addListener(async () => {
  log.info("Extension installed");
  // await startup();
});

// Add runtime startup listener
browser.runtime.onStartup.addListener(async () => {
  log.info("Extension started");
  // await startup();
});

// Listen for messages from popup or content scripts
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  log.info("Received message:", message);
  switch (message.action) {
    case "getConfig":
      sendResponse({ success: true, config: App.config });
      break;
    case "updateConfig":
      App.config = { ...App.config, ...message.config };
      App.eventSystem.notify("configUpdated");

      // Save the updated config to storage
      chrome.storage.local
        .set({ config: App.config })
        .then(() => log.info("Config saved to storage"))
        .catch((error) => log.error("Error saving config to storage", error));

      // You might also want to save to sync storage
      chrome.storage.sync
        .set({ config: App.config })
        .then(() => log.info("Config saved to sync storage"))
        .catch((error) => log.error("Error saving config to sync storage", error));

      sendResponse({ success: true });
      break;
    case "refreshConfig":
      App.initialize(App.session.sessionId, App.session.instanceId)
        .then(() => sendResponse({ success: true }))
        .catch((error) => sendResponse({ success: false, error: error.message }));
      break;
    case "getAppState":
      App.getAppState()
        .then((state) => sendResponse({ success: true, data: state }))
        .catch((error) => sendResponse({ success: false, error: error.message }));
      break;
    case "sendToApp":
      App.sendData(message.data)
        .then((response) => sendResponse({ success: true, data: response }))
        .catch((error) => sendResponse({ success: false, error: error.message }));
      break;
    case "registerAppLaunch":
      const { sessionId, instanceId, data } = message;
      App.initialize(sessionId, instanceId)
        .then(async (result) => {
          await startup();
          sendResponse({ success: result === true, url: App.config.urls.start });
        })
        .catch((error) => {
          log.error("Error registering app launch", error);
          sendResponse({ success: false, error: error.message });
        });
      break;
    default:
      log.warn("Unknown message action:", message.action);
      sendResponse({ success: false, error: "Unknown message type" });
      break;
  }

  return true; // Keep the message channel open for async response
});

// Run for existing tabs and Handle chameleon.mode.com redirects
browser.tabs
  .query({ url: ["*://com.mode.chameleon/*"] })
  .then(async (tabs) => {
    const tab = tabs.at(-1);
    if (!tab) return;
    // Create the redirect URL with our extension path
    const url = new URL(tab.url);
    const sessionId = url.searchParams.get("sessionId");
    const instanceId = url.searchParams.get("instanceId");
    await App.initialize(sessionId, instanceId);
    await browser.tabs.update(tab.id, { url: App.config.urls.start });
    await startup();
  })
  .catch((error) => {
    log.warn("Error in redirect:", error);
  });
