import { log } from "./logger.js";
import { offsets } from "./offsets.js";
import { SETTINGS_ARRAY } from "./settings.js";

export async function applyOverrides(tab) {
  try {
    const settings = await chrome.storage.sync.get(SETTINGS_ARRAY);
    if (tab.url.indexOf("chrome://") < 0 && (settings.timezoneSpoofing || settings.geoSpoofing)) {
      try {
        await chrome.debugger.attach({ tabId: tab.id }, "1.3");
      } catch (error) {
        log.error(`Failed to attach debugger to tab ${tab.id}:`, error);
      }
      if (tab && tab.url) {
        await applyTimezoneOverride(tab, settings);
        if (settings.geoSpoofing) {
          await applyGeoOverride(tab, settings);
        }
      }
      log.log(`Debugger attached and overrides applied for tab ${tab.id}`);
      return true;
    }
  } catch (error) {
    log.error(`Failed to attach debugger or apply overrides for tab ${tab.id}:`, error);
  }
  return false;
}

async function applyTimezoneOverride(tab, settings) {
  const { myIP, randomizeTZ, timezone, locale } = settings;
  const timezoneId = myIP
    ? Intl.DateTimeFormat().resolvedOptions().timeZone
    : randomizeTZ
    ? Object.keys(offsets)[Math.floor(Math.random() * Object.keys(offsets).length)]
    : timezone;

  await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setTimezoneOverride", { timezoneId });
  await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setLocaleOverride", { locale });

  log.info(`Timezone set to ${timezoneId} for tab ${tab.id}`);
}

async function applyGeoOverride(tab, settings) {
  const { randomizeGeo, accuracy } = settings;
  let { latitude, longitude } = settings;
  if (randomizeGeo) {
    const m = latitude + (Math.random() > 0.5 ? 1 : -1) * randomizeGeo * Math.random();
    latitude = Number(m.toFixed(latitude.toString().split(".")[1].length));

    const n = longitude + (Math.random() > 0.5 ? 1 : -1) * randomizeGeo * Math.random();
    longitude = Number(n.toFixed(longitude.toString().split(".")[1].length));
  }

  await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setGeolocationOverride", {
    latitude: latitude,
    longitude: longitude,
    accuracy: accuracy,
  });

  log.info(`Geolocation set to ${latitude}, ${longitude} for tab ${tab.id}`);
}
