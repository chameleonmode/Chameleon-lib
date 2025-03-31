// background.js
browser.runtime.onInstalled.addListener(() => {
  console.log("Geckoleon extension installed");

  // Initialize default configuration
  browser.storage.local.set({
    config: {
      noise: "mid",
      rects: { enabled: true, random: false },
      geo: {
        enabled: true,
        lat: 40.7128, // New York City
        lon: -74.006,
        random: 0.01, // Random factor (about 1km)
        accuracy: 100,
        bypass: [], // Optional list of URLs to bypass
      },
      tz: {
        enabled: true,
        timezone: "Pacific/Honolulu", // Default timezone
        locale: "en-US",
      },
    },
  });
});

// Function to inject our script into the page
async function injectNoiseScript(tabId) {
  try {
    const result = await browser.storage.local.get("config");
    const config = result.config || {
      noise: "max",
      rects: { enabled: true, random: false },
    };

    if (!config.rects.enabled) return;

    console.log("Injecting noise script with config", config);

    // Inject into ALL frames including iframes
    await browser.scripting.executeScript({
      world: "MAIN",
      injectImmediately: true,
      target: { tabId: tabId, allFrames: true },
      func: setupDirectOverride,
      args: [config.noise, config.rects.random],
    });
  } catch (error) {
    console.error("Error injecting script:", error);
  }
}

// This function directly overrides the getClientRects and getBoundingClientRect methods
function setupDirectOverride(noiseLevel, randomNoise) {
  try {
    console.log("Geckoleon: Direct method override starting", noiseLevel, randomNoise);

    // Map different noise levels
    const noises = {
      nano: Number.EPSILON * 5, // 2.22e-15.5
      mini: Number.EPSILON * 10, // 2.22e-15
      low: Number.EPSILON * 100, // 2.22e-14
      mid: Number.EPSILON * 1000, // 2.22e-13
      bold: Number.EPSILON * 10000, // 2.22e-12
      high: Number.EPSILON * 100000, // 2.22e-11
      ultra: Number.EPSILON * 1000000, // 2.22e-10
      super: 0.000000001, // 1e-9
      max: 0.00000001, // 1e-8
    };

    function getNoise() {
      if (randomNoise) {
        const keys = Object.keys(noises);
        return noises[keys[Math.floor(Math.random() * keys.length)]];
      }
      return noises[noiseLevel] || noises.max;
    }

    // Store original methods
    const original = {
      getBoundingClientRect: Element.prototype.getBoundingClientRect,
      getClientRects: Element.prototype.getClientRects,
    };

    // CRITICAL: Override the getClientRects method directly
    Element.prototype.getClientRects = function () {
      // Call the original method
      const originalRects = original.getClientRects.apply(this, arguments);

      // Apply noise to each rect
      const noise = getNoise();

      // Create a completely new DOMRectList-like object
      const noisyRects = {};

      // Set the length property
      Object.defineProperty(noisyRects, "length", {
        value: originalRects.length,
      });

      // Add the item method
      noisyRects.item = function (index) {
        if (index >= originalRects.length) return null;

        const rect = originalRects[index];
        return createNoisyRect(rect, noise);
      };

      // Add index access
      for (let i = 0; i < originalRects.length; i++) {
        noisyRects[i] = createNoisyRect(originalRects[i], noise);
      }

      // Make it iterable
      noisyRects[Symbol.iterator] = function* () {
        for (let i = 0; i < originalRects.length; i++) {
          yield noisyRects[i];
        }
      };

      if (Math.random() < 0.001) {
        console.log("Geckoleon: Applied noise to getClientRects", noise);
      }

      return noisyRects;
    };

    // Override getBoundingClientRect
    Element.prototype.getBoundingClientRect = function () {
      const originalRect = original.getBoundingClientRect.apply(this, arguments);
      const noise = getNoise();

      return createNoisyRect(originalRect, noise);
    };

    // Helper function to create a noisy rect
    function createNoisyRect(originalRect, noise) {
      // Create a new object with the same properties
      const noisyRect = {};

      // Apply noise to each property
      ["x", "y", "width", "height", "top", "right", "bottom", "left"].forEach((prop) => {
        if (prop in originalRect) {
          // Use Object.defineProperty to ensure the property appears in Object.keys
          Object.defineProperty(noisyRect, prop, {
            value: originalRect[prop] * (1 + noise),
            enumerable: true,
          });
        }
      });

      // Copy any other properties
      Object.getOwnPropertyNames(originalRect).forEach((prop) => {
        if (!noisyRect.hasOwnProperty(prop) && typeof originalRect[prop] !== "function") {
          noisyRect[prop] = originalRect[prop];
        }
      });

      // Copy methods
      ["toJSON"].forEach((method) => {
        if (typeof originalRect[method] === "function") {
          noisyRect[method] = originalRect[method].bind(originalRect);
        }
      });

      return noisyRect;
    }

    // Add a test function
    window.geckoleonTest = function () {
      // Create a test element
      const div = document.createElement("div");
      div.style.width = "100px";
      div.style.height = "100px";
      document.body.appendChild(div);

      // Test getClientRects
      const rects = div.getClientRects();
      console.log("Test getClientRects:", rects[0]);

      // Test getBoundingClientRect
      const rect = div.getBoundingClientRect();
      console.log("Test getBoundingClientRect:", rect);

      document.body.removeChild(div);
    };

    // Run a test
    setTimeout(window.geckoleonTest, 500);

    console.log("Geckoleon: Direct method override complete");
  } catch (error) {
    console.error("Geckoleon override error:", error);
  }
}

