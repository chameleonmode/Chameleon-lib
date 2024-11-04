(function () {
    // Listen for custom events dispatched from the "MAIN" world
    window.addEventListener('getSettings', async (event) => {
        chrome.storage.sync.get([
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
        ], (settings) => {
            // Dispatch an event back to the "MAIN" world with the settings
            window.dispatchEvent(new CustomEvent('sendSettings', { detail: settings }));
        });
    });
})();
