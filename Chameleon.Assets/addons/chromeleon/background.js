import { App } from "./app.js";
import { log, setLogLevel } from "./modules/logger.js";
import { updateSettings, SETTINGS_ARRAY } from "./modules/settings.js";
import * as WebRTC from "./modules/webrtc.js";
import "./modules/emulations.js";
import "./modules/uule.js";
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
  await chrome.storage.local.set({ ...App.config });

  setLogLevel(App.config.log);
  createContextMenus();
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
    App.initialize(sessionId, instanceId, data).then(
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
