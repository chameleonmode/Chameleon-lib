import App from "./src/app.js";
import { log } from "./src/services/logger.js";
import { addUrlsAsBookmarks } from "./src/services/bookmarks.js";
import "./src/services/webrtc.js";
import "./src/services/debugger.js";

const startup = async () => {
  // Restore session from storage
  await App.startup();
  log.info("Session restored:", App.session);
  log.info("Config restored:", App.config);
  log.info("Launched sessions restored:", App.launchedSessions);

  // Set up the proxy if enabled
  await chrome.proxy.settings.set({
    value: !App.config.proxy.enabled
      ? { mode: "system" }
      : {
          mode: "fixed_servers",
          rules: {
            bypassList: ["<local>"],
            singleProxy: {
              scheme: "http",
              host: App.config.proxy.host,
              port: App.config.proxy.port,
            },
          },
        },
  });
  
  // Query for all HTTP and HTTPS tabs thenreload each matching tab with bypassCache option
  for (const tab of await chrome.tabs.query({ url: ["http://*/*", "https://*/*"] })) {
    await chrome.tabs.reload(tab.id, { bypassCache: true });
  }

  // Add bookmarks for home pages
  await addUrlsAsBookmarks("Home Pages", App.config.urls.homePages);
};

// Fix the incomplete runtime event listener
chrome.runtime.onInstalled.addListener(async () => {
  log.info("Extension installed");
  // await startup();
});

// Add runtime startup listener
chrome.runtime.onStartup.addListener(async () => {
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

chrome.webRequest.onAuthRequired.addListener(
  (_) => {
    return {
      authCredentials: {
        username: App.config.proxy.username,
        password: App.config.proxy.password,
      },
    };
  },
  { urls: ["<all_urls>"] },
  ["blocking"]
);
