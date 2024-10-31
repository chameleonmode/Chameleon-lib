import { setLogLevel, log } from "./modules/logger.js";
import { settings, updateSettings } from "./modules/settings.js";
import { createWebRTCContextMenus, handleWebRTCMenuClick, handleWebRTCSettings } from "./modules/webrtc.js";
import { createGeoContextMenus, handleGeoMenuClick } from "./modules/geolocation.js";
import { createTimezoneContextMenus, handleTimezoneMenuClick } from "./modules/timezone.js";
import { applyOverrides, setupTabListeners } from "./modules/emulations.js";
import { genUULE, updateLocationRules } from './modules/uule.js';
let loaded = false
chrome.runtime.onConnectExternal.addListener((port) => {
  console.assert(port.name === "communication");

  // Listen for messages from the sender extension
  port.onMessage.addListener(async (msg) => {
    await updateSettings(msg.message);
    setLogLevel(settings.debug);
    const uule = genUULE(settings.latitude, settings.longitude);
    updateLocationRules(uule);
    if (!loaded) {
      OnLoad();
      loaded = true;
    }
  });
});

chrome.contextMenus.onClicked.addListener(handleContextMenuClick);

chrome.storage.onChanged.addListener(async (changes, _) => {
    // Apply changes to settings
    for (let key in changes) {
        if (changes.hasOwnProperty(key)) {
            settings[key] = changes[key].newValue;
        }
    }
    handleWebRTCSettings();
    applyTabOverrides();
    const uule = genUULE(settings.latitude, settings.longitude);
    updateLocationRules(uule);
  log.info("Settings updated");
});

function OnLoad() {
  applyTabOverrides();
  createWebRTCContextMenus();
  createGeoContextMenus();
  createTimezoneContextMenus();

  log.info("OnLoad");
}

function applyTabOverrides(){
  chrome.tabs.query({}, (tabs) => {
    tabs.forEach((tab) => {
        applyOverrides(tab);
    });
});
}

async function handleContextMenuClick(info, tab) {
  if (info.menuItemId === "test") {
    chrome.tabs.create({
      url: "https://webbrowsertools.com/ip-address/",
      index: tab.index + 1,
    });
  } else if (info.menuItemId.startsWith("webrtc") || ["dApi", "disable_non_proxied_udp", "proxy_only", "default_public_interface_only", "default_public_and_private_interfaces"].includes(info.menuItemId)) {
    handleWebRTCMenuClick(info);
  } else if (info.menuItemId.startsWith("geo") || info.menuItemId === "enabled" || info.menuItemId === "reset" || info.menuItemId.startsWith("set:") || info.menuItemId.startsWith("randomizeGeo:") || info.menuItemId.startsWith("accuracy:") || ["add-exception", "remove-exception", "exception-editor"].includes(info.menuItemId)) {
    await handleGeoMenuClick(info, tab);
  } else if (["update-timezone", "set-timezone", "check-timezone", "randomize-timezone"].includes(info.menuItemId)) {
    await handleTimezoneMenuClick(info, tab);
  }
  
  await chrome.storage.sync.set(settings);
}
setupTabListeners();

log.info("Background script loaded");
