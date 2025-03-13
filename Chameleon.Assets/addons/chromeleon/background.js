import { App } from "./app.js";
import { log, setLogLevel } from "./modules/logger.js";
import { updateSettings, SETTINGS_ARRAY } from "./modules/settings.js";
import { updateLocationRules } from "./modules/uule.js";
import { applyOverrides } from "./modules/emulations.js";
import * as WebRTC from "./modules/webrtc.js";
//import "./modules/canvasing.js";

// Fix the incomplete runtime event listener
chrome.runtime.onInstalled.addListener(async () => {
  log.info("Extension installed");
  // Restore session from storage
  App.session = await chrome.storage.local.get("session");
  App.config = await chrome.storage.local.get("config");
  if (App.session && App.config) {
    log.info("Restored session", { session: App.session, config: App.config });
  }

  App.config.enabled = true;
  App.config.log = "all";
  App.config.dAPI = WebRTC.policies.disable_non_proxied_udp.id;
  App.config.canvasing = true;
  await chrome.storage.sync.set({ ...App.config });

  setLogLevel(App.config.log);
  createContextMenus();

  // await chrome.userScripts.configureWorld({
  //     csp: "script-src 'self'; object-src 'self'",
  // });
  // Register user scripts
});

// Background script approach for bypassing CSP in iframes
// This requires appropriate permissions in manifest.json:
// - "activeTab" or specific site permissions
// - "scripting" permission for Manifest V3

// Store your script content
const scriptContent = `
  // Your actual script code here
  console.log("Script executed successfully, bypassing CSP");
  
  // Add your functionality here
  
`;

// For Manifest V3 extensions, use this approach
if (typeof chrome !== "undefined" && chrome.scripting) {
  // Function to inject into all frames including those with CSP
  async function injectIntoAllFrames() {
 try {
    // Get the current active tab
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });

    if (!tab) {
      console.error("No active tab found");
      return;
    }

    // Execute script in the current tab to target the specific iframe
    chrome.scripting
      .executeScript({
        target: { tabId: tab.id },
        func: (scriptToInject) => {
          // Function to inject script into the specific iframe
          function injectIntoTargetIframe() {
            // Find the target iframe
            const targetIframe = document.getElementById('canvas-iframe');
            
            if (!targetIframe) {
              console.error("Target iframe with id 'canvas-iframe' not found");
              return;
            }
            
            console.log("Target iframe found:", targetIframe);
            
            // Function to inject script into a document
            function injectScriptIntoDocument(scriptContent, document) {
              try {
                // Create blob from the script content
                const blob = new Blob([scriptContent], { type: "application/javascript" });
                
                // Create a URL for the blob
                const blobURL = URL.createObjectURL(blob);
                
                // Create and inject the script element
                const script = document.createElement("script");
                script.src = "about:blank"; // Set a dummy URL to allow script execution
                script.onload = function () {
                  // Clean up the URL when done
                  URL.revokeObjectURL(blobURL);
                  console.log("Script injected successfully into canvas-iframe");
                };
                
                // Append the script to the document
                document.documentElement.appendChild(script);
              } catch (err) {
                console.error("Error injecting script:", err);
              }
            }
            
            // Try to access the iframe's content document
            try {
              // Since the iframe has sandbox="allow-same-origin", we should be able to access its contentDocument
              if (targetIframe.contentDocument) {
                injectScriptIntoDocument(scriptToInject, targetIframe.contentDocument);
              } else {
                console.error("Cannot access iframe contentDocument - same-origin policy or sandbox restrictions may be preventing access");
              }
            } catch (err) {
              console.error("Error accessing iframe:", err);
            }
          }
          
          // Run the injection immediately
          injectIntoTargetIframe();
          
          // Also set up a MutationObserver to handle if the iframe is created dynamically
          const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
              mutation.addedNodes.forEach((node) => {
                // Check if the added node is our target iframe
                if (node.id === 'canvas-iframe' && node.tagName === 'IFRAME') {
                  // Wait a bit for the iframe to load
                  setTimeout(() => {
                    injectIntoTargetIframe();
                  }, 100);
                }
                
                // Also check if our target iframe was added inside this node
                if (node.querySelectorAll) {
                  const targetIframe = node.querySelector('#canvas-iframe');
                  if (targetIframe) {
                    setTimeout(() => {
                      injectIntoTargetIframe();
                    }, 100);
                  }
                }
              });
            });
          });
          
          // Start observing the document with the configured parameters
          observer.observe(document, {
            childList: true,
            subtree: true,
          });
        },
        args: [scriptContent],
        world: "MAIN",
      })
      .catch((e) => console.error("Error injecting script:", e));
      
  } catch (error) {
    console.error("Error in injectIntoCanvasIframe:", error);
  }
  }

  // For popup or background script to trigger injection
  chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.action === "injectScript") {
      injectIntoAllFrames()
        .then(() => {
          sendResponse({ success: true });
        })
        .catch((error) => {
          sendResponse({ success: false, error: error.message });
        });
      return true; // Required for async response
    }
  });
}

