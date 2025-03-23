import { settings } from "./settings.js";
chrome.webRequest.onAuthRequired.addListener(
  (_) => {
    return {
      authCredentials: {
        username: settings.username,
        password: settings.password,
      },
    };
  },
  { urls: ["<all_urls>"] },
  ["blocking"]
);

const proxyConfig = settings.enabled
  ? {
      mode: "fixed_servers",
      rules: {
        bypassList: ["<local>"],
        singleProxy: {
          scheme: "http",
          host: settings.host,
          port: settings.port,
        },
      },
    }
  : { mode: "system" };
const promise = chrome.proxy.settings.set({ value: proxyConfig, scope: "regular" });
chrome.runtime.onInstalled.addListener(async () => {
  await promise;
  await chrome.tabs.reload({ bypassCache: true });
  return true;
});

