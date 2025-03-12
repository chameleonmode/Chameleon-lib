import { log } from "./logger.js";
import { offsets } from "./offsets.js";
import { SETTINGS_ARRAY } from "./settings.js";

// chrome.storage.onChanged.addListener(async (changes, namespace) => {
//   chrome.tabs.query({}, async (tabs) => {
//     await tabs.forEach(async (tab) => {
//       await applyOverrides(tab);
//     });
//   });
// });

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
        await applyGeoOverride(tab, settings);
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
  const { randomizeGeo, accuracy, geoSpoofing, lat, lon } = settings;
  if(!geoSpoofing) return;
  
  let latitude = lat;
  let longitude = lon;
  
  if (randomizeGeo) {
    const m = lat + (Math.random() > 0.5 ? 1 : -1) * randomizeGeo * Math.random();
    latitude = Number(m.toFixed(lat.toString().split(".")[1].length));

    const n = lon + (Math.random() > 0.5 ? 1 : -1) * randomizeGeo * Math.random();
    latitude = Number(n.toFixed(lon.toString().split(".")[1].length));
  }

  await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setGeolocationOverride", {
    latitude,
    longitude,
    accuracy: accuracy,
  });

  log.info(`Geolocation set to ${lat}, ${lon} for tab ${tab.id}`);
}

chrome.tabs.onCreated.addListener(async (tab) => {
  await applyOverrides(tab);
});

chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
  if (changeInfo.status === "loading") {
    await applyOverrides(tab);
  }
});