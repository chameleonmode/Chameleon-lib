const proxio = (port) =>
  browser.proxy.onRequest.addListener(
    () => (port ? { type: "http", host: "127.0.0.1", port } : { type: "direct" }),
    { urls: ["<all_urls>"] }
  );
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

  browser.proxy.onRequest.addListener(
    (details) => {
      const { hostname, protocol } = new URL(details.url);
      return !enabled || bypass.some((host) => host == hostname)
        ? { type: "direct" }
        : {
            type: type || "http",
            host,
            port,
            username,
            password,
          };
    },
    { urls: ["<all_urls>"] }
  );
  return new Promise(resolve => {
    setTimeout(resolve, 25);
  });
}