// Function to inject geolocation spoofing script
async function injectGeoSpoofingScript(tabId) {
  try {
    const result = await browser.storage.local.get("config");
    const config = result.config || {};

    if (!config.geo || !config.geo.enabled) {
      console.log("Geolocation spoofing disabled or not configured");
      return;
    }

    console.log("Injecting geolocation spoofing with config", config.geo);

    // Then inject our geo spoofing script
    await browser.scripting.executeScript({
      world: "MAIN",
      injectImmediately: true,
      target: { tabId: tabId, allFrames: true },
      func: setupGeoSpoofing,
      args: [config.geo],
    });
  } catch (error) {
    console.error("Error injecting geolocation spoofing:", error);
  }
}

// This function runs in the page context and overrides geolocation APIs
function setupGeoSpoofing(geoConfig) {
  try {
    console.log("Geckoleon: Geolocation spoofing starting", geoConfig);

    // Counter for watchPosition IDs
    let watchPositionId = 0;

    // Get spoofed coordinates
    function getSpoofedCoordinates() {
      const { lat, lon, random, accuracy } = geoConfig;

      // Apply randomization if configured
      const latitude = random ? lat + (Math.random() - 0.5) * random : lat;
      const longitude = random ? lon + (Math.random() - 0.5) * random : lon;

      return {
        latitude,
        longitude,
        accuracy: accuracy || 100,
      };
    }

    // Store original methods
    const originalGeolocation = {
      getCurrentPosition: navigator.geolocation.getCurrentPosition,
      watchPosition: navigator.geolocation.watchPosition,
      clearWatch: navigator.geolocation.clearWatch,
      query: navigator.permissions.query,
    };

    // Setup geolocation API overrides
    function setupGeoAPIs() {
      // Override getCurrentPosition
      navigator.geolocation.getCurrentPosition = function geckoleonGetCurrentPosition(
        success,
        error,
        options
      ) {
        console.log("Geckoleon: getCurrentPosition called");

        // Always try to use our spoofed coordinates first
        const coords = getSpoofedCoordinates();
        const position = {
          coords: {
            latitude: coords.latitude,
            longitude: coords.longitude,
            altitude: null,
            accuracy: coords.accuracy,
            altitudeAccuracy: null,
            heading: null,
            speed: null,
          },
          timestamp: Date.now(),
        };

        // Call success callback asynchronously
        setTimeout(() => {
          if (typeof success === "function") {
            success(position);
          }
        }, 50);

        // Log occasionally
        if (Math.random() < 0.1) {
          console.log("Geckoleon: Spoofed location", coords.latitude, coords.longitude);
        }
      };

      // Override watchPosition
      navigator.geolocation.watchPosition = function geckoleonWatchPosition(success, error, options) {
        console.log("Geckoleon: watchPosition called");

        // Call getCurrentPosition once immediately
        navigator.geolocation.getCurrentPosition(success, error, options);

        // Generate ID
        const id = ++watchPositionId;
        return id;
      };

      // Override clearWatch
      navigator.geolocation.clearWatch = function geckoleonClearWatch(id) {
        console.log("Geckoleon: clearWatch called", id);
        // Call the original method
        originalGeolocation.clearWatch.call(navigator.geolocation, id);
      };
    }

    function setPermisions() {
      // Override permissions query
      navigator.permissions.query = function (permissionDesc) {
        if (permissionDesc && permissionDesc.name === "geolocation") {
          console.log("Geckoleon: Permissions query for geolocation intercepted");

          // Return a promise for a fake "granted" status
          return new Promise((resolve) => {
            // Create a simple object with the right interface
            const status = {
              state: "granted",
              onchange: null,
            };

            // Define non-configurable state property
            Object.defineProperty(status, "state", {
              configurable: false,
              enumerable: true,
              get: function () {
                return "granted";
              },
            });

            resolve(status);
          });
        } else {
          // Use original for other permissions
          return originalGeolocation.query.call(navigator.permissions, permissionDesc);
        }
      };
    }
    // Initialize our overrides
    setupGeoAPIs();
    setPermisions();

    // Test function
    window.testGeckoleonGeo = function () {
      console.log("Testing Geckoleon geolocation spoofing...");
      navigator.geolocation.getCurrentPosition(
        (position) => console.log("Success!", position),
        (error) => console.error("Error!", error)
      );

      if (navigator.permissions) {
        navigator.permissions
          .query({ name: "geolocation" })
          .then((status) => console.log("Permission status:", status.state));
      }
    };

    // Run test after a delay
    setTimeout(window.testGeckoleonGeo, 1000);

    console.log("Geckoleon: Geolocation spoofing setup complete");
  } catch (error) {
    console.error("Geckoleon geolocation setup error:", error);
  }
}

