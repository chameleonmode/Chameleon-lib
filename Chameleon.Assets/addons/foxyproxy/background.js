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
    }
}
updateProxyConfig();
//// Listen for the onInstalled event
//browser.runtime.onInstalled.addListener(async (details) => {
//    if (details.reason === "install") {
//        // Perform tasks for first-time installation
//        console.log("Extension installed for the first time");
//    } else if (details.reason === "update") {
//        // Perform tasks for extension update
//        console.log("Extension updated to a new version");
//    } else if (details.reason === "browser_update") {
//        // Perform tasks for browser update
//        console.log("Browser updated to a new version");
//    }
//});
//var set = false;
//browser.webNavigation.onCommitted.addListener(async (details) => {
//    if (!set && details.frameId === 0) { // Ensures the script is only registered for the main frame
//        set = true;
//        await updateProxyConfig();
//        log.info("Injection script registered onCommitted");
//    }
//}, { url: [{ schemes: ["http", "https"] }] });
//browser.webRequest.onAuthRequired.addListener(
//    () => {
//        return {
//            authCredentials: {
//                username: settings.username,
//                password: settings.password,
//            },
//        };
//    },
//    { urls: ["<all_urls>"] },
//    ["blocking"]
//);