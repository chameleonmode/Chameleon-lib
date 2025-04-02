// Version: 2.0
import App from "../app.js";

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
  if (changes.config && changes.config.oldValue && changes.config.oldValue.dAPI === changes.config.newValue.dAPI) return;

  await chrome.privacy.network.webRTCIPHandlingPolicy.clear({});
  await chrome.privacy.network.webRTCIPHandlingPolicy.set({
    value: App.config.dAPI,
  });
});