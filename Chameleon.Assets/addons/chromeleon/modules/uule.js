import { log } from "./logger.js";

// Function to modify headers for Google search requests
async function updateLocationRules() {
  const { lat, lon } = await chrome.storage.local.get(["lat", "lon"]);
  const acceptLanguage = "en-US";
  const latitude = `latitude_e7: ${Math.floor(lat * 1e7)}`;
  const longitude = `longitude_e7: ${Math.floor(lon * 1e7)}`;
  const locationString = `role: CURRENT_LOCATION\nproducer: DEVICE_LOCATION\nradius: 65000\nlatlng <\n  ${latitude}\n  ${longitude}\n>`;
  const uule = `a ${btoa(locationString)}`;

  chrome.declarativeNetRequest.updateSessionRules(
    {
      removeRuleIds: [1],
      addRules: [
        {
          id: 1,
          priority: 1,
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
    },
    () => {
      if (chrome.runtime.lastError) {
        log.error(chrome.runtime.lastError.message);
      } else {
        log.log("Google search headers updated with new location.");
      }
    }
  );
}
updateLocationRules();

function removeLocationRules() {
  chrome.declarativeNetRequest.updateSessionRules(
    {
      removeRuleIds: [1],
    },
    () => {
      if (chrome.runtime.lastError) {
        log.error(chrome.runtime.lastError.message);
      } else {
        log.log("Custom Google search location reset to default.");
      }
    }
  );
}

chrome.storage.onChanged.addListener(async (changes, namespace) => {
  if (!changes.lat && !changes.lon) return;
  await updateLocationRules();
});
