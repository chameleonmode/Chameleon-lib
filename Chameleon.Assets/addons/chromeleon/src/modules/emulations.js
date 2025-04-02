import App from "../app.js";
import { log } from "../services/logger.js";
import { getAllSupportedLocales } from "../lib/util.js";


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
    const opts = App.config.tz;
    if (opts.enabled) {
      log.info(`Applying timezone emulation for tab ${this.tabId}`, App.config.tz);
      if (opts.system) {
        opts.zone = Intl.DateTimeFormat().resolvedOptions().timeZone;
        opts.locale = Intl.DateTimeFormat().resolvedOptions().locale;
      } else if (opts.random) {
        const timezones = Intl.supportedValuesOf("timeZone");
        opts.zone = timezones[Math.floor(Math.random() * timezones.length)];

        const flat = getAllSupportedLocales().flat;
        opts.locale = flat[Math.floor(Math.random() * flat.length)];
      }
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Emulation.setTimezoneOverride", {
        timezoneId: opts.zone,
      });
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Emulation.setLocaleOverride", {
        locale: opts.locale,
      });
    }
  }

  async setupGeoEmulation() {
    const { enabled, lat, lon, random, accuracy } = App.config.geo;
    if (enabled) {
      log.info(`Applying geolocation emulation for tab ${this.tabId}`, App.config.geo);
      const latitude = random ? lat + (Math.random() - 0.5) * random : lat;
      const longitude = random ? lon + (Math.random() - 0.5) * random : lon;
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Emulation.setGeolocationOverride", {
        latitude,
        longitude,
        accuracy,
      });

      const acceptLanguage = "en-US";
      const locationString = `role: CURRENT_LOCATION
         producer: DEVICE_LOCATION
         radius: 65000
         latlng <
           latitude_e7: ${Math.floor(lat * 1e7)}
           longitude_e7: ${Math.floor(lon * 1e7)}
         >`;
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
    }else{
      await chrome.declarativeNetRequest.updateSessionRules({
        removeRuleIds: [1, 420],
      });
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Emulation.clearGeolocationOverride");
    }
  }
}

export default PageEmulations;
