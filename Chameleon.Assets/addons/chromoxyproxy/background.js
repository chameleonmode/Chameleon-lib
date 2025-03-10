import { settings } from './settings.js';

chrome.webRequest.onAuthRequired.addListener((_) => {
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

chrome.runtime.onInstalled.addListener(async () => {
  let tabs = await chrome.tabs.query({});
  for (let tab of tabs) {
    await chrome.tabs.discard(tab.id);
  }
  await updateProxy();
  //const extensionId = "cffjcbnflngjpnjenjogeaojacooflng";
  //await chrome.management.setEnabled(extensionId, false);
  //await chrome.management.setEnabled(extensionId, true);
  //chrome.management.launchApp(extensionId);
});

async function updateProxy() {
  try {
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
    await chrome.proxy.settings.set({ value: proxyConfig, scope: "regular" });
    const [tab] = await chrome.tabs.query({
      active: true,
      currentWindow: true,
    });
    if (tab) {
      await chrome.tabs.update(tab.id, { url: settings.url });
    }
  } catch (error) {
    console.error(`Error updating proxy settings: ${error.message}`);
  }
}
