import { settings } from "./settings.js";
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
});

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
