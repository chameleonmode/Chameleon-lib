// content-script.js - Canvas Fingerprint Protection
// This script runs in the content script context and injects the protection into the page

(async function() {
    // Get settings from storage
    const settings = await getSettings();
    
    // Early exit if protection is disabled
    if (!settings.enabled || !settings.canvasing) {
      return;
    }
  
    // Apply protection in content script context (limited effect)
    applyCanvasProtection(settings);
    
    // Inject settings into page as a global variable
    injectSettings(settings);
    
    // Inject the main protection script into the page
    injectProtectionScript();
    
    // Also monitor for frames ourselves as a fallback
    observeIFrames(settings);
  
    /**
     * Get extension settings from storage
     */
    async function getSettings() {
      return new Promise((resolve) => {
        chrome.storage.sync.get(
          ["canvasing", "randomCanvasing", "canvasR", "canvasG", "canvasB", "canvasA", "enabled"],
          (result) => {
            // Set default values if not found
            const settings = {
              canvasing: result.canvasing !== undefined ? result.canvasing : true,
              randomCanvasing: result.randomCanvasing !== undefined ? result.randomCanvasing : true,
              canvasR: result.canvasR !== undefined ? result.canvasR : 1,
              canvasG: result.canvasG !== undefined ? result.canvasG : 1,
              canvasB: result.canvasB !== undefined ? result.canvasB : 1,
              canvasA: result.canvasA !== undefined ? result.canvasA : 1,
              enabled: result.enabled !== undefined ? result.enabled : true
            };
            resolve(settings);
          }
        );
      });
    }
  
    /**
     * Inject settings as a global variable in the page context
     */
    function injectSettings(settings) {
      try {
        // Create a global variable to hold settings for the protection script
        const settingsScript = document.createElement('script');
        settingsScript.textContent = `
          window.canvasProtectionSettings = ${JSON.stringify(settings)};
          window.canvasProtectionApplied = false;
        `;
        
        // Try to inject the script - this might fail due to CSP, but it's worth trying
        document.documentElement.appendChild(settingsScript);
        settingsScript.remove();
      } catch (e) {
        console.error("Failed to inject settings directly:", e);
        
        // Fallback: Store settings in sessionStorage
        try {
          sessionStorage.setItem('canvasProtectionSettings', JSON.stringify(settings));
          
          // Add a script to read from sessionStorage
          const fallbackScript = document.createElement('script');
          fallbackScript.textContent = `
            try {
              window.canvasProtectionSettings = JSON.parse(sessionStorage.getItem('canvasProtectionSettings'));
              window.canvasProtectionApplied = false;
            } catch(e) {
              console.error("Failed to get settings from sessionStorage:", e);
              // Default settings as a last resort
              window.canvasProtectionSettings = {
                canvasing: true,
                randomCanvasing: true,
                canvasR: 1,
                canvasG: 1,
                canvasB: 1,
                canvasA: 1,
                enabled: true
              };
            }
          `;
          document.documentElement.appendChild(fallbackScript);
          fallbackScript.remove();
        } catch (e2) {
          console.error("All settings injection methods failed:", e2);
        }
      }
    }
  
    /**
     * Inject the main protection script from extension resources
     */
    function injectProtectionScript() {
      try {
        // Get the URL of the protection script
        const scriptURL = chrome.runtime.getURL('canvas-protection.js');
        
        // Create and inject the script element
        const script = document.createElement('script');
        script.src = scriptURL;
        script.onload = function() {
          // Clean up after loading
          this.remove();
        };
        
        // Append to document to load it
        (document.head || document.documentElement).appendChild(script);
      } catch (e) {
        console.error("Failed to inject protection script:", e);
      }
    }
  
    /**
     * Apply canvas protection in content script context
     * This has limited effect since most fingerprinting happens in the page context,
     * but it's a useful defense-in-depth measure
     */
    function applyCanvasProtection(settings) {
      // Store original methods before they can be accessed by any script
      const origGetImageData = CanvasRenderingContext2D.prototype.getImageData;
      const origToDataURL = HTMLCanvasElement.prototype.toDataURL;
      const origToBlob = HTMLCanvasElement.prototype.toBlob;
      
      // Define our noise function
      function noisify(canvas, context) {
        if (!context) return;
        
        const shift = {
          r: settings.randomCanvasing ? Math.floor(Math.random() * 10) - 5 : settings.canvasR,
          g: settings.randomCanvasing ? Math.floor(Math.random() * 10) - 5 : settings.canvasG,
          b: settings.randomCanvasing ? Math.floor(Math.random() * 10) - 5 : settings.canvasB,
          a: settings.randomCanvasing ? Math.floor(Math.random() * 10) - 5 : settings.canvasA,
        };
        
        const width = canvas.width;
        const height = canvas.height;
        
        if (width && height) {
          try {
            const imageData = origGetImageData.apply(context, [0, 0, width, height]);
            
            // Add noise to image data
            for (let i = 0; i < height; i++) {
              for (let j = 0; j < width; j++) {
                const n = i * (width * 4) + j * 4;
                imageData.data[n + 0] = Math.max(0, Math.min(255, imageData.data[n + 0] + shift.r));
                imageData.data[n + 1] = Math.max(0, Math.min(255, imageData.data[n + 1] + shift.g));
                imageData.data[n + 2] = Math.max(0, Math.min(255, imageData.data[n + 2] + shift.b));
                imageData.data[n + 3] = Math.max(0, Math.min(255, imageData.data[n + 3] + shift.a));
              }
            }
            
            context.putImageData(imageData, 0, 0);
          } catch (e) {
            console.error("Error in canvas noisify (content script):", e);
          }
        }
      }
  
      // Override toDataURL method
      HTMLCanvasElement.prototype.toDataURL = function() {
        noisify(this, this.getContext('2d', { willReadFrequently: true }));
        return origToDataURL.apply(this, arguments);
      };
      
      // Override toBlob method
      HTMLCanvasElement.prototype.toBlob = function() {
        noisify(this, this.getContext('2d', { willReadFrequently: true }));
        return origToBlob.apply(this, arguments);
      };
      
      // Override getImageData method
      CanvasRenderingContext2D.prototype.getImageData = function() {
        noisify(this.canvas, this);
        return origGetImageData.apply(this, arguments);
      };
    }
  
    /**
     * Observe for new iframes and inject protection
     * This is a fallback in case the page script observer fails
     */
    function observeIFrames(settings) {
      const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
          if (mutation.addedNodes) {
            mutation.addedNodes.forEach((node) => {
              // Check if the added node is an iframe
              if (node.tagName === 'IFRAME') {
                // Listen for the iframe to load
                node.addEventListener('load', () => {
                  try {
                    // Try to access the iframe document
                    const iframeDoc = node.contentDocument;
                    if (!iframeDoc) return; // Cross-origin iframe
                    
                    // Get our script URL
                    const scriptURL = chrome.runtime.getURL('canvas-protection.js');
                    
                    // First inject the settings
                    const settingsScript = iframeDoc.createElement('script');
                    settingsScript.textContent = `
                      window.canvasProtectionSettings = ${JSON.stringify(settings)};
                      window.canvasProtectionApplied = false;
                    `;
                    
                    iframeDoc.head.appendChild(settingsScript);
                    settingsScript.remove();
                    
                    // Then inject the protection script
                    const script = iframeDoc.createElement('script');
                    script.src = scriptURL;
                    iframeDoc.head.appendChild(script);
                  } catch (e) {
                    // Cannot access cross-origin iframe content
                    // This is expected due to browser security restrictions
                  }
                });
              }
            });
          }
        });
      });
  
      // Start observing the document with the configured parameters
      observer.observe(document, { childList: true, subtree: true });
    }
  }());