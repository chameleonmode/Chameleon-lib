import { applyOverrides, setupTabListeners } from "./modules/emulations.js";
import {
  createGeoContextMenus,
  handleGeoMenuClick,
} from "./modules/geolocation.js";
import { log, setLogLevel } from "./modules/logger.js";
import {
  SETTINGS_ARRAY,
  settings,
  updateSettings,
} from "./modules/settings.js";
import {
  createTimezoneContextMenus,
  handleTimezoneMenuClick,
} from "./modules/timezone.js";
import { genUULE, updateLocationRules } from "./modules/uule.js";
import {
  createWebRTCContextMenus,
  handleWebRTCMenuClick,
  handleWebRTCSettings,
} from "./modules/webrtc.js";

fetch(chrome.runtime.getURL("settings.json"))
  .then((response) => {
    if (!response.ok) {
      throw new Error("Network response was not ok");
    }
    return response.json(); // Parse JSON directly
  })
  .then(async (data) => {
    await updateSettings(data);
    setLogLevel(settings.debug);
    await applyAllOverrides();
    createWebRTCContextMenus();
    createGeoContextMenus();
    createTimezoneContextMenus();
    log.info("Received: ", data);
  })
  .catch((error) => console.error("Error loading settings:", error));

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  if (info.menuItemId === "rtc-test") {
    chrome.tabs.create({
      url: "https://webbrowsertools.com/ip-address/",
      index: tab.index + 1,
    });
  } else if (
    info.menuItemId.startsWith("webrtc") ||
    [
      "dApi",
      "disable_non_proxied_udp",
      "proxy_only",
      "default_public_interface_only",
      "default_public_and_private_interfaces",
    ].includes(info.menuItemId)
  ) {
    handleWebRTCMenuClick(info);
  } else if (
    info.menuItemId.startsWith("geo") ||
    info.menuItemId.startsWith("set:") ||
    info.menuItemId.startsWith("randomizeGeo:") ||
    info.menuItemId.startsWith("accuracy:") ||
    ["add-exception", "remove-exception", "exception-editor"].includes(
      info.menuItemId
    )
  ) {
    await handleGeoMenuClick(info, tab);
  } else if (
    info.menuItemId.startsWith("timezone") ||
    [
      "update-timezone",
      "set-timezone",
      "check-timezone",
      "randomize-timezone",
      "timezone-",
      "tz-enabled",
    ].includes(info.menuItemId)
  ) {
    await handleTimezoneMenuClick(info, tab);
  }

  await chrome.storage.sync.set(settings);
});

chrome.storage.onChanged.addListener(async (changes, namespace) => {
  for (let [key, { oldValue, newValue }] of Object.entries(changes)) {
    log.info(
      `Storage key "${key}" in namespace "${namespace}" changed.`,
      `Old value was "${oldValue}", new value is "${newValue}".`
    );
    settings[key] = newValue;
  }
  await applyAllOverrides();
  return true;
});
setupTabListeners();

async function applyAllOverrides() {
  chrome.tabs.query({}, (tabs) => {
    tabs.forEach((tab) => {
      applyOverrides(tab);
    });
  });

  await handleWebRTCSettings();
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
      matches: ["*://*/*"],
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
      ],
    },
  ];

  const existingScripts = await chrome.userScripts.getScripts({
    ids: [USER_SCRIPT_ID],
  });
  if (existingScripts.length > 0) {
    await chrome.userScripts.update(userscripts);
  } else {
    await chrome.userScripts.register(userscripts);
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
