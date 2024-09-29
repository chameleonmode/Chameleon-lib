function updateProxyConfig() {
    if (settings.enabled) {
        browser.proxy.settings.set({
            value: {
                proxyType: "manual",
                httpProxyAll: true,
                autoLogin: false,
                http: settings.server,
            },
            scope: 'regular'
        },
            async () => {
                let tabs = await browser.tabs.query({});
                browser.tabs.update({ url: settings.url });
            });

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
    }
}
updateProxyConfig();