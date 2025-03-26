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

  browser.management
    .get("geckoleon@chameleonmode.com")
    .then((extensionInfo) => {
      console.log("Extension UUID:", extensionInfo);
      // moz-extension://f58c1d0c-2715-468a-9039-74f6dce80d07/*
      // Open a new tab with the extension's host permissions remove the last *
      const base = extensionInfo.hostPermissions[0].slice(0, -1);
      browser.tabs.create({
        url: `${base}data/web/register.html?instanceId=${settings.instanceId}&sessionId=${settings.sessionId}`,
      });
      return extensionInfo;
    })
    .catch((error) => {
      console.error("Error fetching extension info:", error);
    });
});
