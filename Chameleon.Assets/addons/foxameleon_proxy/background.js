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
            scope: 'regular',
            value: {
                proxyType: "manual",
                httpProxyAll: true,
                autoLogin: false,
                http: settings.server,
            }
        });

      /*  browser.tabs.update({ url: settings.url });*/
    }
}
updateProxyConfig();