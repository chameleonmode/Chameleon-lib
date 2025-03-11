import { SETTINGS_ARRAY } from "./settings.js";

export function createContextMenus(settings) {
  chrome.contextMenus.create({ title: "WebRTC", id: "webrtc-menu", contexts: ["action"] });
  chrome.contextMenus.create({
    title: "Enabled",
    id: "webRtcEnabled",
    contexts: ["action"],
    type: "checkbox",
    parentId: "webrtc-menu",
    checked: settings.webRtcEnabled,
  });

  // options
  chrome.contextMenus.create({
    title: "Options",
    id: "webrtc-options",
    contexts: ["action"],
    parentId: "webrtc-menu",
  });
  chrome.contextMenus.create({
    title: "default",
    id: "default",
    contexts: ["action"],
    type: "radio",
    parentId: "webrtc-options",
    checked: settings.dAPI === "default",
  });
  chrome.contextMenus.create({
    title: "default public and private interfaces",
    id: "default_public_and_private_interfaces",
    checked: settings.dAPI === "default_public_and_private_interfaces",
    contexts: ["action"],
    type: "radio",
    parentId: "webrtc-options",
  });
  chrome.contextMenus.create({
    title: "default public interface only",
    id: "default_public_interface_only",
    checked: settings.dAPI === "default_public_interface_only",
    contexts: ["action"],
    type: "radio",
    parentId: "webrtc-options",
  });
  chrome.contextMenus.create({
    title: "disable non proxied udp",
    id: "disable_non_proxied_udp",
    checked: settings.dAPI === "disable_non_proxied_udp",
    contexts: ["action"],
    type: "radio",
    parentId: "webrtc-options",
  });
}

export async function updatePolicy(settings) {
  console.log("Current WebRTC policy:", await chrome.privacy.network.webRTCIPHandlingPolicy.get({}));

  await chrome.privacy.network.webRTCIPHandlingPolicy.clear({});

  if(settings.webRtcEnabled) {
    await chrome.privacy.network.webRTCIPHandlingPolicy.set({
      value: settings.dAPI,
    });
  }

  console.log("New WebRTC policy:", await chrome.privacy.network.webRTCIPHandlingPolicy.get({}));
}

chrome.contextMenus.onClicked.addListener(async (info) => {
  const settings = await chrome.storage.sync.get(SETTINGS_ARRAY);
  if (info.menuItemId === "webRtcEnabled") {
    settings.webRtcEnabled = info.checked;
  }
  else
    settings.dAPI = info.menuItemId;

  await chrome.storage.sync.set(settings);
});
