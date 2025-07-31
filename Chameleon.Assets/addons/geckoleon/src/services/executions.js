import App from "../app.js";
import { log } from "./logger.js";
import { getAllSupportedLocales, matchesPattern } from "../lib/util.js";
import rects from "../scripts/rects.js";
import geo from "../scripts/geo.js";
import time from "../scripts/time.js";

function executions(details) {
  const { tabId, url } = details;
  // Check if the URL matches the bypass pattern
  if (
    !App.config.enabled ||
    !url.startsWith("http") ||
    matchesPattern(url, [...App.config.bypass, "com.mode.chameleon"])
  )
    return;

  // Inject into ALL frames including iframes
  log.info("Executing injections for tabId:", tabId, "url:", url, "config:", App.config);
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
browser.webNavigation.onCommitted.addListener(executions);
// Run on page load
browser.webNavigation.onDOMContentLoaded.addListener(executions);
// Run on tab creation
chrome.tabs.onCreated.addListener(async (tab) => {
  if (tab.index === 0) chrome.tabs.update(tab.id, { url: "about:blank" });
  else if (tab.title.startsWith("127.0.0.1") && tab.title.endsWith("foreground"))
    chrome.tabs.remove(tab.id);
});

