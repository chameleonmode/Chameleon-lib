// app.js for Chrome Extension to communicate with the app server

/**
 * Observer pattern implementation
 * Allows subscribers to register for notifications when events occur
 */
class EventObserver {
  constructor() {
    // Object to store event types and their callback functions
    this.observers = {};
  }

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
  }

  /**
   * Unsubscribe from an event
   * @param {string} event - The event type to unsubscribe from
   * @param {function} callback - The function to remove from subscribers
   */
  unsubscribe(event, callback) {
    if (this.observers[event]) {
      this.observers[event] = this.observers[event].filter((subscriber) => subscriber !== callback);
    }
  }

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
  }
}

// Default configuration for the app
const App = {
  eventSystem: new EventObserver(),
  server: null,
  port: null,
  config: {
    enabled: true,
    sync: true,
    log: "all",
    noise: "mid",
    noises: ["nano", "mini", "low", "mid", "bold", "high", "ultra", "super", "max"],
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
      start: "https://example.com/start",
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
  },
  session: {
    sessionId: null,
    instanceId: null,
  },
  launchedSessions: {},

  // Startup
  startup: async function () {
    const { session, launchedSessions, config } = await chrome.storage.local.get([
      "session",
      "launchedSessions",
      "config",
    ]);
    this.session = session || this.session;
    this.launchedSessions = launchedSessions || this.launchedSessions;

    if (config) {
      config["noises"] = this.config["noises"];
      for (const [key, value] of Object.entries(config)) {
        if (typeof value === "object" && !Array.isArray(value)) {
          this.config[key] = { ...this.config[key], ...value };
        } else {
          this.config[key] = value;
        }
      }
    }

    return {
      session: this.session,
      launchedSessions: this.launchedSessions,
      config: this.config,
    };
  },

  // Register a new session launched by the app
  initialize: async function (sessionId, instanceId) {
    if (!(await this.discoverServer())) return false;
    this.session = { sessionId, instanceId };
    this.launchedSessions[sessionId] = this.session;

    const sync = await chrome.storage.sync.get(["config"]);
    if (sync.config) {
      this.config = sync.config.sync ? { ...this.config, ...sync.config } : { ...sync.config, ...this.config };
    }

    const response = await this.sendData({ type: "init" });
    for (const [key, value] of Object.entries(response.config)) {
      this.config[key] = { ...this.config[key], ...value };
    }

    await chrome.storage.local.set({
      session: this.session,
      launchedSessions: this.launchedSessions,
      config: this.config,
    });

    return true;
  },

  // Find the app server
  discoverServer: async function () {
    // Try each port in the list
    for (const port of [3663, 3993, 3693, 3963, 6969, 6996, 9669, 9696]) {
      try {
        const url = `http://127.0.0.1:${port}`;
        const response = await fetch(`${url}/ping`, {
          signal: AbortSignal.timeout(500), // 500ms timeout
        });
        if (!response.ok) continue;

        this.port = port;
        this.server = url;
        return true;
      } catch (error) {
        // Continue to next port
        console.error(error);
      }
    }
    return false;
  },

  // Send data to the app
  sendData: async function (data) {
    if (!this.server && !(await this.discoverServer())) {
      throw new Error("app not found");
    }

    const response = await fetch(`${this.server}/app/data`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Session-ID": this.session.sessionId,
        "X-Instance-ID": this.session.instanceId,
      },
      body: JSON.stringify(data),
    });
    return await response.json();
  },

  // Get app state
  getAppState: async function () {
    if (!this.server && !(await this.discoverServer())) {
      throw new Error("app not found");
    }

    const response = await fetch(`${this.server}/app/state`);
    return await response.json();
  },
};

export default App;
