import App from "../app.js";
import { log } from "./logger.js";

// export const settings = () => {
//   return {
//     value: {
//       proxyType: !!App.config.proxy.enabled ? "manual" : "none",
//       http: App.config.proxy.server,
//       https: App.config.proxy.server,
//       passthrough: "localhost, 127.0.0.1",
//     },
//   };
// };

// // Set up the proxy
// export default async function () {
//   log.info("Proxy config:", App.config.proxy);

    // await browser.proxy.settings.set({
    //   value: {
    //     proxyType: !!App.config.proxy.enabled ? "manual" : "none",
    //     http: App.config.proxy.server,
    //     https: App.config.proxy.server,
    //     no_proxies_on: ["localhost", "127.0.0.1"]
    //   }
    // });

//   //   browser.proxy.settings.set({
//   //     value: {
//   //       proxyType: "manual",
//   //       http: "proxy.example.com:8080",
//   //       https: "proxy.example.com:8080",
//   //       no_proxies_on: ["localhost", "127.0.0.1"]
//   //     }
//   //   }).then(async () => {
//   //     console.log("Proxy settings applied successfully");
//   //     await Promise.all(
//   //         (
//   //           await chrome.tabs.query({ url: ["http://*/*", "http://*/*"] })
//   //         ).map((tab) => chrome.tabs.reload(tab.id))
//   //       );
//   //   }).catch(error => {
//   //     console.error("Error applying proxy settings:", error);
//   //   });

//   await Promise.all(
//     (
//       await chrome.tabs.query({ url: ["http://*/*", "http://*/*"] })
//     ).map((tab) => chrome.tabs.reload(tab.id))
//   );
// }

export async function proxy() {
  // Authentication handler for proxy requests
  chrome.webRequest.onAuthRequired.addListener(
    (details) => {
      return {
        authCredentials: {
          username: App.config.proxy.username,
          password: App.config.proxy.password,
        },
      };
    },
    { urls: ["<all_urls>"] },
    ["blocking"]
  );

  browser.proxy.onRequest.addListener(
    (details) => {
      const { hostname, protocol } = new URL(details.url);
      if (
        !App.config.proxy.enabled ||
        ["localhost", "127.0.0.1", "com.mode.chameleon"].some((bypass) => bypass == hostname)
      ) {
        return { type: "direct" };
      }

      return {
        type: App.config.proxy.type || "http", // Support different proxy types
        host: App.config.proxy.host,
        port: App.config.proxy.port,
        username: App.config.proxy.username,
        password: App.config.proxy.password,
        //   proxyDNS: App.config.proxy.proxyDNS || false, // DNS through proxy
        //   failoverTimeout: App.config.proxy.failoverTimeout || 5 // Timeout in seconds
      };
    },
    { urls: ["<all_urls>"] }
  );

  await browser.proxy.settings.clear({});
  await browser.proxy.settings.set({
    value: {
      proxyType: !!App.config.proxy.enabled ? "manual" : "none",
      http: App.config.proxy.server,
      https: App.config.proxy.server,
      no_proxies_on: ["localhost", "127.0.0.1", "com.mode.chameleon"],
    },
  });
}

// // Uncomment this block if you want to handle proxy settings manually per request
// // on auth requred is still necessary
// browser.proxy.onRequest.addListener(
//   (details) => {
//     const { hostname, protocol } = new URL(details.url);
//     if (
//       !App.config.proxy.enabled ||
//       ["localhost", "127.0.0.1", "com.mode.chameleon"].some((bypass) => bypass == hostname)
//     ) {
//       return { type: "direct" };
//     }

//     log.info("Proxy request:", details);
//     return {
//       type: App.config.proxy.type || "http", // Support different proxy types
//       host: App.config.proxy.host,
//       port: App.config.proxy.port,
//       username: App.config.proxy.username,
//       password: App.config.proxy.password,
//       //   proxyDNS: App.config.proxy.proxyDNS || false, // DNS through proxy
//       //   failoverTimeout: App.config.proxy.failoverTimeout || 5 // Timeout in seconds
//     };
//   },
//   { urls: ["<all_urls>"] }
// );