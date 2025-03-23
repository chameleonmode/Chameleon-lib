// app.js for Chrome Extension to communicate with the app server

// Default configuration for the app
const App = {
  server: null,
  port: null,
  config: {
    enabled: true,
    log: "all",
    noise: "medium",
    noises: ["micro", "mini", "low", "medium", "bold", "high", "ultra", "super", "max"],
    bypass: [],
    history: [],
    dAPI: "disable_non_proxied_udp",
    urls: {
      start: "https://example.com/start",
    },
    tz: {
      enabled: true,
      random: false,
      zone: "Pacific/Honolulu",
      locale: "en-US",
      useSystem: false,
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
    for (const [key, value] of Object.entries(config)) {
      if (typeof value === "object" && !Array.isArray(value)) {
        this.config[key] = { ...this.config[key], ...value };
      } else {
        this.config[key] = value;
      }
    }
    this.session = session || this.session;
    this.launchedSessions = launchedSessions || this.launchedSessions;

    return {
      session: this.session,
      launchedSessions: this.launchedSessions,
      config: this.config,
    };
  },

  // Register a new session launched by the app
  initialize: async function (sessionId, instanceId) {
    if (!(await this.discoverServer())) return undefined;
    this.session = { sessionId, instanceId };
    this.launchedSessions[sessionId] = this.session;

    const sync = await chrome.storage.sync.get(["config"]);
    if (sync.config) {
      this.config = { ...this.config, ...sync.config };
    }

    const response = await this.sendData({ type: "init" });
    for (const [key, value] of Object.entries(response.config)) {
      this.config[key] = { ...this.config[key], ...value };
    }

    return await chrome.storage.local.set({
      session: this.session,
      launchedSessions: this.launchedSessions,
      config: this.config,
    });
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
