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
  config,
  observers: {},
  state: { loaded: false, server: null, port: null },
  session: { sessionId: null, instanceId: null },

  // Find the app server
  async discoverServer() {
    while (!this.state.server) {
      await new Promise((resolve) => setTimeout(resolve, 900)); // Wait for 0.9 second
      // Try each port in the list
      for (const port of [3663, 3993, 3693, 3963, 6969, 6996, 9669, 9696]) {
        const url = `http://127.0.0.1:${port}`;
        try {
          // Try the first attempt
          try {
            await fetch(`${url}/ping`, { signal: AbortSignal.timeout(600) });
          } catch (error) {
            // Try a second attempt before giving up on this port
            await new Promise((resolve) => setTimeout(resolve, 900)); // Wait for 0.9 second
            await fetch(`${url}/ping`, { signal: AbortSignal.timeout(600) });
          }
          this.state.server = url;
          break; // Exit the port loop if successful
        } catch (error) {
          // Continue to next port if both attempts fail
          console.error(`Port ${port} failed after two attempts:`, error);
        }
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
