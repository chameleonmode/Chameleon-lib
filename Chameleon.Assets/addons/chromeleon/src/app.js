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

// Find the app server
const discoverServer = async () => {
	await new Promise((resolve) => setTimeout(resolve, 500)); // Wait for .5 second
	// Try each port in the list
	for (const port of [3663, 3993, 3693, 3963, 6969, 6996, 9669, 9696]) {
		try {
			const url = `http://127.0.0.1:${port}`;
			const response = await fetch(`${url}/ping`, {
				signal: AbortSignal.timeout(5000), // 5000ms timeout
			});
			return { port, url };
		} catch (error) {
			// Continue to next port
			console.error(error);
		}
	}
	return discoverServer();
}
const App = {
	server: null,
	port: null,
	remoteDebugPort: undefined,
	config,
	session: {
		sessionId: null,
		instanceId: null,
	},
	launchedSessions: {},
	// Object to store event types and their callback functions
	observers: {},

	onUpdated: async () => {
		if(App.server && App.remoteDebugPort){
			return await App.sendData({ type: "port", port: App.remoteDebugPort });
		}
	},

	// Startup
	startup: async function () {
		const { session, launchedSessions } = await chrome.storage.local.get(["session", "launchedSessions"]);
		this.session = session || this.session;
		this.launchedSessions = launchedSessions || this.launchedSessions;

		const local = await chrome.storage.local.get(["config"]);
		if (local.config) {
			for (const [key, value] of Object.entries(local.config)) {
				if (typeof value === "object" && !Array.isArray(value)) {
					this.config[key] = { ...this.config[key], ...value };
				} else {
					this.config[key] = value;
				}
			}
		}

		const { noise, hash } = await chrome.storage.local.get(["noise", "hash"]);
		if (!noise || !hash) {
			this.config.noise = noises[Math.floor(Math.random() * noises.length)];
			this.config.hash = Math.random() * (100 - 1.5) + 1.5; // Random number between 1.5 and 100
			await chrome.storage.local.set({ config: App.config });
			const sync = await chrome.storage.sync.get(["config"]);
			if (sync.config) {
				sync.config.noise = this.config.noise;
				sync.config.hash = this.config.hash;
			  await chrome.storage.local.set({ config: sync.config });
			}
			await chrome.storage.local.set({ noise: this.config.noise, hash: this.config.hash });
		} else {
			this.config.noise = noise;
			this.config.hash = hash;
		}
    // this.config.noise = noises[Math.floor(Math.random() * noises.length)];
    // this.config.hash = Math.random() * (100 - 1.5) + 1.5; // Random number between 1.5 and 100
		// await chrome.storage.sync.set({ config: App.config });
		// await chrome.storage.local.set({ config: App.config });

		return {
			session: this.session,
			launchedSessions: this.launchedSessions,
			config: this.config,
		};
	},

	// Register a new session launched by the app
	initialize: async function (sessionId, instanceId, data) {
		this.session = { sessionId, instanceId };
		this.launchedSessions[sessionId] = this.session;

		const sync = await chrome.storage.sync.get(["config"]);
		if (sync.config) {
			this.config = { ...this.config, ...sync.config };
		}

		const { config, port } = await this.sendData({ type: "init" });
		this.remoteDebugPort = port;
		const response = data || config;
		for (const [key, value] of Object.entries(response)) {
			this.config[key] =
				this.config.sync || key === "proxy"
					? { ...this.config[key], ...value }
					: { ...value, ...this.config[key] };
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
		const { port, url } = await discoverServer();
		this.server = url;
		this.port = port;
		return true;
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

export default App;
