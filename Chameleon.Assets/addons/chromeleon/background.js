import App from "./src/app.js";
import proxy from "./src/services/proxy.js";
import { log } from "./src/services/logger.js";
import { addUrlsAsBookmarks } from "./src/services/bookmarks.js";
import { checkForExtensionUpdate } from "./src/lib/util.js";
import "./src/services/webrtc.js";
import "./src/services/debugger.js";

const on = async () => {
  log.info("On installed or started");
  await new Promise(resolve => setTimeout(resolve, 500)); // Wait for 1 second
  await checkForExtensionUpdate();
  const initializer = async () => {
    await App.discoverServer();
    const tabs = await chrome.tabs.query({});
    const tab = tabs.find(t=>t.url.includes(App.server));
    if (tab) return tab;
    else return await initializer();
  };
  const tab = await initializer();
  if (tab) {
    const url = new URL(tab.url);
    const sessionId = url.searchParams.get("sessionId");
    const instanceId = url.searchParams.get("instanceId");

    // Wait for page to fully load
    await waitForTabLoad(tab.id);

    // Get page content
    const results = await chrome.scripting.executeScript({
      target: { tabId: tab.id },
      func: () => document.body.textContent,
    });

    await App.initialize(sessionId, instanceId, JSON.parse(results[0].result));
  }  

  // Common startup operations
  await App.startup();
  await proxy(App.config.proxy);
  await addUrlsAsBookmarks("Chromeleon", App.config.urls.homePages);
  
  const id = tab?.id || tab?.id || (await chrome.tabs.query({}))[0].id;
  await chrome.tabs.update(id, { url: App.config.urls.start});
};

// Helper function to wait for tab to load
const waitForTabLoad = async (tabId) => {
  return new Promise((resolve) => {
    const checkStatus = async () => {
      const tabInfo = await chrome.tabs.get(tabId);
      tabInfo.status === "complete" ? resolve() : setTimeout(checkStatus, 100);
    };
    checkStatus();
  });
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
    default:
      log.warn("Unknown message action:", message.action);
      sendResponse({ success: false, error: "Unknown message type" });
      break;
  }

  // Return true to indicate that sendResponse will be called asynchronously and keep channel open
  return true;
});