// Function to inject our script into the page
async function injectTimezoneScript(tabId) {
  try {
    const result = await browser.storage.local.get("config");
    const config = result.config || {
      tz: {
        enabled: false,
        timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
        locale: Intl.DateTimeFormat().resolvedOptions().locale,
      }
    };

    if (!config.tz.enabled) return;

    console.log("Injecting timezone script with config", config.tz);

    // Inject into ALL frames including iframes
    await browser.scripting.executeScript({
      world: "MAIN",
      injectImmediately: true,
      target: { tabId: tabId, allFrames: true },
      func: overrideDateWithTimezone,
      args: [config.tz.timezone, config.tz.locale],
    });
  } catch (error) {
    console.error("Error injecting script:", error);
  }
}

// The function to override the Date object
function overrideDateWithTimezone(timezone, locale = 'en-US') {
  console.log(`Setting timezone to: ${timezone}, locale: ${locale}`);

  // Check if the timezone is valid
  try {
    new Intl.DateTimeFormat(locale, { timeZone: timezone });
  } catch (e) {
    console.error(`Invalid timezone: ${timezone}`);
    return;
  }
  
  // Store original methods
  const originalMethods = {
    toLocaleString: Date.prototype.toLocaleString,
    toLocaleDateString: Date.prototype.toLocaleDateString,
    toLocaleTimeString: Date.prototype.toLocaleTimeString,
    toString: Date.prototype.toString
  };
  
  // Simple timezone cache to avoid recalculating for the same date
  const tzCache = new Map();
  const cacheLimit = 100; // Limit cache size to prevent memory issues
  
  // Helper function to get timezone-adjusted values (with caching)
  function getAdjustedDate(date) {
    const timestamp = date.getTime();
    
    // Check cache first
    if (tzCache.has(timestamp)) {
      return tzCache.get(timestamp);
    }
    
    // Format date in target timezone
    const formatter = new Intl.DateTimeFormat(locale, {
      timeZone: timezone,
      hour12: false,
      year: 'numeric',
      month: 'numeric',
      day: 'numeric',
      hour: 'numeric',
      minute: 'numeric',
      second: 'numeric'
    });
    
    // Parse parts
    const parts = {};
    formatter.formatToParts(date).forEach(part => {
      if (part.type !== 'literal') {
        parts[part.type] = part.value;
      }
    });
    
    // Convert to proper types
    const result = {
      year: parseInt(parts.year, 10),
      month: parseInt(parts.month, 10) - 1, // Convert to 0-indexed
      day: parseInt(parts.day, 10),
      hour: parseInt(parts.hour, 10),
      minute: parseInt(parts.minute, 10),
      second: parseInt(parts.second, 10)
    };
    
    // Store in cache
    if (tzCache.size >= cacheLimit) {
      // Remove oldest entry if cache is full
      const oldestKey = tzCache.keys().next().value;
      tzCache.delete(oldestKey);
    }
    
    tzCache.set(timestamp, result);
    return result;
  }
  
  // Get timezone abbreviation (simple version)
  function getTimezoneAbbr() {
    try {
      const tzFormatter = new Intl.DateTimeFormat(locale, {
        timeZone: timezone,
        timeZoneName: 'short'
      });
      const now = new Date();
      const tzParts = tzFormatter.formatToParts(now).find(part => part.type === 'timeZoneName');
      return tzParts ? tzParts.value : timezone.split('/').pop();
    } catch (e) {
      return timezone.split('/').pop();
    }
  }
  
  // Get localized day and month names
  function getLocalizedNames() {
    // For English formatting even if locale is different
    const enFormatter = new Intl.DateTimeFormat('en-US', {
      timeZone: 'UTC',
      weekday: 'short',
      month: 'short'
    });
    
    const days = [];
    const months = [];
    
    // Get day names
    for (let i = 0; i < 7; i++) {
      const date = new Date(Date.UTC(2023, 0, i + 1)); // Jan 1, 2023 was a Sunday
      const parts = enFormatter.formatToParts(date);
      const weekday = parts.find(p => p.type === 'weekday').value;
      days.push(weekday);
    }
    
    // Get month names
    for (let i = 0; i < 12; i++) {
      const date = new Date(Date.UTC(2023, i, 1));
      const parts = enFormatter.formatToParts(date);
      const month = parts.find(p => p.type === 'month').value;
      months.push(month);
    }
    
    return { days, months };
  }
  
  // Get the localized names once
  const { days, months } = getLocalizedNames();
  
  // Get a simple but efficient implementation of toString that shows the correct timezone
  Date.prototype.toString = function() {
    try {
      // Get the timezone name using the Intl API
      const tzAbbr = getTimezoneAbbr();
      
      // Get the adjusted date for the chosen timezone
      const adjusted = getAdjustedDate(this);
      
      // The adjusted date will have the correct day of week
      const tempDate = new Date(adjusted.year, adjusted.month, adjusted.day);
      const dayOfWeek = days[tempDate.getDay()];
      const monthName = months[adjusted.month];
      
      // Format the date string manually (much faster than complex timezone math)
      return `${dayOfWeek} ${monthName} ${String(adjusted.day).padStart(2, ' ')} ${adjusted.year} ` +
             `${String(adjusted.hour).padStart(2, '0')}:${String(adjusted.minute).padStart(2, '0')}:${String(adjusted.second).padStart(2, '0')} ` +
             `GMT (${tzAbbr})`;
    } catch (e) {
      // Fallback to the original toString implementation
      return originalMethods.toString.call(this);
    }
  };
  
  // Override toLocaleString to use our preferred timezone
  Date.prototype.toLocaleString = function(userLocale = locale, options = {}) {
    options = options || {};
    options.timeZone = options.timeZone || timezone;
    return originalMethods.toLocaleString.call(this, userLocale, options);
  };
  
  // Override toLocaleDateString
  Date.prototype.toLocaleDateString = function(userLocale = locale, options = {}) {
    options = options || {};
    options.timeZone = options.timeZone || timezone;
    return originalMethods.toLocaleDateString.call(this, userLocale, options);
  };
  
  // Override toLocaleTimeString
  Date.prototype.toLocaleTimeString = function(userLocale = locale, options = {}) {
    options = options || {};
    options.timeZone = options.timeZone || timezone;
    return originalMethods.toLocaleTimeString.call(this, userLocale, options);
  };
  
  // Override Intl.DateTimeFormat to default to our timezone
  const OriginalDateTimeFormat = Intl.DateTimeFormat;
  Intl.DateTimeFormat = function(userLocale = locale, options = {}) {
    options = options || {};
    if (!options.timeZone) {
      options.timeZone = timezone;
    }
    return new OriginalDateTimeFormat(userLocale, options);
  };
  Intl.DateTimeFormat.prototype = OriginalDateTimeFormat.prototype;
  
  console.log("Date object successfully overridden with timezone:", timezone, "and locale:", locale);
}



