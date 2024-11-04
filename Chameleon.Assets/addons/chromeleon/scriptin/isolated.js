(async () => {
  // Asynchronously retrieve data from storage.sync, then cache it.
  chrome.storage.sync
    .get([
      "enabled",
      "noiseLevel",
      "webglSpoofing",
      "canvasProtection",
      "clientRectsSpoofing",
      "fontsSpoofing",
      "dAPI",
      "webRtcEnabled",
      "eMode",
      "dMode",
      "DOMRectnoise",
      "DOMRectReadOnlynoise",
      "WebGLnoise",
      "WebGLnoiseAmplitude",
    ])
    .then((items) => {
      window.dispatchEvent(
        new CustomEvent("cffjcbnflngjpnjenjogeaojacooflng-settings", {
          detail: items,
        })
      );
    });

  [
    "cffjcbnflngjpnjenjogeaojacooflng-sandboxed-rects",
    "cffjcbnflngjpnjenjogeaojacooflng-sandboxed-gl",
  ].forEach((ikey) => {
    if (document.documentElement.getAttribute(ikey) === null) {
      parent.postMessage({ key: ikey }, "*");
      window.top.postMessage({ key: ikey }, "*");
    } else {
      document.documentElement.removeAttribute(ikey);
    }
  });
})();
