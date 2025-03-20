import { settings } from "./settings.js";

// Configure proxy settings
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

const setProxy = browser.proxy.settings.set({ value: proxyConfig, scope: "regular" });
// Apply proxy settings when extension is installed or updated
browser.runtime.onInstalled.addListener(async () => {
  await setProxy;
});

// Handle authentication requests
browser.webRequest.onAuthRequired.addListener(
  (details) => {
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