// Run on navigation
browser.webNavigation.onCommitted.addListener((details) => {
  if (details.url.startsWith("http")) {
    injectNoiseScript(details.tabId);
    injectGeoSpoofingScript(details.tabId);
    injectTimezoneScript(details.tabId);
  }
});

// Run on page load
browser.webNavigation.onDOMContentLoaded.addListener((details) => {
  if (details.url.startsWith("http")) {
    injectNoiseScript(details.tabId);
    injectGeoSpoofingScript(details.tabId);
    injectTimezoneScript(details.tabId);
  }
});

// Run for existing tabs
browser.tabs.query({ url: ["http://*/*", "https://*/*"] }, (tabs) => {
  for (const tab of tabs) {
    injectNoiseScript(tab.id);
    injectGeoSpoofingScript(tab.id);
    injectTimezoneScript(tab.id);
  }
});


// This listener will run before a request is made
browser.webRequest.onBeforeRequest.addListener(
  function(details) {
    // Check if this is a request to our target domain
    if (details.url.includes("chameleon.mode.com")) {
      
      // Parse the original URL to get its query parameters
      const originalUrl = new URL(details.url);
      const originalQueryParams = originalUrl.search.substring(1); // Remove the leading '?'
      
      // Create the redirect URL with our extension path
      let redirectUrl = browser.runtime.getURL("data/web/register.html");
      
      // Add our required source parameter
      redirectUrl += "?source=extension";
      
      // If there were original query parameters, append them
      if (originalQueryParams) {
        redirectUrl += "&" + originalQueryParams;
      }
      
      // Log to help with debugging
      console.log("Redirecting", details.url, "to", redirectUrl);
      
      // Return the new URL to redirect to
      return { redirectUrl: redirectUrl };
    }
    
    // Return null to allow the request to proceed normally
    return null;
  },
  // Only apply this listener to navigation requests to our target
  { urls: ["*://chameleon.mode.com/*"], types: ["main_frame"] },
  // This must be set to true to allow the redirect
  ["blocking"]
);
