import { config } from "./config.js";
import { log, setLogLevel } from "./modules/logger.js";
import { updateSettings, SETTINGS_ARRAY } from "./modules/settings.js";
import { applyOverrides } from "./modules/emulations.js";
import { genUULE, updateLocationRules } from "./modules/uule.js";
import { createWebRTCContextMenus, handleWebRTCMenuClick } from "./modules/webrtc.js";

async function init() {
  setLogLevel(config.logLevel);
  createWebRTCContextMenus(config);
  chrome.contextMenus.create({
    title: "Exception List Editor",
    id: "exception-editor",
    contexts: ["action"],
  });
  await updateSettings(config);
  await applyAllOverrides();
}
chrome.runtime.onInstalled.addListener(init);
chrome.runtime.onStartup.addListener(init);

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  if (info.menuItemId === "exception-editor") {
    const msg = `Insert one hostname per line. Press the "Save List" button to update the list.

    Example of valid formats:
    
      example.com
      *.example.com
      https://example.com/*
      *://*.example.com/*`;
    chrome.windows.getCurrent((win) => {
      chrome.windows.create({
        url: `data/editor/index.html?msg=${encodeURIComponent(msg)}&storage=bypass`,
        width: 600,
        height: 600,
        left: win.left + Math.round((win.width - 600) / 2),
        top: win.top + Math.round((win.height - 600) / 2),
        type: "popup",
      });
    });
  } else {
    handleWebRTCMenuClick(info);
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

chrome.tabs.onCreated.addListener(async (tab) => {
  await applyOverrides(tab);
});

chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
  if (changeInfo.status === "loading") {
    await applyOverrides(tab);
  }
});

async function applyAllOverrides() {
  log.info("Applying all overrides");

  chrome.tabs.query({}, async(tabs) => {
    await tabs.forEach( async(tab) => {
      await applyOverrides(tab);
    });
  });

  const settings = await chrome.storage.sync.get(SETTINGS_ARRAY);
  const value = settings.webRtcEnabled && settings.dAPI ? settings.eMode : settings.dMode;
  chrome.privacy.network.webRTCIPHandlingPolicy.clear({}, () => {
    chrome.privacy.network.webRTCIPHandlingPolicy.set({ value }, () => {
      chrome.privacy.network.webRTCIPHandlingPolicy.get({}, (s) => {
        //
      });
    });
  });
  updateLocationRules(genUULE(settings.latitude, settings.longitude));

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
