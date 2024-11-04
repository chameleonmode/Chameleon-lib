(async () => {
  const SETTINGS_ARRAY = [
      "enabled",
      "webglSpoofing",
      "canvasProtection",
      "clientRectsSpoofing",
      "fontsSpoofing",
      "geoSpoofing",
      "timezoneSpoofing",
      "dAPI",
      "webRtcEnabled",
      "randomizeTZ",
      "randomizeGeo",
      "noiseLevel",
      "eMode",
      "dMode",
      "timezone",
      "locale",
      "debug",
      "latitude",
      "longitude",
      "accuracy",
      "myIP",
      "bypass",
      "history",
  ];
  let settings = await chrome.storage.sync.get(SETTINGS_ARRAY);

  const win = {
    send(type, data) {
      // Relay settings to page context script
      window.postMessage({ type: type, data: data }, "*");
    },
    listen() {
      // Listen for messages from the main script
      window.addEventListener(
        "message",
        async function (e) {
          if (
            e.data &&
            e.data.type === "REQUEST_Chromeleon_DEFENDER_SETTINGS"
          ) { 
            e.preventDefault();
            e.stopPropagation();
            win.send("Chromeleon_DEFENDER_SETTINGS_RESPONSE", settings);
          }
        },
        false
      );
    },
  };
  win.listen();
})();
