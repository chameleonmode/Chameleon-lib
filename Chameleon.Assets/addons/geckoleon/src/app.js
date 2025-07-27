export const noises = ["nano", "mini", "low", "mid", "bold", "high", "ultra", "super", "max"];
export const config = {
  enabled: true,
  sync: true,
  log: "all",
  noise: "mid",
  hash: 0.50293784,
  bypass: ["*://example.com/*", "example.com"],
  history: [],
  dAPI: "disable_non_proxied_udp",
  proxy: {
    enabled: false,
    type: "http",
    server: "http://host:port",
    host: "host",
    port: 8080,
    username: "username",
    password: "password",
  },
  urls: {
    start: "about:blank",
    homePages: ["https://example.com/home", "https://example.com/dashboard"],
  },
  tz: {
    enabled: true,
    random: false,
    system: false,
    zone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    locale: Intl.DateTimeFormat().resolvedOptions().locale,
  },
  geo: {
    enabled: true,
    random: false,
    lat: 40.7128,
    lon: -74.006,
    accuracy: 64.0999,
  },
  canvas: {
    enabled: true,
    random: false,
  },
  webgl: {
    enabled: true,
    random: false,
  },
  rects: {
    enabled: true,
    random: false,
  },
  fonts: {
    enabled: true,
    random: false,
  },
  audio: {
    enabled: true,
    random: false,
  },
  navi: {
    enabled: true,
    random: false,
    os: "default",
  },
};
const app = {
  name: "Geckoleon",
  config,
  observers: {},
  state: { loaded: false, server: null, port: null, tabId: null },
  session: { sessionId: null, instanceId: null },

  // Find the app server
  async discoverServer() {
    while (!this.state.server) {
      // Try each port in the list
      for (const port of [3663, 3993, 3693, 3963]) {
        //, 6969, 6996, 9669, 9696]) {
        const url = `http://127.0.0.1:${port}`;
        for (let i = 0; i < 2; i++) {
          try {
            await fetch(`${url}/ping`, { signal: AbortSignal.timeout(300) });
            this.state.server = url;
            break; // Exit the retry loop if successful
          } catch (error) {
            console.error(`Port ${port} failed on attempt ${i + 1}:`, error);
            await new Promise((resolve) => setTimeout(resolve, 600)); // Wait for 0.6 second
          }
        }
        if (this.state.server) break; // Exit if server is already found
        else await new Promise((resolve) => setTimeout(resolve, 600)); // Wait for 0.6 second
      }
    }
  },

  // Send data to the app
  async sendData(data, { instanceId = this.session.instanceId, sessionId = this.session.sessionId } = {}) {
    const response = await fetch(`${this.state.server}/app/data`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Session-ID": sessionId,
        "X-Instance-ID": instanceId,
      },
      body: JSON.stringify(data),
    });
    return await response.json();
  },

  /**
   * Subscribe to an event
   * @param {string} event - The event type to subscribe to
   * @param {function} callback - The function to call when event occurs
   * @returns {function} Unsubscribe function
   */
  subscribe(event, callback) {
    // Create the event array if it doesn't exist
    if (!this.observers[event]) {
      this.observers[event] = [];
    }

    // Add the callback to the event's observers
    this.observers[event].push(callback);

    // Return an unsubscribe function
    return () => {
      this.observers[event] = this.observers[event].filter((subscriber) => subscriber !== callback);
    };
  },

  /**
   * Unsubscribe from an event
   * @param {string} event - The event type to unsubscribe from
   * @param {function} callback - The function to remove from subscribers
   */
  unsubscribe(event, callback) {
    if (this.observers[event]) {
      this.observers[event] = this.observers[event].filter((subscriber) => subscriber !== callback);
    }
  },

  /**
   * Notify all subscribers of an event
   * @param {string} event - The event type to notify about
   * @param {*} data - The data to pass to subscribers
   */
  notify(event, data) {
    if (this.observers[event]) {
      this.observers[event].forEach((callback) => {
        callback(data);
      });
    }
  },
};

export default app;
