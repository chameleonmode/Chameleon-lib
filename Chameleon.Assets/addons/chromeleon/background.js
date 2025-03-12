import { App } from "./app.js";
import { log, setLogLevel } from "./modules/logger.js";
import { updateSettings, SETTINGS_ARRAY } from "./modules/settings.js";
import { updateLocationRules } from "./modules/uule.js";
import { applyOverrides } from "./modules/emulations.js";
import "./modules/webrtc.js";


// Fix the incomplete runtime event listener
chrome.runtime.onInstalled.addListener(async () => {
  log.info("Extension installed");
  // Restore session from storage
  App.session = await chrome.storage.local.get("session");
  App.config = await chrome.storage.local.get("config");
  if (App.session && App.config) {
    App.config.log = App.config.log || "all";
    if (!App.config.webRtcEnabled) {
      App.config.webRtcEnabled = true;
      App.config.dAPI = "default";
    }
    App.session.ready = true;
    log.info("Restored session", App.session, App.config);
  } else {
    App.config.log = "all";
    App.config.webRtcEnabled = true;
    App.config.dAPI = "default";
  }

  setLogLevel(App.config.log);
  createContextMenus();
});

// Add runtime startup listener
chrome.runtime.onStartup.addListener(async() => {
  log.info("Extension started");
});

// Listen for messages from popup or content scripts
chrome.runtime.onMessage.addListener(async (message, sender, sendResponse) => {
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
          setLogLevel(App.session.config.logLevel);
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

  chrome.tabs.query({}, async (tabs) => {
    await tabs.forEach(async (tab) => {
      await applyOverrides(tab);
    });
  });

  const settings = await chrome.storage.sync.get(SETTINGS_ARRAY);
  // Set WebRTC IP handling policy
  updateLocationRules(settings);

  //https://developer.chrome.com/docs/extensions/reference/api/userScripts
  const USER_SCRIPT_ID = "chromeleonairz";
  const __myAddonRandObjName__ = `${
    String.fromCharCode(65 + Math.floor(Math.random() * 26)) +
    Math.random()
      .toString(36)
      .substring(Math.floor(Math.random() * 5) + 5)
  }`;
  const userscripts = [
    {
      id: USER_SCRIPT_ID,
      allFrames: true,
      world: "MAIN",
      runAt: "document_start",
      matches: ["<all_urls>"],
      js: [
        {
          code: `
          if(!window.${__myAddonRandObjName__}) {
            window.${__myAddonRandObjName__} = ${Math.random() * 0.00000001};
            settings = JSON.parse(\`${JSON.stringify(settings)}\`);
          }`,
        },
        { file: "scriptin/clientrects.js" },
        { file: "scriptin/canvas.js" },
        { file: "scriptin/webgl.js" },
        { file: "scriptin/fonts.js" },
        { file: "scriptin/audio.js" },
      ],
    },
  ];

  const existingScripts = await chrome.userScripts.getScripts({
    ids: [USER_SCRIPT_ID],
  });
  if (existingScripts.length > 0) {
    await chrome.userScripts.update(userscripts);
  } else {
    try {
      await chrome.userScripts.register(userscripts);
    } catch (error) {
      log.error("Error registering user scripts", error);
      await chrome.userScripts.update(userscripts);
    }
  }
}

export function createContextMenus() {
  chrome.contextMenus.create({ title: "WebRTC", id: "webrtc-menu", contexts: ["action"] });
  chrome.contextMenus.create({
    title: "Enabled",
    id: "webRtcEnabled",
    contexts: ["action"],
    type: "checkbox",
    parentId: "webrtc-menu",
    checked: App.config.webRtcEnabled === true,
  });

  // options
  chrome.contextMenus.create({
    title: "Options",
    id: "webrtc-options",
    contexts: ["action"],
    parentId: "webrtc-menu",
  });
  chrome.contextMenus.create({
    parentId: "webrtc-options",
    type: "radio",
    contexts: ["action"],
    title: "default",
    id: "default",
    checked: App.config.dAPI === "default",
  });
  chrome.contextMenus.create({
    parentId: "webrtc-options",
    type: "radio",
    contexts: ["action"],
    title: "default public and private interfaces",
    id: "default_public_and_private_interfaces",
    checked: App.config.dAPI === "default_public_and_private_interfaces",
  });
  chrome.contextMenus.create({
    parentId: "webrtc-options",
    type: "radio",
    contexts: ["action"],
    title: "default public interface only",
    id: "default_public_interface_only",
    checked: App.config.dAPI === "default_public_interface_only",
  });
  chrome.contextMenus.create({
    parentId: "webrtc-options",
    type: "radio",
    contexts: ["action"],
    title: "disable non proxied udp",
    id: "disable_non_proxied_udp",
    checked: App.config.dAPI === "disable_non_proxied_udp",
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
