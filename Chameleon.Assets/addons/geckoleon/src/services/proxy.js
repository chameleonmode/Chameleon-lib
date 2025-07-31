export const proxio = async ({
  enabled = true,
  scheme = "http",
  host = "127.0.0.1",
  port = 33333,
  username = null,
  password = null,
} = {}) => {
  if (enabled) {
    if (username && password)
      await chrome.webRequest.onAuthRequired.addListener(
        (_) => ({ authCredentials: { username, password } }),
        { urls: ["<all_urls>"] },
        ["blocking"]
      );
    await browser.proxy.onRequest.addListener((_) => ({ type: scheme, host, port, username, password }), {
      urls: ["<all_urls>"],
    });
  } else {
    await browser.proxy.onRequest.addListener((_) => ({ type: "direct" }), { urls: ["<all_urls>"] });
  }
};
