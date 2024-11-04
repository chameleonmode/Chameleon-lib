import { setLogLevel, log } from "./modules/logger.js";
import {
  settings,
  updateSettings,
  SETTINGS_ARRAY,
} from "./modules/settings.js";
import {
  createWebRTCContextMenus,
  handleWebRTCMenuClick,
  handleWebRTCSettings,
} from "./modules/webrtc.js";
import {
  createGeoContextMenus,
  handleGeoMenuClick,
} from "./modules/geolocation.js";
import {
  createTimezoneContextMenus,
  handleTimezoneMenuClick,
} from "./modules/timezone.js";
import { applyOverrides, setupTabListeners } from "./modules/emulations.js";
import { genUULE, updateLocationRules } from "./modules/uule.js";

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
    await handleWebRTCSettings();
    const uule = genUULE(settings.latitude, settings.longitude);
    updateLocationRules(uule);
    applyTabOverrides();
    createWebRTCContextMenus();
    createGeoContextMenus();
    createTimezoneContextMenus();
    log.info("Received: ", data);
  })
  .catch((error) => console.error("Error loading settings:", error));

//chrome.runtime.onConnectExternal.addListener((port) => {
//  console.assert(port.name === "communication");

//  // Listen for messages from the sender extension
//    port.onMessage.addListener(async (msg) => {
//      if(msg.message === "reload")
//        chrome.runtime.reload();
//  });
//});

//chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
//    if (request.action === 'getSettings') {
//        chrome.storage.sync.get(SETTINGS_ARRAY, (settings) => {
//            sendResponse({ settings });
//        });
//        // Return true to indicate you want to send a response asynchronously
//        return true;
//    }
//});

// chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
//   if (changeInfo.status === "loading" && /^http/.test(tab.url)) {
//     chrome.scripting.executeScript(
//       {
//         target: { tabId: tabId, allFrames: true },
//         func: (settings) => {
//           window._extensionSettings = settings;
//         },
//         args: [settings],
//       },
//       (results) => {
//         if (chrome.runtime.lastError) {
//           console.error("Injection error:", chrome.runtime.lastError);
//         } else {
//           results.forEach((result, index) => {
//             console.log(
//               `Settings successfully injected in frame ${index}`,
//               result
//             );
//           });
//         }
//       }
//     );
//   }
// });

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

chrome.storage.onChanged.addListener((changes, namespace) => {
  for (let [key, { oldValue, newValue }] of Object.entries(changes)) {
    log.info(
      `Storage key "${key}" in namespace "${namespace}" changed.`,
      `Old value was "${oldValue}", new value is "${newValue}".`
    );
    settings[key] = newValue;
  }
  applyTabOverrides();
  const uule = genUULE(settings.latitude, settings.longitude);
  updateLocationRules(uule);
});

function applyTabOverrides() {
  try {
    chrome.tabs.query({}, (tabs) => {
      tabs.forEach((tab) => {
        applyOverrides(tab);
      });
    });
  } catch (e) {
    log.error("Failed to apply tab overrides", e);
  }
}
setupTabListeners();

log.info("Background script loaded");
