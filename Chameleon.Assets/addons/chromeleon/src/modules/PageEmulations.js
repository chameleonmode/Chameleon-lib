import App from "../app.js";
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
    const { tzEmulation, timezone, tzSystem, tzLocale, tzRandomize } = App.config;
    if (tzEmulation) {
      log.info(`Applying timezone emulation for tab ${this.tabId}`);
      log.info(`Timezone: ${timezone}`);
      log.info(`System timezone: ${tzSystem}`);
      log.info(`Randomize timezone: ${tzRandomize}`);
      log.info(`Locale: ${tzLocale}`);
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Emulation.setTimezoneOverride", {
        timezoneId: tzSystem
          ? Intl.DateTimeFormat().resolvedOptions().timeZone
          : tzRandomize
          ? Object.keys(offsets)[Math.floor(Math.random() * Object.keys(offsets).length)]
          : timezone,
      });
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Emulation.setLocaleOverride", {
        locale: tzLocale,
      });
    }

    const { geoEmulation, lat, lon, geoRandomize, geoAccuracy } = App.config;
    if (geoEmulation) {
      log.info(`Applying geolocation emulation for tab ${this.tabId}`);
      log.info(`Latitude: ${lat}`);
      log.info(`Longitude: ${lon}`);
      log.info(`Randomize geolocation: ${geoRandomize}`);
      log.info(`Accuracy: ${geoAccuracy}`);
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Emulation.setGeolocationOverride", {
        latitude: geoRandomize ? lat + (Math.random() - 0.5) * geoRandomize : lat,
        longitude: geoRandomize ? lon + (Math.random() - 0.5) * geoRandomize : lon,
        accuracy: geoAccuracy,
      });

      const acceptLanguage = "en-US";
      const latitude = `latitude_e7: ${Math.floor(lat * 1e7)}`;
      const longitude = `longitude_e7: ${Math.floor(lon * 1e7)}`;
      const locationString = `role: CURRENT_LOCATION\nproducer: DEVICE_LOCATION\nradius: 65000\nlatlng <\n  ${latitude}\n  ${longitude}\n>`;
      const uule = `a ${btoa(locationString)}`;
      await chrome.declarativeNetRequest.updateSessionRules({
        removeRuleIds: [1, 420],
        addRules: [
          {
            id: 420,
            priority: 2,
            action: {
              type: "modifyHeaders",
              requestHeaders: [
                { header: "x-geo", operation: "set", value: uule },
                { header: "accept-language", operation: "set", value: acceptLanguage },
              ],
            },
            condition: {
              urlFilter: "*://www.google.com/*",
              resourceTypes: ["main_frame", "sub_frame", "xmlhttprequest", "ping"],
            },
          },
        ],
      });
    }
  }
}

export default PageEmulations;