const userscripts = [
  {
    id: "chromeleon",
    world: "MAIN",
    runAt: "document_start",
    matches: ["<all_urls>"],
    allFrames: true,
    js: [
      //   { code: `
      //   // Additional properties that might be used for fingerprinting
      //   // Modify navigator properties
      //   const navigatorProps = {
      //     hardwareConcurrency: Math.min(8, navigator.hardwareConcurrency),
      //     deviceMemory: Math.min(8, navigator.deviceMemory || 8),
      //   };

      //   // Apply navigator property spoofing
      //   for (const [prop, value] of Object.entries(navigatorProps)) {
      //     if (navigator[prop] !== undefined) {
      //       try {
      //         Object.defineProperty(navigator, prop, {
      //           get: function() { return value; }
      //         });
      //       } catch (e) {
      //         console.log("Failed to override navigator." + prop);
      //       }
      //     }
      //   }
      // `},
      { file: "scriptin/canvas.js" },
      { file: "scriptin/navigator.js" },
    ],
  },
];
//chrome.userScripts.register(userscripts);

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
  if (message.action === "sendToApp") {
    App.sendData(message.data)
      .then((response) => sendResponse({ success: true, data: response }))
      .catch((error) => sendResponse({ success: false, error: error.message }));
    return true; // Indicates async response
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
    App.initialize(message.sessionId, message.appInstanceId, message.additionalData).then(
      async (success) => {
        if (success) {
          createContextMenus();
          setLogLevel(App.session.config.log);
          App.session.ready = true;
          await applyAllOverrides();
          log.info("App connected", config);
        }
        sendResponse({ success });
      }
    );
    return true;
  }

  if (message.action === "getAppSession") {
    sendResponse({
      session: App.session,
    });
    return true;
  }
});

chrome.storage.onChanged.addListener(async (changes, namespace) => {
  for (let [key, { oldValue, newValue }] of Object.entries(changes)) {
    log.info(
      `Storage key "${key}" in namespace "${namespace}" changed.`,
      `Old value was "${oldValue}", new value is "${newValue}".`
    );
  }
  await applyAllOverrides();
  return true;
});

