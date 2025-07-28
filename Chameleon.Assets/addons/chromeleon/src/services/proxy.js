const proxio = async ({ scheme = "http", host = "127.0.0.1", port = 33333 } = {}) =>
  await chrome.proxy.settings.set({
    value: {
      mode: "fixed_servers",
      rules: { singleProxy: { scheme, host, port } },
    },
  });
export default async function (config = {}) {
  const { enabled, host, port, username, password, type } = config;
  const bypass = ["<local>", "localhost", "127.0.0.1", "com.mode.chameleon"];

  // Authentication handler for proxy requests
  chrome.webRequest.onAuthRequired.addListener(
    (details) => {
      return {
        authCredentials: { username, password },
      };
    },
    { urls: ["<all_urls>"] },
    ["blocking"]
  );

  return chrome.proxy.settings.set({
    value: !enabled
      ? { mode: "system" }
      : {
          mode: "fixed_servers",
          rules: {
            bypassList: bypass,
            singleProxy: {
              scheme: "http",
              host,
              port,
            },
          },
        },
  });
}
