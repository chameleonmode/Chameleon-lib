// Version: 2.0
export const policies = {
  default: { title: "default", id: "default" },
  default_public_and_private_interfaces: {
    title: "default public and private interfaces",
    id: "default_public_and_private_interfaces",
  },
  default_public_interface_only: {
    title: "default public interface only",
    id: "default_public_interface_only",
  },
  disable_non_proxied_udp: {
    title: "disable non proxied udp",
    id: "disable_non_proxied_udp",
  },
};

chrome.storage.onChanged.addListener(async (changes, namespace) => {
  if (!changes.dAPI) return;

  await chrome.privacy.network.webRTCIPHandlingPolicy.clear({});

  await chrome.privacy.network.webRTCIPHandlingPolicy.set({
    value: changes.dAPI.newValue,
  });
});

chrome.contextMenus.onClicked.addListener(async (info) => {
  await chrome.storage.sync.set({ dAPI: info.menuItemId });
});
