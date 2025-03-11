// app.js for Chrome Extension

// Object to track sessions launched by the app
export const AppLaunchManager = {
  launchedSessions: {},

  // Register a new session launched by the app
  registerSession: function (sessionId, params = {}) {
    console.log(`Registering app-launched session: ${sessionId}`);

    this.launchedSessions[sessionId] = {
      sessionId: sessionId,
      appInstanceId: params.appInstanceId || "unknown",
      timestamp: Date.now(),
      source: "app",
      active: true,
      params: params,
    };

    // Store in persistent storage
    this.saveSessionsToStorage();

    // Notify any listeners about this new session
    this.notifySessionRegistered(sessionId);

    return true;
  },

  // Load sessions from persistent storage
  loadSessionsFromStorage: async function () {
    const result = await chrome.storage.local.get(["launchedSessions"]);
    if (result && result.launchedSessions) {
      this.launchedSessions = result.launchedSessions;
      console.log(`Loaded ${Object.keys(this.launchedSessions).length} launched sessions from storage`);
    }
  },

  // Save sessions to persistent storage
  saveSessionsToStorage: function () {
    chrome.storage.local.set({ launchedSessions: this.launchedSessions });
  },

  // Check if a session was launched by the app
  isAppLaunchedSession: function (sessionId) {
    return !!this.launchedSessions[sessionId];
  },

  // Get session details
  getSessionDetails: function (sessionId) {
    return this.launchedSessions[sessionId] || null;
  },

  // Notify listeners about new session
  notifySessionRegistered: function (sessionId) {
    // You could implement custom event dispatch here if needed
    console.log(`Session ${sessionId} registered and ready`);
  },

  // Add the session info to API requests
  addSessionToRequest: function (data, sessionId) {
    if (!sessionId || !this.isAppLaunchedSession(sessionId)) {
      return data;
    }

    const session = this.getSessionDetails(sessionId);

    return {
      ...data,
      _launchedByApp: true,
      _sessionId: sessionId,
      _appInstanceId: session.appInstanceId,
    };
  },
};

// The App object is used to communicate with the app server
export const App = {
  candidatePorts: [5016, 5031, 7034, 8032, 8084, 9027],
  port: null,
  sessionId: null,

  // Set the current session ID
  setSessionId: function (id) {
    this.sessionId = id;
  },

  // Find the app server
  discoverServer: async function () {
    console.log("Attempting to discover app...");

    // Try each port in the list
    for (const port of this.candidatePorts) {
      try {
        const response = await fetch(`http://127.0.0.1:${port}/ping`, {
          signal: AbortSignal.timeout(300), // 300ms timeout
        });

        if (response.ok) {
          console.log(`Found app on port ${port}`);
          this.port = port;
          return true;
        }
      } catch (error) {
        // Continue to next port
      }
    }

    console.log("app not found on any expected port");
    return false;
  },

  // Send data to the app
  sendData: async function (data) {
    if (!this.port && !(await this.discoverServer())) {
      throw new Error("app not found");
    }

    try {
      const session = AppLaunchManager.getSessionDetails(this.sessionId);  
      const response = await fetch(`http://localhost:${this.port}/app/data`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          // Add session headers if available
          ...(session
            ? {
                "X-Session-ID": this.sessionId,
                "X-Instance-ID": session.appInstanceId
              }
            : {}),
        },
        body: JSON.stringify(data),
      });

      if (!response.ok) {
        throw new Error(`Server error: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      this.port = null;
      throw error;
    }
  },

  // Get app state
  getState: async function () {
    // Make sure we're connected first
    if (!this.port && !(await this.discoverServer())) {
      throw new Error("app not found");
    }

    try {
      const response = await fetch(`http://127.0.0.1:${this.port}/api/state`);

      if (!response.ok) {
        throw new Error(`Server error: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      // If connection fails, reset port and try discovery on next attempt
      this.port = null;
      throw error;
    }
  },

  // Check if the app is running
  isRunning: async function () {
    return await this.discoverServer();
  },
};
