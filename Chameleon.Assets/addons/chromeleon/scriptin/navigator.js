// Browser Spoofing Module
// This script runs in the main execution world to modify browser identification values

(() => {
    // Define configurations for different operating systems
    const osConfigs = {
      mac: {
        os: "Mac",
        os_ver: "10.15",
        ua: "Chrome",
        device: "Apple",
        platform: "MacIntel",
        engine_ver: "Blink 131",
        uaTemplate: "(Macintosh; Mac 10.15)"
      },
      windows: {
        os: "Windows",
        os_ver: "10.0",
        ua: "Chrome",
        device: "PC",
        platform: "Win32",
        engine_ver: "Blink 131",
        uaTemplate: "(Windows NT 10.0; Win64; x64)"
      },
      linux: {
        os: "Linux",
        os_ver: "x86_64",
        ua: "Chrome",
        device: "PC",
        platform: "Linux x86_64",
        engine_ver: "Blink 131",
        uaTemplate: "(X11; Linux x86_64)"
      }
    };
    
    // Choose which OS to spoof - options: 'mac', 'windows', 'linux'
    const osToSpoof = 'windows'; // Change this to select different OS
    
    // Set the active configuration
    const spoofedValues = osConfigs[osToSpoof];
  
    // Get current UA and modify only the parts we want to spoof
    const originalUA = navigator.userAgent;
    
    // Parse and modify the UA string to only replace specific parts
    let customUA = originalUA
      // Replace OS and version with the appropriate template for the selected OS
      .replace(/\([^)]+\)/, spoofedValues.uaTemplate)
      // Replace Chrome version if present
      .replace(/Chrome\/[\d.]+/, `Chrome/${spoofedValues.engine_ver.split(' ')[1]}.0.0.0`);
      
    console.debug("Original UA:", originalUA);
    console.debug("Modified UA:", customUA);
    console.debug("Spoofing OS:", osToSpoof);
  
    // Override navigator properties
    const navigatorProps = {
      userAgent: { value: customUA },
      platform: { value: spoofedValues.platform },
      appVersion: { value: `5.0 (Macintosh; ${spoofedValues.os} ${spoofedValues.os_ver})` }
    };
  
    // Apply navigator property overrides
    Object.defineProperties(Navigator.prototype, navigatorProps);
  
    // Override navigator.userAgentData properties if available
    if ('userAgentData' in navigator) {
      // Override getHighEntropyValues method
      const originalGetHighEntropyValues = navigator.userAgentData.getHighEntropyValues;
      navigator.userAgentData.getHighEntropyValues = function(hints) {
        return originalGetHighEntropyValues.call(this, hints).then(data => {
          const newData = { ...data };
          if (hints.includes('platform')) newData.platform = spoofedValues.platform;
          if (hints.includes('platformVersion')) newData.platformVersion = spoofedValues.os_ver;
          if (hints.includes('model')) newData.model = spoofedValues.device;
          
          // Set architecture based on OS
          if (hints.includes('architecture')) {
            newData.architecture = osToSpoof === 'mac' ? 'x86_64' : 
                                  osToSpoof === 'windows' ? 'x86_64' : 'x86_64';
          }
          
          if (hints.includes('uaFullVersion')) {
            newData.uaFullVersion = spoofedValues.engine_ver.split(' ')[1] + '.0.0.0';
          }
          
          return newData;
        });
      };
  
      // Override brands property
      Object.defineProperty(navigator.userAgentData, 'brands', {
        get: function() {
          const chromeVersion = spoofedValues.engine_ver.split(' ')[1];
          return [
            { brand: "Chrome", version: chromeVersion },
            { brand: "Chromium", version: chromeVersion },
            { brand: "Not-A.Brand", version: "99" }
          ];
        }
      });
  
      // Override platform property
      Object.defineProperty(navigator.userAgentData, 'platform', {
        get: function() {
          return spoofedValues.platform;
        }
      });
    }
  
    // Inject custom JS object for site-specific fingerprinting
    window.navigator.oscpu = `${spoofedValues.os} ${spoofedValues.os_ver}`;
    window.navigator.vendor = "Google Inc.";
    
    // Override document.documentElement for CSS detection
    const originalMatchesMethod = Element.prototype.matches;
    Element.prototype.matches = function(selector) {
      if (selector.includes(':-webkit-') || 
          selector.includes('::-webkit-') || 
          selector.includes('-webkit-')) {
        // Return true for Webkit-specific selectors
        return true;
      }
      return originalMatchesMethod.call(this, selector);
    };
  
    // Create a script to be injected that sets additional properties
    // These need to be set in the page context to bypass closure scoping
    const script = document.createElement('script');
    script.textContent = `
      // Get the current OS being spoofed from a data attribute
      const osToSpoof = '${osToSpoof}';
      
      // Override window properties based on OS
      const pixelRatio = osToSpoof === 'mac' ? 2 : 
                         osToSpoof === 'windows' ? 1 : 
                         1.5; // Linux default
      Object.defineProperty(window, 'devicePixelRatio', { value: pixelRatio });
      
      // Add custom browser engine fingerprint
      window.chrome = window.chrome || {};
      window.chrome.runtime = window.chrome.runtime || {};
      
      // Set OS-specific properties
      if (osToSpoof === 'windows') {
        // Windows-specific properties
        window.navigator.msMaxTouchPoints = 0;
      }
      
      // Add custom navigator plugins
      if (navigator.__defineGetter__) {
        navigator.__defineGetter__('plugins', function() {
          const plugins = [];
          plugins.refresh = function() {};
          return plugins;
        });
      }
      
      // Override screen properties
      Object.defineProperties(Screen.prototype, {
        colorDepth: { value: osToSpoof === 'mac' ? 30 : 24 },
        pixelDepth: { value: osToSpoof === 'mac' ? 30 : 24 }
      });
      
      // Log that the spoofing was successful
      console.debug("Browser spoofing module activated for " + osToSpoof);
    `;
  
    // Execute the script in the page context
    document.documentElement.appendChild(script);
    //document.documentElement.removeChild(script);
  })();