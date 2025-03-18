import { App } from "./app.js";
import { log } from "./src/services/logger.js";
import * as WebRTC from "./src/services/webrtc.js";
import "./src/services/uule.js";
import "./src/services/debugger.js";

const startup = async () => {
  // Restore session from storage
  await App.startup();

  log.setLogLevel(App.config.log);
  createContextMenus();
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
  if (message.type === "getSettings") {
    chrome.storage.local.get(null, (settings) => {
      sendResponse(settings);
    });
    return true; // Keep the message channel open for async response
  }
  if (message.action === "injectScript") {
    // injectIntoAllFrames()
    //   .then(() => {
    //     sendResponse({ success: true });
    //   })
    //   .catch((error) => {
    //     sendResponse({ success: false, error: error.message });
    //   });
    return true;
  }
  if (message.action === "sendToApp") {
    App.sendData(message.data)
      .then((response) => sendResponse({ success: true, data: response }))
      .catch((error) => sendResponse({ success: false, error: error.message }));
    return true;
  }
  if (message.action === "getAppState") {
    App.getAppState()
      .then((state) => sendResponse({ success: true, data: state }))
      .catch((error) => sendResponse({ success: false, error: error.message }));
    return true;
  }
  if (message.action === "checkConnection") {
    App.discoverServer()
      .then((running) => sendResponse({ connected: running }))
      .catch(() => sendResponse({ connected: false }));
    return true;
  }
  if (message.action === "registerAppLaunch") {
    const { sessionId, instanceId, data } = message;
    log.info("Registering app launch", { sessionId, instanceId, data });
    App.initialize(sessionId, instanceId)
      .then(async (config) => {
        if (config) {
          log.debug("App connected", config);
          await startup();
        }
        sendResponse({ success: true, config });
      })
      .catch((error) => {
        log.error("Error registering app launch", error);
        sendResponse({ success: false, error: error.message });
      });
    return true;
  }
  if (message.action === "getAppSession") {
    sendResponse({
      session: App.session,
    });
    return true;
  }
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

export function createContextMenus() {
  chrome.contextMenus.removeAll();
  chrome.contextMenus.create({ title: "WebRTC", id: "webrtc-menu", contexts: ["action"] });

  // create context menus
  Object.keys(WebRTC.policies).forEach((key) => {
    const policy = WebRTC.policies[key];
    chrome.contextMenus.create({
      parentId: "webrtc-menu",
      type: "radio",
      contexts: ["action"],
      title: policy.title,
      id: policy.id,
      checked: App.config.dAPI === policy.id,
    });
  });
}
