import WebpageMutations from "../modules/WebpageMutations.js";
import { log } from "./logger.js";

// Keep track of mutator per tab
const observers = new Map();

/**
 * Initialize iframe monitoring for a specific tab
 * @param {number} tabId - The ID of the tab to monitor
 */
async function monitorTabIframes(tabId) {
  try {
    log.log(`Setting up iframe monitoring for tab ${tabId}`);

    // Attach debugger to the tab (as per requirements)
    await chrome.debugger.attach({ tabId }, "1.3");
    // Enable required domains for Chrome Debugger API
    await chrome.debugger.sendCommand({ tabId: tabId }, "DOM.enable");
    await chrome.debugger.sendCommand({ tabId: tabId }, "Runtime.enable");
    await chrome.debugger.sendCommand({ tabId: tabId }, "Page.enable");

    // Initialize the mutation mutator
    const mutations = new WebpageMutations(tabId);
    await mutations.initialize();

    // Set up the emulation
    const { tzEmulation, timezone, tzSystem, tzLocale, tzRandomize } = await chrome.storage.local.get([
      "tzEmulation",
      "timezone",
      "tzSystem",
      "tzRandomize",
      "tzLocale",
    ]);
    if (tzEmulation) {
      log.info(`Applying timezone emulation for tab ${tab.id}`);
      log.info(`Timezone: ${timezone}`);
      log.info(`System timezone: ${tzSystem}`);
      log.info(`Randomize timezone: ${tzRandomize}`);
      log.info(`Locale: ${tzLocale}`);
      await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setTimezoneOverride", {
        timezoneId: tzSystem
          ? Intl.DateTimeFormat().resolvedOptions().timeZone
          : tzRandomize
          ? Object.keys(offsets)[Math.floor(Math.random() * Object.keys(offsets).length)]
          : timezone,
      });
      await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setLocaleOverride", {
        locale: tzLocale,
      });
    }

    const { geoEmulation, lat, lon, geoRandomize, geoAccuracy } = await chrome.storage.local.get([
      "geoEmulation",
      "lat",
      "lon",
      "geoRandomize",
      "geoAccuracy",
    ]);
    if (geoEmulation) {
      log.info(`Applying geolocation emulation for tab ${tab.id}`);
      log.info(`Latitude: ${lat}`);
      log.info(`Longitude: ${lon}`);
      log.info(`Randomize geolocation: ${geoRandomize}`);
      log.info(`Accuracy: ${geoAccuracy}`);
      await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setGeolocationOverride", {
        latitude: geoRandomize ? lat + (Math.random() - 0.5) * geoRandomize : lat,
        longitude: geoRandomize ? lon + (Math.random() - 0.5) * geoRandomize : lon,
        accuracy: geoAccuracy,
      });
    }

    // Store reference to the mutations instance and unsubscribe function
    // so you can clean up later when the tab is closed
    observers.set(tabId, {
        mutations,
    });
  } catch (error) {
    log.error(`Error setting up iframe monitoring for tab ${tabId}:`, error);

    // Try to clean up if we failed
    try {
      chrome.debugger.detach({ tabId }).catch(() => {
        // Ignore errors when detaching
      });
    } catch (e) {
      // Ignore cleanup errors
    }

    return null;
  }
}

chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
  if (changeInfo.status !== "loading" || observers.has(tabId) || !tab.url.startsWith("http")) return;
  log.log(`Tab ${tabId} is loading with URL: ${tab.url}`);
  await monitorTabIframes(tabId);
});

chrome.tabs.onRemoved.addListener(async (tabId) => {
  const observer = observers.get(tabId);
  if (observer) {
    try {
      await chrome.debugger.detach({ tabId });
    } catch (e) {
      // Ignore errors when detaching
    }
    observers.delete(tabId);
  }
});