async function applyAllOverrides() {
  if (App.session.ready === false) return;

  log.info("Applying all overrides");

  // chrome.tabs.query({}, async (tabs) => {
  //   await tabs.forEach(async (tab) => {
  //     await applyOverrides(tab);
  //   });
  // });

  const settings = await chrome.storage.sync.get(SETTINGS_ARRAY);
  // Set WebRTC IP handling policy
  updateLocationRules(settings);
  //return;

  //https://developer.chrome.com/docs/extensions/reference/api/userScripts
  // const USER_SCRIPT_ID = "chromeleonairz";
  // const __myAddonRandObjName__ = `${
  //   String.fromCharCode(65 + Math.floor(Math.random() * 26)) +
  //   Math.random()
  //     .toString(36)
  //     .substring(Math.floor(Math.random() * 5) + 5)
  // }`;
  // const userscripts = [
  //   {
  //     id: USER_SCRIPT_ID,
  //     allFrames: true,
  //     world: "MAIN",
  //     runAt: "document_start",
  //     matches: ["<all_urls>"],
  //     js: [
  //       {
  //         code: `
  //         if(!window.${__myAddonRandObjName__}) {
  //           window.${__myAddonRandObjName__} = ${Math.random() * 0.00000001};
  //           settings = JSON.parse(\`${JSON.stringify(settings)}\`);
  //         }`,
  //       },
  //       //{ file: "scriptin/clientrects.js" },
  //       { file: "scriptin/canvas.js" },
  //      // { file: "scriptin/webgl.js" },
  //      // { file: "scriptin/fonts.js" },
  //       //{ file: "scriptin/audio.js" },
  //     ],
  //   },
  // ];

  // const existingScripts = await chrome.userScripts.getScripts({
  //   ids: [USER_SCRIPT_ID],
  // });
  // if (existingScripts.length > 0) {
  //   await chrome.userScripts.update(userscripts);
  // } else {
  //   try {
  //     await chrome.userScripts.register(userscripts);
  //   } catch (error) {
  //     log.error("Error registering user scripts", error);
  //     await chrome.userScripts.update(userscripts);
  //   }
  // }
}

export function createContextMenus() {
  chrome.contextMenus.removeAll();
  chrome.contextMenus.create({ title: "WebRTC", id: "webrtc-menu", contexts: ["action"] });

  // options
  const options = [
    WebRTC.policies.default,
    WebRTC.policies.default_public_and_private_interfaces,
    WebRTC.policies.default_public_interface_only,
    WebRTC.policies.disable_non_proxied_udp,
  ];
  // create context menus
  options.forEach((option) => {
    chrome.contextMenus.create({
      parentId: "webrtc-menu",
      type: "radio",
      contexts: ["action"],
      title: option.title,
      id: option.id,
      checked: App.config.dAPI === option.id,
    });
  });
}

// chrome.webNavigation.onDOMContentLoaded.addListener(async ({ tabId, url }) => {
//   chrome.scripting.executeScript({
//     target: { tabId, allFrames : true},
//     injectImmediately: true,
//     world: "MAIN",
//     args: [settings],
//     func: (settings) => {
//       // window.__myAddonSettings__ = settings;
//       document.documentElement.setAttribute("__myAddonSettings__", settings);
//     }
//   });
//   chrome.scripting.executeScript({
//     target: { tabId, allFrames : true},
//     injectImmediately: true,
//     world: "MAIN",
//     files: ['scriptin/clientrects.js'],
//   });
// });
// chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
//   if (changeInfo.status === "loading" && /^http/.test(tab.url)) {
//   chrome.scripting.executeScript({
//     target: { tabId, allFrames : true},
//     injectImmediately: true,
//     world: "MAIN",
//     args: [settings],
//     func: (settings) => {
//       // window.__myAddonSettings__ = settings;
//       document.documentElement.setAttribute("__myAddonSettings__", settings);
//     }
//   });
//   chrome.scripting.executeScript({
//     target: { tabId, allFrames : true},
//     injectImmediately: true,
//     world: "MAIN",
//     files: ['scriptin/clientrects.js'],
//   });
// }
// });

log.info("Background script loaded");

// // background.js - Updated for isolated world script injection
// chrome.runtime.onInstalled.addListener(() => {
//   console.log("Canvas Fingerprint Protector installed");

//   // Set up declarativeNetRequest rules to block known fingerprinting scripts
//   const rules = [
//     {
//       id: 1,
//       priority: 1,
//       action: { type: "block" },
//       condition: {
//         urlFilter: "*fingerprint*.js",
//         resourceTypes: ["script"]
//       }
//     },
//     {
//       id: 2,
//       priority: 1,
//       action: { type: "block" },
//       condition: {
//         urlFilter: "*analytics*canvas*",
//         resourceTypes: ["script"]
//       }
//     }
//   ];

