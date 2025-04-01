import App from "./src/app.js";
import { log } from "./src/services/logger.js";
import { addUrlsAsBookmarks } from "./src/services/bookmarks.js";
import rects from "./src/scripts/rects.js";
import geo from "./src/scripts/geo.js";
import tz from "./src/scripts/tz.js";

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
      `Old value was "${JSON.stringify(oldValue)}", new value is "${JSON.stringify(newValue)}".`
    );
  }
  return true;
});

// Function to inject our script into the page
function executeRects() {
  const config = App.config || {
    noise: "max",
    rects: { enabled: true, random: false },
  };

  return {
    type: "rects",
    enabled: config.rects.enabled,
    func: rects,
    args: [config.noise, config.rects.random],
  };
}

// Function to inject geolocation spoofing script
function executeGeo() {
  const config = App.config.geo || {
    enabled: true,
    random: false,
    lat: 40.7128,
    lon: -74.006,
    accuracy: 64.0999
  };

  return {
    type: "geo",
    enabled: config.enabled,
    func: geo,
    args: [config],
  };
}

// Function to inject our script into the page
function executeTimezone() {
  const config = App.config.tz || {
    enabled: true,
    random: false,
    useSystem: false,
    zone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    locale: Intl.DateTimeFormat().resolvedOptions().locale,
  };

  return {
    type: "tz",
    enabled: config.enabled,
    func: tz,
    args: [config.zone, config.locale],
  };
}

function executions(tabId) {
  const config = App.config || {
    enabled: true,
    noise: "max",
  };
  if (!config.enabled) return;
  
  // Inject into ALL frames including iframes
  const injections = [executeRects(), executeGeo(), executeTimezone()];
  injections.forEach(async (injection) => {
    const { type, enabled, func, args } = injection;
    if (!enabled) return;

    log.info(`Executing ${type} script into tab ${tabId}`);
    await browser.scripting.executeScript({
      world: "MAIN",
      injectImmediately: true,
      target: { tabId, allFrames: true },
      func: func,
      args: args,
    });
  });
}

// Run on navigation
browser.webNavigation.onCommitted.addListener((details) => {
  if (details.url.startsWith("http")) {
    executions(details.tabId);
  }
});

// Run on page load
browser.webNavigation.onDOMContentLoaded.addListener((details) => {
  if (details.url.startsWith("http")) {
    executions(details.tabId);
  }
});

// Run for existing tabs
browser.tabs.query({ url: ["http://*/*", "https://*/*"] }, (tabs) => {
  for (const tab of tabs) {
    executions(tab.id);
  }
});

// This listener will run before a request is made
browser.webRequest.onBeforeRequest.addListener(
  function (details) {
    // Check if this is a request to our target domain
    if (details.url.includes("chameleon.mode.com")) {
      // Parse the original URL to get its query parameters
      const originalUrl = new URL(details.url);
      const originalQueryParams = originalUrl.search.substring(1); // Remove the leading '?'

      // Create the redirect URL with our extension path
      let redirectUrl = browser.runtime.getURL("data/web/register.html");

      // Add our required source parameter
      redirectUrl += "?source=extension";

      // If there were original query parameters, append them
      if (originalQueryParams) {
        redirectUrl += "&" + originalQueryParams;
      }

      // Log to help with debugging
      log.log("Redirecting", details.url, "to", redirectUrl);

      // Return the new URL to redirect to
      return { redirectUrl: redirectUrl };
    }

    // Return null to allow the request to proceed normally
    return null;
  },
  // Only apply this listener to navigation requests to our target
  { urls: ["*://chameleon.mode.com/*"], types: ["main_frame"] },
  // This must be set to true to allow the redirect
  ["blocking"]
);
