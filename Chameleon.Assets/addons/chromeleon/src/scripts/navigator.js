export default async function (opts) {
  return function (params) {
    const { config } = params || {};
    // Override only the specified navigator properties
    const navigatorProps = {
      userAgent: {
        get: function () {
          return config["User-Agent"];
        },
      },
      platform: {
        get: function () {
          return config["sec-ch-ua-platform"];
        },
      },
      appVersion: {
        get: function () {
          return config["User-Agent"].replace("Mozilla/5.0", "5.0");
        },
      },
    };
    // Apply navigator property overrides
    Object.defineProperties(Navigator.prototype, navigatorProps);

    // Override navigator.userAgentData properties if available
    if ("userAgentData" in navigator) {
      // Create a comprehensive set of client hints
      const clientHints = {
        // === DEVICE/PLATFORM RELATED HINTS ===
        // Low entropy hints
        platform: config["sec-ch-ua-platform"],
        brands: navigator.userAgentData.brands, //spoofedValues.brandInfo,
        mobile: navigator.userAgentData.mobile,

        // High entropy hints for getHighEntropyValues() method
        platformVersion: config["sec-ch-ua-platform-version"],
        //architecture: osConfigs[os].architecture,
        //bitness: osConfigs[os].bitness,
        model: config["sec-ch-ua-model"],
        //wow64: osConfigs[os].wow64,
        //fullVersionList: spoofedValues.fullVersionList,
        //formFactors: osConfigs[os].formFactors,

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
      const highEntropyValues = navigator.userAgentData.getHighEntropyValues.bind(navigator.userAgentData);

      // Try to completely replace the userAgentData object
      Object.defineProperty(navigator, "userAgentData", {
        get: function () {
          return {
            // Low entropy hints (directly accessible)
            platform: clientHints.platform,
            brands: clientHints.brands,
            mobile: clientHints.mobile,

            // High entropy method
            getHighEntropyValues: function (hints) {
              // Call the original method without causing recursion
              return highEntropyValues(hints)
                .then((originalValues) => {
                  const result = { ...originalValues }; // Start with original values

                  // Replace with spoofed values when available
                  hints.forEach((hint) => {
                    // Convert hint name to camelCase if needed
                    const hintName = hint
                      .replace(/^[A-Z]/, (c) => c.toLowerCase())
                      .replace(/-([a-z])/g, (_, c) => c.toUpperCase());

                    // Add the hint if we have a value for it
                    if (hintName in clientHints) {
                      result[hintName] = clientHints[hintName];
                    }
                  });
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

                    if (hintName in clientHints) {
                      fallbackResult[hintName] = clientHints[hintName];
                    }
                  });
                  return fallbackResult;
                });
            },

            // ToJSON method for serialization
            toJSON: function () {
              return {
                brands: this.brands,
                mobile: this.mobile,
                platform: this.platform,
              };
            },
          };
        },
        configurable: true,
      });
    }
    // Inject custom oscpu value for site-specific fingerprinting
    window.navigator.oscpu = `${config["sec-ch-ua-platform"]} ${config["sec-ch-ua-platform-version"]}`;
    return true;
  };
}
