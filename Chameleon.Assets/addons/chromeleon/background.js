import App from "./src/app.js";
import proxy from "./src/services/proxy.js";
import { log } from "./src/services/logger.js";
import { addUrlsAsBookmarks } from "./src/services/bookmarks.js";
import "./src/services/webrtc.js";
import "./src/services/debugger.js";

const startup = async (id = -1) => {
  // Restore session from storage
  log.info("App initialized with session:", App.session);
  log.info("App initialized with config:", App.config);
  log.info("App initialized with launchedSessions:", App.launchedSessions);

  // Reload all tabs except the one that triggered the startup
  const tabs = await chrome.tabs.query({});
  await Promise.all(
    tabs.filter((tab) => tab.id !== id).map((tab) => chrome.tabs.reload(tab.id, { bypassCache: true }))
  );

  // Add bookmarks for home pages
  await addUrlsAsBookmarks("Home Pages", App.config.urls.homePages);
};

const on = async () => {
  log.info("On installed or started");
  // Run for existing tabs and Handle chameleon.mode.com redirects
  const initializer = (await chrome.tabs.query({ url: ["*://com.mode.chameleon/*"] })).at(-1);
  if (initializer) {
    const url = new URL(initializer.url);
    const sessionId = url.searchParams.get("sessionId");
    const instanceId = url.searchParams.get("instanceId");
    await App.initialize(sessionId, instanceId);

    await App.startup();
    await proxy(App.config.proxy);
    await chrome.tabs.update(initializer.id, { url: App.config.urls.start });
  } else {
    await App.startup();
    await proxy(App.config.proxy);
  }

  await startup(initializer?.id);
};
chrome.runtime.onInstalled.addListener(on);
chrome.runtime.onStartup.addListener(on);

// Listen for messages from popup or content scripts
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  log.info("Received message:", message);
  switch (message.action) {
    case "getConfig":
      sendResponse({ success: true, config: App.config });
      break;
    case "updateConfig":
      App.config = { ...App.config, ...message.config };
      App.notify("configUpdated");

      // Save the updated config to both local and sync storage in parallel
      Promise.all([
        chrome.storage.local.set({ config: App.config }),
        chrome.storage.sync.set({ config: App.config }),
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

  // Return true to indicate that sendResponse will be called asynchronously and keep channel open
  return true;
});

// zip -r -X archive.zip * -x "*.DS_Store" -x ".*"