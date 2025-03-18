// app.js for Chrome Extension to communicate with the app server
export const App = {
  server: null,
  port: null,
  config: {
    enabled: true,
    log: 'all',
    dApi: 'disable_non_proxied_udp',
  },
  session: {
    sessionId: null,
    instanceId: null,
  },
  launchedSessions: {},

  // Startup
  startup: async function () {
    const { session, launchedSessions, config } = await chrome.storage.local.get(["session", "launchedSessions", "config"]);
    this.config = config || this.config;
    this.session = session || this.session;
    this.launchedSessions = launchedSessions || this.launchedSessions;
    return true;
  },

  // Register a new session launched by the app
  initialize: async function (sessionId, instanceId) {
    if (!(await this.discoverServer())) return false

    const response = await this.sendData({ type: "init" });
    this.config = response.config;
    this.session = {
      sessionId,
      instanceId
    };
    this.launchedSessions[sessionId] = this.session;
    await chrome.storage.local.set({ config: this.config, session: this.session, launchedSessions: this.launchedSessions });
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
