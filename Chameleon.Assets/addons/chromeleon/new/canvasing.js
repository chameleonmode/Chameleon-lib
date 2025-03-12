// content-script.js - CSP-Safe Canvas Fingerprint Protection
// This script modifies canvas methods without using inline script injection

(async function() {
  // Get settings from storage
  const settings = await getSettings();
  
  // Early exit if protection is disabled
  if (!settings.enabled || !settings.canvasing) {
    return;
  }

  // Use a CSP-safe method to modify canvas behavior
  applyCanvasProtectionCSPSafe(settings);
  
  // Monitor for dynamically created iframes
  observeIFramesCSPSafe(settings);

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
   * Creates a script element with external source instead of inline content
   */
  function injectExternalScript(settings) {
    // Instead of using inline scripts, create a blob URL which is allowed in many CSP contexts
    const scriptContent = `
      const settings = ${JSON.stringify(settings)};
      
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
            console.error("Error in canvas noisify:", e);
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
      
      // Also defeat WebGL fingerprinting
      try {
        const getParameter = WebGLRenderingContext.prototype.getParameter;
        WebGLRenderingContext.prototype.getParameter = function(parameter) {
          // Add noise to shader precision
          if (parameter === 35724 || parameter === 35725) { // SHADING_LANGUAGE_VERSION
            const result = getParameter.apply(this, arguments);
            return result + ' (' + Math.random().toString(36).substr(2, 8) + ')';
          }
          
          // Return original result for other parameters
          return getParameter.apply(this, arguments);
        };
      } catch (e) {
        console.error("Error applying WebGL protection:", e);
      }
    `;
    
    // Create a blob URL from the script content
    const blob = new Blob([scriptContent], { type: 'application/javascript' });
    const scriptURL = URL.createObjectURL(blob);
    
    // Create and append the script element
    const script = document.createElement('script');
    script.src = scriptURL;
    (document.head || document.documentElement).appendChild(script);
    
    // Clean up after the script loads
    script.onload = () => {
      URL.revokeObjectURL(scriptURL);
      script.remove();
    };
  }

  /**
   * Apply canvas fingerprint protection in a CSP-safe way
   */
  function applyCanvasProtectionCSPSafe(settings) {
    // Method 1: Inject an external script via Blob URL
    injectExternalScript(settings);
    
    // Method 2: Direct modification from content script
    // This provides a fallback if Method 1 fails due to CSP restrictions
    try {
      // We can modify these from the content script context directly
      const origToDataURL = HTMLCanvasElement.prototype.toDataURL;
      const origToBlob = HTMLCanvasElement.prototype.toBlob;
      const origGetImageData = CanvasRenderingContext2D.prototype.getImageData;
      
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
            console.error("Error in canvas noisify:", e);
          }
        }
      }
      
      // Apply overrides using wrapper functions
      HTMLCanvasElement.prototype.toDataURL = function() {
        noisify(this, this.getContext('2d', { willReadFrequently: true }));
        return origToDataURL.apply(this, arguments);
      };
      
      HTMLCanvasElement.prototype.toBlob = function() {
        noisify(this, this.getContext('2d', { willReadFrequently: true }));
        return origToBlob.apply(this, arguments);
      };
      
      CanvasRenderingContext2D.prototype.getImageData = function() {
        noisify(this.canvas, this);
        return origGetImageData.apply(this, arguments);
      };
    } catch (e) {
      console.error("Error applying content script protection:", e);
    }
  }

  /**
   * Observes dynamically created iframes in a CSP-safe way
   */
  function observeIFramesCSPSafe(settings) {
    const observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        if (mutation.addedNodes) {
          mutation.addedNodes.forEach((node) => {
            // Check if the added node is an iframe
            if (node.tagName === 'IFRAME') {
              try {
                // Instead of trying to inject scripts directly, message the background script
                // to handle this iframe when it loads
                node.addEventListener('load', () => {
                  const iframeUrl = node.src || window.location.href;
                  chrome.runtime.sendMessage({
                    action: 'protectIframe',
                    frameUrl: iframeUrl
                  });
                });
              } catch (e) {
                console.error("Error handling iframe:", e);
              }
            }
          });
        }
      });
    });

    // Start observing the document
    observer.observe(document, { childList: true, subtree: true });
  }
}());