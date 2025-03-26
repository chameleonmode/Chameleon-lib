import App from "./src/app.js";
import { log } from "./src/services/logger.js";
import { addUrlsAsBookmarks } from "./src/services/bookmarks.js";
import "./src/services/webrtc.js";
import "./src/services/debugger.js";

const startup = async () => {
  // Restore session from storage
  await App.startup();
  log.info("App started", App.config);

  await addUrlsAsBookmarks("Home Pages", App.config.urls.homePages);
};

// Fix the incomplete runtime event listener
chrome.runtime.onInstalled.addListener(async () => {
  log.info("Extension installed");
  await startup();
});

// Add runtime startup listener
chrome.runtime.onStartup.addListener(async () => {
  log.info("Extension started");
  await startup();
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
          sendResponse({ success: true, url: App.config.urls.start });
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

chrome.storage.onChanged.addListener((changes, namespace) => {
  for (let [key, { oldValue, newValue }] of Object.entries(changes)) {
    log.info(
      `Storage key "${key}" in namespace "${namespace}" changed.`,
      `Old value was "${oldValue}", new value is "${newValue}".`
    );
  }
  return true;
});
