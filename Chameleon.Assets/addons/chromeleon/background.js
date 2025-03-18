import { App } from "./app.js";
import { log } from "./src/services/logger.js";
import * as WebRTC from "./src/services/webrtc.js";
import "./src/services/uule.js";
import "./src/services/debugger.js";

// Fix the incomplete runtime event listener
chrome.runtime.onInstalled.addListener(async () => {
  
  // Restore session from storage
  App.session = (await chrome.storage.local.get("session")).session;
  App.config = (await chrome.storage.local.get("config")).config;

  log.setLogLevel(App.config.log);
  createContextMenus();

  log.info("Restored session", { session: App.session, config: App.config });
});

// Add runtime startup listener
chrome.runtime.onStartup.addListener(async () => {
  log.info("Extension started");
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
    App.initialize(sessionId, instanceId).then((success) => {
      if (success) {
        log.setLogLevel(App.config.log);
        createContextMenus();
        // Configure once at application startup

        log.info("App connected", App.config);
      }
      sendResponse({ success });
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

log.info("Background script loaded");
