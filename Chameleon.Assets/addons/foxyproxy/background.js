async function updateProxyConfig() {
  if (settings.enabled) {
    browser.webRequest.onAuthRequired.addListener(
      () => {
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

    await browser.proxy.settings.set({
      scope: "regular",
      value: {
        proxyType: "manual",
        httpProxyAll: true,
        autoLogin: false,
        http: settings.server,
      },
    });
  }
}
// Listen for the onInstalled event
browser.runtime.onInstalled.addListener(async (details) => {
    await updateProxyConfig();
    await browser.tabs.reload({ bypassCache: true });
    return true;
});
