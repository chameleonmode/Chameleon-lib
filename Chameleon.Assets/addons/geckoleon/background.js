import App from "./src/app.js";
import { getAllSupportedLocales, matchesPattern } from "./src/lib/util.js";
import { log } from "./src/services/logger.js";
import { addUrlsAsBookmarks } from "./src/services/bookmarks.js";
import rects from "./src/scripts/rects.js";
import geo from "./src/scripts/geo.js";
import time from "./src/scripts/time.js";

const startup = async () => {
  // Restore session from storage
  await App.startup();
  log.info("App initialized with session:", App.session);
  log.info("App initialized with config:", App.config);
  log.info("App initialized with launchedSessions:", App.launchedSessions);

  // Set up the proxy if enabled
  await browser.proxy.settings.set({
    value: !App.config.proxy.enabled
      ? {
          proxyType: "none",
        }
      : {
          proxyType: "manual",
          http: App.config.proxy.server,
          https: App.config.proxy.server,
          passthrough: "localhost, 127.0.0.1, *://chameleon.mode.com/*",
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
browser.runtime.onInstalled.addListener(async () => {
  log.info("Extension installed");
  // await startup();
});

// Add runtime startup listener
browser.runtime.onStartup.addListener(async () => {
  log.info("Extension started");
  // await startup();
});

browser.webRequest.onAuthRequired.addListener(
  (details) => {
    log.info("Auth required for request:", details);
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

// Authentication handler for proxy requests
browser.proxy.onRequest.addListener(
  (details) => {
    log.info("Proxy request:", details);
    if (App.config.proxy.enabled) {
      return {
        type: "http",
        host: App.config.proxy.host,
        port: App.config.proxy.port,
        username: App.config.proxy.username,
        password: App.config.proxy.password,
      };
    } else {
      // If not authenticated or not enabled, use direct connection
      return { type: "direct" };
    }
  },
  { urls: ["<all_urls>"] }
);

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

function executions(tabId, url) {
  // Check if the URL matches the bypass pattern
  if (!App.config.enabled || matchesPattern(url, [...App.config.bypass, "chameleon.mode.com"])) return;

  log.info("Executing injections for tabId:", tabId, "url:", url, "config:", App.config);

  // Inject into ALL frames including iframes
  const injections = [
    // Rects
    {
      init: () => {
        const type = "rects";
        const opts = { ...App.config[type], noise: App.config.noise };
        return {
          type,
          opts,
          func: rects,
        };
      },
    },
    // Geo
    {
      init: () => {
        const type = "geo";
        const opts = App.config[type];
        if (opts.enabled) {
          chrome.declarativeNetRequest
            .updateSessionRules({
              removeRuleIds: [420],
              addRules: [
                {
                  id: 420,
                  priority: 2,
                  action: {
                    type: "modifyHeaders",
                    requestHeaders: [
                      {
                        header: "x-geo",
                        operation: "set",
                        value: `a ${btoa(`role: CURRENT_LOCATION 
                         producer: DEVICE_LOCATION
                         radius: 65000
                         latlng <
                          latitude_e7: ${Math.floor(opts.lat * 1e7)}
                          longitude_e7: ${Math.floor(opts.lon * 1e7)}
                         >`)}`,
                      },
                      { header: "accept-language", operation: "set", value: App.config.tz.locale },
                    ],
                  },
                  condition: {
                    urlFilter: "*://www.google.com/*",
                    resourceTypes: ["main_frame", "sub_frame", "xmlhttprequest", "ping"],
                  },
                },
              ],
            })
            .then(() => {
              log.info("Session rules updated successfully");
            })
            .catch((error) => {
              log.warn("Error updating session rules", error);
            });
        } else {
          chrome.declarativeNetRequest
            .updateSessionRules({
              removeRuleIds: [420],
            })
            .then(() => {
              log.info("Session rules removed successfully");
            })
            .catch((error) => {
              log.warn("Error removeing session rules", error);
            });
        }
        return {
          type,
          opts,
          func: geo,
        };
      },
    },
    // Time
    {
      init: () => {
        const type = "tz";
        const opts = App.config[type];

        if (opts.system) {
          opts.zone = Intl.DateTimeFormat().resolvedOptions().timeZone;
          opts.locale = Intl.DateTimeFormat().resolvedOptions().locale;
        } else if (opts.random) {
          const timezones = Intl.supportedValuesOf("timeZone");
          opts.zone = timezones[Math.floor(Math.random() * timezones.length)];
          const flat = getAllSupportedLocales().flat;
          opts.locale = flat[Math.floor(Math.random() * flat.length)];
        }
        return {
          type,
          opts,
          func: time,
        };
      },
    },
  ];
  injections.forEach((i) => {
    const { type, opts, func } = i.init();

    log.info(`Executing ${type}, enabled: ${opts.enabled}, opts: ${opts} into tab ${tabId}`);
    if (opts.enabled) {
      browser.scripting.executeScript({
        world: "MAIN",
        injectImmediately: true,
        target: { tabId, allFrames: true },
        func: func,
        args: [opts],
      });
    }
  });
}

// Run on navigation
browser.webNavigation.onCommitted.addListener((details) => {
  // Handle chameleon.mode.com redirects
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
    console.log("Redirecting", details.url, "to", redirectUrl);
  
    // Use browser.tabs.update to redirect the tab
    browser.tabs.update(details.tabId, { url: redirectUrl });

    return;
  }
  
  if (details.url.startsWith("http")) {
    executions(details.tabId, details.url);
  }
});

// Run on page load
browser.webNavigation.onDOMContentLoaded.addListener((details) => {
  if (details.url.startsWith("http")) {
    executions(details.tabId, details.url);
  }
});
