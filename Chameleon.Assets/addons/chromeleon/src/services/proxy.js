export const proxio = async ({
  enabled = true,
  scheme = "http",
  host = "127.0.0.1",
  port = 33333,
  username = null,
  password = null,
} = {}) => {
  if (!enabled) await chrome.proxy.settings.set({ value: { mode: "system" } });
  else {
    if (username && password)
      await chrome.webRequest.onAuthRequired.addListener(
        (_) => ({ authCredentials: { username, password } }),
        { urls: ["<all_urls>"] },
        ["blocking"]
      );
    await chrome.proxy.settings.set({
      value: {
        mode: "fixed_servers",
        rules: { singleProxy: { scheme, host, port } },
      },
    });
  }
};
