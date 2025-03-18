/**
 * PageEmulations
 *
 * For use with Chrome Extensions Manifest V3 background service workers.
 */
import { log } from "../services/logger.js";
import { offsets } from "../../data/offsets.js";

class PageEmulations {
  constructor(tabId) {
    this.tabId = tabId;
  }

  /**
   * Initialize the observer for a specific tab
   * @param {number} tabId - The ID of the tab to observe
   * @returns {Promise<boolean>} - Whether initialization was successful
   */
  async initialize() {
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
  }
}

export default PageEmulations;
