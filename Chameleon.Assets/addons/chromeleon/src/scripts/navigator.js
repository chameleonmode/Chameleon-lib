export default async function (opts) {
  const { os, random, configs } = opts || {};
  console.log("OS Spoofer with Client Hints Support - Starting" + JSON.stringify(opts));

  const RULE_ID_START = 1000;
  const type = "modifyHeaders";
  const condition = {
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
  };

  // Function to update rules based on current config

  // Remove only existing dynamic rules with IDs >= RULE_ID_START
  const rules = await chrome.declarativeNetRequest.getDynamicRules();
  console.log("Existing rules:", rules);
  const existingRuleIds = rules.filter((rule) => rule.id >= RULE_ID_START).map((rule) => rule.id);
  console.log("Existing rule IDs:", existingRuleIds);
  // Remove all existing rules with IDs >= RULE_ID_START
  await chrome.declarativeNetRequest.updateDynamicRules({
    removeRuleIds: existingRuleIds,
  });

  if (random || os !== "default") {
    const config =
      configs[!random ? os : Object.keys(configs)[Math.floor(Math.random() * Object.keys(configs).length)]];

    // Prepare new rules to add
    const addRules = [];
    let ruleId = RULE_ID_START + existingRuleIds.length;

    // First, create a rule to remove all existing client hint headers
    addRules.push({
      id: ruleId,
      priority: 1,
      action: {
        type,
        requestHeaders: Object.keys(config).map((hint) => ({
          header: hint,
          operation: "remove",
        })),
      },
      condition,
    });

    // Reset activeRuleIds and start tracking
    const activeRuleIds = [ruleId++];

    // Then add each client hint with the spoofed value
    for (const [name, value] of Object.entries(config)) {
      addRules.push({
        id: ruleId,
        priority: 2, // Higher priority than the removal rule
        action: {
          type,
          requestHeaders: [
            {
              header: name,
              operation: "set",
              value: value,
            },
          ],
        },
        condition,
      });
      activeRuleIds.push(ruleId++);
    }

    // Remove all existing rules and add the new ones
    await chrome.declarativeNetRequest.updateDynamicRules({
      removeRuleIds: existingRuleIds,
      addRules: addRules,
    });
  }

  return function (params) {
    const { os, random, configs } = params || {};
    console.log("OS Spoofer with Client Hints Support - Starting", JSON.stringify(params));

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
        brands: navigator.userAgentData.brands, //spoofedValues.brandInfo,
        mobile: true,

        // High entropy hints for getHighEntropyValues() method
        platformVersion: spoofedValues.os_ver,
        architecture: spoofedValues.architecture,
        bitness: spoofedValues.bitness,
        model: spoofedValues.device,
        wow64: spoofedValues.wow64,
        //fullVersionList: spoofedValues.fullVersionList,
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

      // Store the original method before we replace anything
      const originalNavigatorUAData = navigator.userAgentData;
      const originalGetHighEntropyValues =
        originalNavigatorUAData.getHighEntropyValues.bind(originalNavigatorUAData);

      // Method for high entropy hints
      const getHighEntropyValues = function (hints) {
        console.log("Intercepted getHighEntropyValues with hints:", hints);

        // Call the original method without causing recursion
        return originalGetHighEntropyValues(hints)
          .then((originalValues) => {
            console.log("Original high entropy values:", originalValues);

            const result = { ...originalValues }; // Start with original values

            // Replace with spoofed values when available
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
            return result;
          })
          .catch((error) => {
            console.error("Error getting original high entropy values:", error);

            // Fallback to only spoofed values if original values can't be retrieved
            const fallbackResult = {};
            hints.forEach((hint) => {
              const hintName = hint
                .replace(/^[A-Z]/, (c) => c.toLowerCase())
                .replace(/-([a-z])/g, (_, c) => c.toUpperCase());

              if (hintName in spoofedClientHints) {
                fallbackResult[hintName] = spoofedClientHints[hintName];
              }
            });

            console.log("Returning fallback spoofed values:", fallbackResult);
            return fallbackResult;
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
          navigator.userAgentData.getHighEntropyValues = getHighEntropyValues;

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