//   chrome.declarativeNetRequest.updateDynamicRules({
//     removeRuleIds: [1, 2],
//     addRules: rules
//   });

//   // Initialize default settings
//   chrome.storage.local.set({
//     enableProxyAPI: true,
//     enableCSSInjection: true,
//     enableShadowDOM: true,
//     noiseLevel: 5, // 1-10 scale
//     blockedCount: 0,
//     isolatedWorldInjection: true // New setting for isolated world injection
//   });
// });

// // Handle tab activation to ensure protection is applied
// chrome.tabs.onActivated.addListener((activeInfo) => {
//   injectProtectionScripts(activeInfo.tabId);
// });

// // Handle tab updates to ensure protection is applied to new page loads
// chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
//   if (changeInfo.status === 'loading') {
//     injectProtectionScripts(tabId);
//   }
// });

// // Function to inject scripts into the isolated world
// function injectProtectionScripts(tabId) {
//   chrome.storage.local.get(['isolatedWorldInjection'], (settings) => {
//     if (settings.isolatedWorldInjection) {
//       // Only inject into main frame, not iframes (could be changed if needed)
//       chrome.scripting.executeScript({
//         target: { tabId: tabId, allFrames: true },
//         files: ['content-isolated.js'],
//         // world: "ISOLATED" is the default in Manifest V3
//       }).catch(error => {
//         console.error("Script injection failed:", error);
//       });
//     }
//   });
// }

// // Handle messages from content scripts
// chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
//   if (message.type === "getSettings") {
//     chrome.storage.local.get(null, (settings) => {
//       sendResponse(settings);
//     });
//     return true; // Keep the message channel open for async response
//   }

//   if (message.type === "fingerprintingDetected" || message.type === "protectionActive") {
//     console.log(`${message.type} in ${message.world || 'unknown'} world`);

//     // Update badge counter if it's a fingerprinting detection
//     if (message.type === "fingerprintingDetected") {
//       // Increment counter for detected fingerprinting attempts
//       chrome.storage.local.get("blockedCount", (data) => {
//         const newCount = (data.blockedCount || 0) + 1;
//         chrome.storage.local.set({ blockedCount: newCount });

//         // Update the badge
//         chrome.action.setBadgeText({ text: newCount.toString() });
//         chrome.action.setBadgeBackgroundColor({ color: '#F44336' });
//       });
//     }
//   }

//   if (message.type === "updateSettings") {
//     // Broadcast settings update to all content scripts
//     chrome.tabs.query({}, (tabs) => {
//       tabs.forEach(tab => {
//         chrome.tabs.sendMessage(tab.id, {
//           type: "updateSettings",
//           ...message.settings
//         }).catch(() => {
//           // Tab might not have content script injected, that's okay
//         });
//       });
//     });

//     // Re-inject scripts if isolated world setting changed
//     if (message.settings.isolatedWorldInjection !== undefined) {
//       chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
//         if (tabs[0]) {
//           injectProtectionScripts(tabs[0].id);
//         }
//       });
//     }

//     sendResponse({ status: "settings-broadcast-initiated" });
//   }
// });

// // Monitor web requests for potential fingerprinting
// chrome.webRequest.onCompleted.addListener(
//   function(details) {
//     // Check if the URL contains likely fingerprinting indicators
//     const url = details.url.toLowerCase();
//     if (url.includes('fingerprint') ||
//         (url.includes('canvas') && (url.includes('track') || url.includes('detect'))) ||
//         (url.includes('device') && url.includes('identify'))) {

//       // Log the detected request
//       console.log("Potential fingerprinting request detected:", details.url);

//       // Increment counter
//       chrome.storage.local.get("blockedCount", (data) => {
//         const newCount = (data.blockedCount || 0) + 1;
//         chrome.storage.local.set({ blockedCount: newCount });

//         // Update the badge
//         chrome.action.setBadgeText({ text: newCount.toString() });
//         chrome.action.setBadgeBackgroundColor({ color: '#F44336' });
//       });
//     }
//   },
//   { urls: ["<all_urls>"] }
// );
