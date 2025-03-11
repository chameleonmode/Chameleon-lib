const actions = {
  TZ_RESET: "tz_reset",
  GEO_RESET: "geo_reset",
};

chrome.runtime.onMessage.addListener(async function (request, sender, sendResponse) {
  if (request.action === actions.TZ_RESET || request.action === actions.GEO_RESET) {
    const settings = await chrome.storage.sync.get(["lat", "lon"]);
    const promptDictionary = {
      [actions.TZ_RESET]: {
        promptText:
          'Enter a "timezone" value. Use https://www.timeanddate.com/time/map/ to find these values',
        defaultInput: settings.timezone,
      },
      [actions.GEO_RESET]: {
        promptText:
          'Enter a "latitude" and "longitude" separated by a comma. Use https://www.latlong.net/ to find these values',
        defaultInput: `${settings.lat}, ${settings.lon}`,
      },
    };
    let { promptText, defaultInput } = promptDictionary[request.action];
    if (request.action === actions.GEO_RESET) defaultInput = `${settings.lat}, ${settings.lon}`;

    const userInput = prompt(promptText, defaultInput);
    if (userInput === null) {
      sendResponse({ status: "cancelled" });
    } else {
      sendResponse({ status: "success", userInput: userInput });
    }
  }
});
