import { log } from "./logger.js";

// Function to modify headers for Google search requests
export function updateLocationRules(settings) {
    const acceptLanguage = 'en-US';;
    const lat = Math.floor(settings.lat * 1e7);
    const lng = Math.floor(settings.lon * 1e7);
    const locationString = `role: CURRENT_LOCATION\nproducer: DEVICE_LOCATION\nradius: 65000\nlatlng <\n  latitude_e7: ${lat}\n  longitude_e7: ${lng}\n>`;
    const uule = `a ${btoa(locationString)}`;

    chrome.declarativeNetRequest.updateSessionRules({
        removeRuleIds: [1],
        addRules: [
            {
                id: 1,
                priority: 1,
                action: {
                    type: "modifyHeaders",
                    requestHeaders: [
                        { header: "x-geo", operation: "set", value: uule },
                        { header: "accept-language", operation: "set", value: acceptLanguage }
                    ]
                },
                condition: {
                    urlFilter: "*://www.google.com/*",
                    resourceTypes: ["main_frame", "sub_frame", "xmlhttprequest", "ping"]
                }

            }
        ]
    }, () => {
        if (chrome.runtime.lastError) {
            log.error(chrome.runtime.lastError.message);
        } else {
            log.log("Google search headers updated with new location.");
        }
    });
}

export function removeLocationRules() {
    chrome.declarativeNetRequest.updateSessionRules({
        removeRuleIds: [1],
    }, () => {
        if (chrome.runtime.lastError) {
            log.error(chrome.runtime.lastError.message);
        } else {
            log.log("Custom Google search location reset to default.");
        }
    });
}