export default function (opts) {
  const { os, random } = opts || {};
  console.log("OS Spoofer with Client Hints Support - Starting" + JSON.stringify(opts));

  // Configuration for different operating systems
  const osConfigs = {
    mac: {
      userAgent:
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36",
      clientHints: {
        "sec-ch-ua": '"Google Chrome";v="134", "Chromium";v="134", "Not.A/Brand";v="8"',
        "sec-ch-ua-platform": '"macOS"',
        "sec-ch-ua-mobile": "?0",
        "sec-ch-ua-platform-version": '"15.3.1"',
        "sec-ch-ua-arch": '"arm64"',
        "sec-ch-ua-bitness": '"64"',
        "sec-ch-ua-wow64": "?0",
        "sec-ch-ua-model": '"MacBookPro"',
        "sec-ch-ua-full-version-list":
          '"Google Chrome";v="134.0.6998.89", "Chromium";v="134.0.6998.89", "Not.A/Brand";v="8.0.0.0"',
      },
    },
    windows: {
      userAgent:
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36",
      clientHints: {
        "sec-ch-ua": '"Google Chrome";v="134", "Chromium";v="134", "Not.A/Brand";v="8"',
        "sec-ch-ua-platform": '"Windows"',
        "sec-ch-ua-mobile": "?0",
        "sec-ch-ua-platform-version": '"10.0.22621"',
        "sec-ch-ua-arch": '"x86-64"',
        "sec-ch-ua-bitness": '"64"',
        "sec-ch-ua-wow64": "?0",
        "sec-ch-ua-model": '"PC"',
        "sec-ch-ua-full-version-list":
          '"Google Chrome";v="134.0.6998.89", "Chromium";v="134.0.6998.89", "Not.A/Brand";v="8.0.0.0"',
      },
    },
    linux: {
      userAgent:
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36",
      clientHints: {
        "sec-ch-ua": '"Google Chrome";v="134", "Chromium";v="134", "Not.A/Brand";v="8"',
        "sec-ch-ua-platform": '"Linux"',
        "sec-ch-ua-mobile": "?0",
        "sec-ch-ua-platform-version": '"5.15.0"',
        "sec-ch-ua-arch": '"x86-64"',
        "sec-ch-ua-bitness": '"64"',
        "sec-ch-ua-wow64": "?0",
        "sec-ch-ua-model": '"PC"',
        "sec-ch-ua-full-version-list":
          '"Google Chrome";v="134.0.6998.89", "Chromium";v="134.0.6998.89", "Not.A/Brand";v="8.0.0.0"',
      },
    },
  };

  // Store rule IDs to manage them later
  let activeRuleIds = [];
  const RULE_ID_START = 1000;

  // Function to update rules based on current config
  function updateHeaderRules() {
    const config = osConfigs[os];
    if (!config) return;

    // Remove only existing dynamic rules with IDs >= 1000
    chrome.declarativeNetRequest.getDynamicRules((existingRules) => {
      const existingRuleIds = existingRules.filter((rule) => rule.id >= 1000).map((rule) => rule.id);

      // Prepare new rules to add
      const addRules = [];
      let ruleId = RULE_ID_START;

      // User-Agent rule
      addRules.push({
        id: ruleId,
        priority: 1,
        action: {
          type: "modifyHeaders",
          requestHeaders: [
            {
              header: "User-Agent",
              operation: "set",
              value: config.userAgent,
            },
          ],
        },
        condition: {
          urlFilter: "*",
          resourceTypes: [
            "main_frame",
            "sub_frame",
            "stylesheet",
            "script",
            "image",
            "font",
            "object",
            "xmlhttprequest",
            "ping",
            "csp_report",
            "media",
            "websocket",
            "other",
          ],
        },
      });
      activeRuleIds = [ruleId++]; // Reset activeRuleIds and start tracking

      // Client Hints rules
      // First, create a rule to remove all existing client hint headers
      const removeHeaders = Object.keys(config.clientHints).map((hint) => ({
        header: hint,
        operation: "remove",
      }));

      addRules.push({
        id: ruleId,
        priority: 1,
        action: {
          type: "modifyHeaders",
          requestHeaders: removeHeaders,
        },
        condition: {
          urlFilter: "*",
          resourceTypes: [
            "main_frame",
            "sub_frame",
            "stylesheet",
            "script",
            "image",
            "font",
            "object",
            "xmlhttprequest",
            "ping",
            "csp_report",
            "media",
            "websocket",
            "other",
          ],
        },
      });
      activeRuleIds.push(ruleId++);

      // Then add each client hint with the spoofed value
      for (const [name, value] of Object.entries(config.clientHints)) {
        addRules.push({
          id: ruleId,
          priority: 2, // Higher priority than the removal rule
          action: {
            type: "modifyHeaders",
            requestHeaders: [
              {
                header: name,
                operation: "set",
                value: value,
              },
            ],
          },
          condition: {
            urlFilter: "*",
            resourceTypes: [
              "main_frame",
              "sub_frame",
              "stylesheet",
              "script",
              "image",
              "font",
              "object",
              "xmlhttprequest",
              "ping",
              "csp_report",
              "media",
              "websocket",
              "other",
            ],
          },
        });
        activeRuleIds.push(ruleId++);
      }

      // Remove all existing rules and add the new ones
      chrome.declarativeNetRequest.updateDynamicRules({
        removeRuleIds: existingRuleIds,
        addRules: addRules,
      });
    });
  }

  updateHeaderRules();

  return function navigatorization(params) {
    console.log("OS Spoofer with Client Hints Support - Starting");
    const { os, random } = params || {};

    // Define configurations for different operating systems with focused properties
    const osConfigs = {
      mac: {
        os: "Mac",
        os_ver: "10.15.7",
        device: "MacBookPro",
        platform: "MacIntel",
        architecture: "arm64",
        bitness: "64",
        uaTemplate: "(Macintosh; Intel Mac OS X 10_15_7)",
        brandInfo: [
          { brand: "Google Chrome", version: "134" },
          { brand: "Not.A/Brand", version: "8" },
          { brand: "Chromium", version: "134" },
        ],
        fullVersionList: [
          { brand: "Google Chrome", version: "134.0.0.0" },
          { brand: "Not.A/Brand", version: "8.0.0.0" },
          { brand: "Chromium", version: "134.0.0.0" },
        ],
        clientHintsPlatform: "macOS",
        formFactors: ["Desktop"],
        wow64: false,
      },
      windows: {
        os: "Windows",
        os_ver: "10.0.22621",
        device: "PC",
        platform: "Win32",
        architecture: "x86-64",
        bitness: "64",
        uaTemplate: "(Windows NT 10.0; Win64; x64)",
        brandInfo: [
          { brand: "Google Chrome", version: "134" },
          { brand: "Not.A/Brand", version: "8" },
          { brand: "Chromium", version: "134" },
        ],
        fullVersionList: [
          { brand: "Google Chrome", version: "134.0.0.0" },
          { brand: "Not.A/Brand", version: "8.0.0.0" },
          { brand: "Chromium", version: "134.0.0.0" },
        ],
        clientHintsPlatform: "Windows",
        formFactors: ["Desktop"],
        wow64: false,
      },
      linux: {
        os: "Linux",
        os_ver: "5.15.0",
        device: "PC",
        platform: "Linux x86_64",
        architecture: "x86-64",
        bitness: "64",
        uaTemplate: "(X11; Linux x86_64)",
        brandInfo: [
          { brand: "Google Chrome", version: "134" },
          { brand: "Not.A/Brand", version: "8" },
          { brand: "Chromium", version: "134" },
        ],
        fullVersionList: [
          { brand: "Google Chrome", version: "134.0.0.0" },
          { brand: "Not.A/Brand", version: "8.0.0.0" },
          { brand: "Chromium", version: "134.0.0.0" },
        ],
        clientHintsPlatform: "Linux",
        formFactors: ["Desktop"],
        wow64: false,
      },
    };

    // Choose which OS to spoof - use parameter or default to windows
    const osToSpoof = os && osConfigs[os] ? os : "windows";

    // Set the active configuration
    const spoofedValues = osConfigs[osToSpoof];

    // Get current UA and extract relevant information
    const originalUA = navigator.userAgent;

    // Extract the current Chrome version from the user agent
    const chromeVersionMatch = originalUA.match(/Chrome\/([0.9.]+)/);
    const chromeVersion = chromeVersionMatch ? chromeVersionMatch[1] : "";

    // Parse and modify the UA string to only replace the OS part
    let customUA = originalUA
      // Replace OS and version with the appropriate template for the selected OS
      .replace(/\([^)]+\)/, spoofedValues.uaTemplate);

    console.log("Original UA:", originalUA);
    console.log("Modified UA:", customUA);
    console.log("Spoofing OS:", osToSpoof);

    // Store original descriptors to restore if needed
    const originalDescriptors = {
      userAgent: Object.getOwnPropertyDescriptor(Navigator.prototype, "userAgent"),
      platform: Object.getOwnPropertyDescriptor(Navigator.prototype, "platform"),
      appVersion: Object.getOwnPropertyDescriptor(Navigator.prototype, "appVersion"),
    };

    // Override only the specified navigator properties
    const navigatorProps = {
      userAgent: {
        get: function () {
          return customUA;
        },
      },
      platform: {
        get: function () {
          return spoofedValues.platform;
        },
      },
      appVersion: {
        get: function () {
          return originalUA
            .replace(/Mozilla\/[\d.]+/, "Mozilla/5.0")
            .replace(/\([^)]+\)/, spoofedValues.uaTemplate);
        },
      },
    };

    // Apply navigator property overrides
    Object.defineProperties(Navigator.prototype, navigatorProps);

    // Override navigator.userAgentData properties if available
    if ("userAgentData" in navigator) {
      console.log("Patching userAgentData for Client Hints");

      // Create a comprehensive set of client hints
      const spoofedClientHints = {
        // === DEVICE/PLATFORM RELATED HINTS ===
        // Low entropy hints
        platform: spoofedValues.clientHintsPlatform,
        brands: spoofedValues.brandInfo,
        mobile: true,

        // High entropy hints for getHighEntropyValues() method
        platformVersion: spoofedValues.os_ver,
        architecture: spoofedValues.architecture,
        bitness: spoofedValues.bitness,
        model: spoofedValues.device,
        wow64: spoofedValues.wow64,
        fullVersionList: navigator.userAgentData.fullVersionList, //spoofedValues.fullVersionList,
        formFactors: spoofedValues.formFactors,

        // === USER PREFERENCES ===
        // Reuse actual preferences (if available) or provide reasonable defaults
        prefersColorScheme: window.matchMedia?.("(prefers-color-scheme: dark)").matches ? "dark" : "light",
        prefersReducedMotion: window.matchMedia?.("(prefers-reduced-motion: reduce)").matches
          ? "reduce"
          : "no-preference",
        prefersReducedTransparency: false,
        prefersContrast: "no-preference",
        forcedColors: window.matchMedia?.("(forced-colors: active)").matches ? "active" : "none",

        // === DEVICE CAPABILITIES/DISPLAY ===
        // Keep some of the actual data since it's not OS-specific and would be suspicious if wrong
        width: window.screen?.width || 1920,
        viewportWidth: window.innerWidth || 1280,
        viewportHeight: window.innerHeight || 720,
        dpr: window.devicePixelRatio || 1.0,
        deviceMemory: navigator.deviceMemory || 8,

        // === NETWORK RELATED ===
        // Use reasonable defaults
        rtt: 50, // 50ms - typical broadband
        downlink: 10, // 10 Mbps - typical broadband
        ect: "4g", // 4G connection
      };

      // Method for high entropy hints
      const getHighEntropyValues = function (hints) {
        console.log("Intercepted getHighEntropyValues with hints:", hints);

        // Create a result object with only the requested hints
        return new Promise((resolve) => {
          const result = {};

          // Only include the requested hints
          hints.forEach((hint) => {
            // Convert hint name to camelCase if needed
            const hintName = hint
              .replace(/^[A-Z]/, (c) => c.toLowerCase())
              .replace(/-([a-z])/g, (_, c) => c.toUpperCase());

            // Add the hint if we have a value for it
            if (hintName in spoofedClientHints) {
              result[hintName] = spoofedClientHints[hintName];
            }
          });

          console.log("Returning spoofed high entropy values:", result);
          resolve(result);
        });
      };
      console.log("chints", navigator.userAgentData);
      // Create a complete userAgentData replacement with all required methods
      const spoofedUserAgentData = {
        // Low entropy hints (directly accessible)
        platform: spoofedClientHints.platform,
        brands: spoofedClientHints.brands,
        mobile: spoofedClientHints.mobile,

        // High entropy method
        getHighEntropyValues: getHighEntropyValues,

        // ToJSON method for serialization
        toJSON: function () {
          return {
            brands: this.brands,
            mobile: this.mobile,
            platform: this.platform,
          };
        },
      };

      // Try to completely replace the userAgentData object
      try {
        Object.defineProperty(navigator, "userAgentData", {
          get: function () {
            return spoofedUserAgentData;
          },
          configurable: true,
        });

        console.log("Replaced userAgentData object:", navigator.userAgentData);
      } catch (e) {
        console.error("Failed to replace userAgentData object:", e);

        // Fallback: try to override just the properties and methods
        try {
          // Override the basic properties
          Object.defineProperties(navigator.userAgentData, {
            platform: {
              get: function () {
                return spoofedClientHints.platform;
              },
              configurable: true,
            },
            brands: {
              get: function () {
                return spoofedClientHints.brands;
              },
              configurable: true,
            },
            mobile: {
              get: function () {
                return spoofedClientHints.mobile;
              },
              configurable: true,
            },
          });

          // Override the getHighEntropyValues method
          const originalGetHighEntropyValues = navigator.userAgentData.getHighEntropyValues;
          navigator.userAgentData.getHighEntropyValues = function (hints) {
            return getHighEntropyValues(hints);
          };

          console.log("Patched userAgentData properties and methods");
        } catch (err) {
          console.error("Failed to patch userAgentData properties:", err);
        }
      }
    }

    // Inject custom oscpu value for site-specific fingerprinting
    window.navigator.oscpu = `${spoofedValues.os} ${spoofedValues.os_ver}`;

    // Important note about limitations
    console.log(`
        === IMPORTANT LIMITATIONS ===
        This script successfully modifies JavaScript-accessible properties 
        but cannot modify Client Hints HTTP headers sent by the browser.
        
        To spoof HTTP headers like Sec-CH-UA-Platform, you would need:
        1. A browser extension with webRequest permissions
        2. A proxy or network-level interceptor
        3. A modified browser build
        `);

    console.log(`Applied OS spoofing: ${osToSpoof} with Client Hints support`);
    return true;
  };
}
