import WebpageMutationObserver from "../modules/WebpageMutationObserver.js";

// Keep track of observers per tab
const tabObservers = new Map();

/**
 * Initialize iframe monitoring for a specific tab
 * @param {number} tabId - The ID of the tab to monitor
 */
async function monitorTabIframes(tabId) {
  try {
    console.log(`Setting up iframe monitoring for tab ${tabId}`);

    // Attach debugger to the tab (as per requirements)
    await chrome.debugger.attach({ tabId }, "1.3");

    // Initialize the mutation observer
    const observer = new WebpageMutationObserver();
    if (!await observer.initialize(tabId)) {
      console.error(`Failed to initialize WebpageMutationObserver for tab ${tabId}`);
      return null;
    }
    // Store the observer instance for later cleanup
    tabObservers.set(tabId, observer);

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

    console.log(`Successfully set up iframe monitoring for tab ${tabId}`);
    return observer;
  } catch (error) {
    console.error(`Error setting up iframe monitoring for tab ${tabId}:`, error);

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

/**
 * Clean up monitoring for a specific tab
 * @param {number} tabId - The ID of the tab to clean up
 */
function cleanupTabMonitoring(tabId) {
  try {
    console.log(`Cleaning up iframe monitoring for tab ${tabId}`);

    const observer = tabObservers.get(tabId);
    if (observer) {
      observer.cleanup();
      tabObservers.delete(tabId);
    }

    // Detach debugger
    chrome.debugger.detach({ tabId }).catch(() => {
      // Ignore errors when detaching (tab might be gone already)
    });

    console.log(`Successfully cleaned up iframe monitoring for tab ${tabId}`);
  } catch (error) {
    console.error(`Error cleaning up iframe monitoring for tab ${tabId}:`, error);
  }
}

// Example: Start monitoring when a tab is activated
chrome.tabs.onActivated.addListener(async (activeInfo) => {
  const { tabId } = activeInfo;
  const { url } = await chrome.tabs.get(tabId);

  console.log(`Tab ${tabId} activated with URL: ${url}`);
  if(url.startsWith("chrome://")) return;
  // Only attach if not already monitoring this tab
  if (!tabObservers.has(tabId)) {
    await monitorTabIframes(tabId);
  }
});

// Cleanup when a tab is closed
chrome.tabs.onRemoved.addListener((tabId) => {
  cleanupTabMonitoring(tabId);
});