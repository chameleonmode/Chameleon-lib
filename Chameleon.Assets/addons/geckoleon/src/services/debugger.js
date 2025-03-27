// This file is responsible for managing the Chrome Debugger API
import App from "../app.js";
import { log } from "./logger.js";

// Keep track of mutator per tab
const observers = new Map();

// Subscribe to "dataUpdated" event
App.eventSystem.subscribe("configUpdated", async (data) => {
  log.log("Confg updated:", data);

  for (const [tabId, observer] of observers.entries()) {
  }
});

App.eventSystem.subscribe("startup", async (data) => {
  // Re-enable the content scripts
  await enable();
  log.log(`Re-enabled content scripts`);
});

const enable = async () => {
  // await browser.scripting.registerContentScripts([{
  //   id: `injection-peroplection`,
  //   allFrames: true,
  //   matchOriginAsFallback: true,
  //   world: "MAIN",
  //   runAt: "document_start",
  //   matches: ["<all_urls>"],
  //   js: [ "../scripts/rects.js",
  //     // { file: "scripts/timezone.js" },
  //     // { file: "scripts/geolocation.js" },
  //   ],
  // }]);
};

// Attach the debugger to the tab
const attach = async (tabId, tab) => {
  try {
    log.log(`Setting up iframe monitoring for tab ${tabId}`);
    // await enable(tabId);

    observers.set(tabId, {
      initialize: async () => {
        log.log(`Attached debugger to tab ${tabId} with version 1.3`, browser.debugger);
        try {
          await browser.scripting.executeScript({
            target: {
              tabId,
              allFrames: true,
            },
            injectImmediately: true,
            func: (uuid, noise, random) => {
              console.log("Geckoleon: Script execution started", uuid, noise, random);
              
              window[uuid] = window[uuid] || {
                params: {
                  uuid,
                  noise,
                  random
                },
              };
              
              // Map different noise levels from smallest to largest
              const noises = {
                nano: Number.EPSILON * 5,
                mini: 0.2,
                low: 0.3,
                medium: 0.4,
                bold: 0.5,
                high: 0.6,
                ultra: 0.7,
                super: 0.8,
                max: 0.9,
              };
          
              // Get noise level for random or fixed noise setting
              const noiseify = () => {
                console.log("Noise selection:", random, noise);
                return noises[
                  random ? Object.keys(noises)[Math.floor(Math.random() * Object.keys(noises).length)] : noise
                ];
              };
          
              const define = (prototype, property) => {
                try {
                  const originalDescriptor = Object.getOwnPropertyDescriptor(prototype, property);
                  if (!originalDescriptor || !originalDescriptor.get) {
                    console.error(`Geckoleon: Missing descriptor or getter for ${property}`);
                    return;
                  }
                  
                  Object.defineProperty(prototype, property, {
                    get: new Proxy(originalDescriptor.get, {
                      apply(target, self, args) {
                        const result = Reflect.apply(target, self, args);
                        const noiseLevel = noiseify();
                        console.log(`Geckoleon: Rects noise injection for ${property}`, result, noiseLevel);
                        return result * noiseLevel;
                      },
                    }),
                  });
                  console.log(`Geckoleon: Successfully defined proxy for ${property}`);
                } catch (e) {
                  console.error(`Geckoleon: Error defining property ${property}`, e);
                }
              };
          
              // Define property lists for each rectangle type
              if (!window[uuid]["rects"]) {
                try {
                  window[uuid]["rects"] = true;
                  
                  // Apply noise to all selected properties
                  ["x", "y", "width", "height"].forEach((property) => {
                    define(window.DOMRect.prototype, property);
                  });
                  
                  ["top", "right", "bottom", "left"].forEach((property) => {
                    define(window.DOMRectReadOnly.prototype, property);
                  });
                  
                  console.log("Geckoleon: Rect noise injection complete");
                } catch (e) {
                  console.error("Geckoleon: Error in rect noise injection", e);
                }
              }

              // At the end of your func:
//const testRect = new DOMRect(10, 10, 100, 100);
console.log("Geckoleon test:", testRect.x, testRect.y, testRect.width, testRect.height);
            },
            args: [
              "geckoleon_uuid", 
              // Ensure these values are properly defined or use fallbacks
              typeof App !== 'undefined' && App.config ? App.config.noise : "medium", 
              typeof App !== 'undefined' && App.config && App.config.rects ? App.config.rects.random : false
            ],
            world: "MAIN",
          });
          // await browser.scripting.executeScript({
          //   target: {
          //     tabId,
          //     allFrames: true,
          //   },
          //   injectImmediately: true,
          //   files: [ "src/scripts/rects.js"],
          //   world: "MAIN",
          // });
        } catch (err) {
          console.error(`failed to execute script: ${err}`);
        }
      },
    });
  } catch (error) {
    log.error(`Error setting up iframe monitoring for tab ${tabId}:`, error);
    await detach(tabId);
  }
};

// Detach the debugger from the tab
const detach = async (tabId) => {
  if (observers.get(tabId)) {
    observers.delete(tabId);
  }
};

chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
  log.info(`Tab ${tabId} is loading with URL: ${tab.url}`);
  if (changeInfo.status !== "loading" || !tab.url.startsWith("http")) {
    return;
  }
  if (!App.config.enabled || App.config.bypass.some((bypassUrl) => tab.url.startsWith(bypassUrl))) {
    log.log(`Tab ${tabId} is bypassed`);
    await detach(tabId);
    return;
  }
  if (!observers.has(tabId)) await attach(tabId, tab);
  await observers.get(tabId).initialize();
});

chrome.tabs.onRemoved.addListener(async (tabId) => {
  log.log(`Tab ${tabId} was removed`);
  await detach(tabId);
});
