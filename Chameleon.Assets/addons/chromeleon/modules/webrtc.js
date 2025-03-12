// Version: 2.0
chrome.storage.onChanged.addListener(async (changes, namespace) => {
  const { webRtcEnabled, dAPI } = changes;
  if (!webRtcEnabled && !dAPI) return;
  
  await chrome.privacy.network.webRTCIPHandlingPolicy.clear({});

  if (webRtcEnabled.newValue) {
    await chrome.privacy.network.webRTCIPHandlingPolicy.set({
      value: dAPI.newValue,
    });
  }
})

chrome.contextMenus.onClicked.addListener(async (info) => {
  if (info.menuItemId === "webRtcEnabled") {
    await chrome.storage.sync.set({ webRtcEnabled: info.checked });
  } else await chrome.storage.sync.set({ dAPI: info.menuItemId });
});
