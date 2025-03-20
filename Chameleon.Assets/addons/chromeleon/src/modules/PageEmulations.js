import App from "../app.js";
import { log } from "../services/logger.js";
import offsets from "../../data/offsets.js";

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
    if (!App.config.enabled) return;

    await this.setupTimezoneEmulation();
    await this.setupGeoEmulation();
  }

  async setupTimezoneEmulation() {
    const { enabled, zone, locale, random, useSystem } = App.config.tz;
    if (enabled) {
      log.info(`Applying timezone emulation for tab ${this.tabId}`, App.config.tz);
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Emulation.setTimezoneOverride", {
        timezoneId: useSystem
          ? Intl.DateTimeFormat().resolvedOptions().timeZone
          : random
          ? Object.keys(offsets)[Math.floor(Math.random() * Object.keys(offsets).length)]
          : zone,
      });
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Emulation.setLocaleOverride", {
        locale: locale,
      });
    }
  }

  async setupGeoEmulation() {
    const { enabled, lat, lon, random, accuracy } = App.config.geo;
    if (enabled) {
      log.info(`Applying geolocation emulation for tab ${this.tabId}`, App.config.geo);
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Emulation.setGeolocationOverride", {
        latitude: random ? lat + (Math.random() - 0.5) * random : lat,
        longitude: random ? lon + (Math.random() - 0.5) * random : lon,
        accuracy: accuracy,
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
