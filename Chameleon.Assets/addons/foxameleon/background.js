import { setLogLevel, log } from "./modules/logger.js";
import { settings, updateSettings } from "./modules/settings.js";
import { createGeoContextMenus, handleGeoMenuClick } from "./modules/geolocation.js";
import { createTimezoneContextMenus, handleTimezoneMenuClick, getRandomTimezone, getTimezoneOffset } from "./modules/timezone.js";
import { genUULE, updateLocationRules } from './modules/uule.js';

let loaded = false

// Assuming the JSON file is located in the root of your extension folder
fetch(browser.runtime.getURL("settings.json"))
    .then(response => {
        if (!response.ok) {
            throw new Error("Network response was not ok");
        }
        return response.json(); // Parse JSON directly
    })
    .then(async (data) => {
        await updateSettings(data);
        setLogLevel(settings.debug);
        const uule = genUULE(settings.latitude, settings.longitude);
        updateLocationRules(uule);
        await OnLoad();
        loaded = true;
        log.info("Received: ", data);
    })
    .catch(error => console.error('Error loading settings:', error));

// Add the webNavigation onCommitted listener
browser.webNavigation.onCommitted.addListener(async (details) => {
    if (details.frameId === 0) { // Ensures the script is only registered for the main frame
        await setInjectionScript();
        log.info("Injection script registered onCommitted");
    }
}, { url: [{ schemes: ["http", "https"] }] });

chrome.contextMenus.onClicked.addListener(handleContextMenuClick);

browser.storage.onChanged.addListener(async (changes, _) => {
    // Apply changes to settings
    for (let key in changes) {
        if (changes.hasOwnProperty(key)) {
            settings[key] = changes[key].newValue;
        }
    }
  setInjectionScript();
  log.info("Settings updated");
});

var injectionScript;
async function setInjectionScript() {
  if(injectionScript) {
    await injectionScript.unregister();
  }
  if (settings.myIP) {
    settings.timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  } else if (settings.randomizeTZ) {
    settings.timezone = getRandomTimezone();
    }

  settings.tzOffset = getTimezoneOffset(settings.timezone);
  
    injectionScript =
        await browser.contentScripts.register({
    allFrames: true,
    matchAboutBlank: true,
    matches: ['http://*/*', 'https://*/*'],
    js: [
      {
      code:`
          if (!window.__myAddonInjected__) {
            window.__myAddonInjected__ = true;
            window.__myAddonSettings__ = JSON.parse(\`${JSON.stringify(settings)}\`);
            window.__myAddonSeed__ = ${Math.random() * 0.00000001};
            window.__myAddonRandObjName__ = '${String.fromCharCode(65 + Math.floor(Math.random() * 26)) + Math.random().toString(36).substring(Math.floor(Math.random() * 5) + 5)}';
            console.log("Addon settings initialized:", window.__myAddonSettings__);
          }
      `,
      },
      { file: 'scripts/inject.js' },
    ],
    runAt: 'document_start',
  });
}
async function OnLoad() {
    try {
        await browser.contextMenus.removeAll();
        createGeoContextMenus();
        createTimezoneContextMenus();
    } catch (e) {
        log.error("Failed to create context menus", e);
    }
  log.info("OnLoad");
}

async function handleContextMenuClick(info, tab) {
  if (info.menuItemId === "test") {
      await chrome.tabs.create({
          url: "https://webbrowsertools.com/ip-address/",
          index: tab.index + 1,
      });
  } else if (info.menuItemId.startsWith("geo") || info.menuItemId === "enabled" || info.menuItemId === "reset" || info.menuItemId.startsWith("set:") || info.menuItemId.startsWith("randomizeGeo:") || info.menuItemId.startsWith("accuracy:") || ["add-exception", "remove-exception", "exception-editor"].includes(info.menuItemId)) {
      await handleGeoMenuClick(info, tab);
  } else if (["update-timezone", "set-timezone", "check-timezone", "randomize-timezone"].includes(info.menuItemId)) {
      await handleTimezoneMenuClick(info, tab);
  }

  await browser.storage.sync.set(settings);
}

log.info("Background script loaded");
