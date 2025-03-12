// canvaser.js - Canvas Fingerprint Protection
// This script runs directly in the web page's context and modifies canvas methods
// before any fingerprinting script can access them

(async function() {
    // Get settings from storage
    const settings = await getSettings();
    
    // Early exit if protection is disabled
    if (!settings.enabled || !settings.canvasing) {
      return;
    }
  
    // Apply protection immediately
    applyCanvasProtection(settings);
    
    // Also inject into the page context to defeat page-level fingerprinting
    injectPageScript(settings);
    
    // Monitor for dynamically created iframes
    observeIFrames();
  
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
     * Creates a script that applies canvas protection and injects it directly into the page
     * to bypass content script isolation
     */
    function injectPageScript(settings) {
      const scriptContent = `
        (function() {
          const settings = ${JSON.stringify(settings)};
          ${applyCanvasProtectionFunc.toString()}
          ${noisifyFunc.toString()}
          
          // Apply protection immediately
          applyCanvasProtection(settings);
          
          // Monitor for dynamically created iframes
          ${observeIFramesFunc.toString()}
          observeIFrames();
        })();
      `;
      
      const script = document.createElement('script');
      script.textContent = scriptContent;
      (document.head || document.documentElement).appendChild(script);
      script.remove(); // Remove after execution to avoid leaving traces
    }
  
    /**
     * Applies canvas fingerprint protection
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
            console.error("Error in canvas noisify:", e);
          }
        }
      }
  
      // Override toDataURL method using defineProperty for maximum compatibility
      Object.defineProperty(HTMLCanvasElement.prototype, 'toDataURL', {
        value: function() {
          noisify(this, this.getContext('2d', { willReadFrequently: true }));
          return origToDataURL.apply(this, arguments);
        }
      });
      
      // Override toBlob method
      Object.defineProperty(HTMLCanvasElement.prototype, 'toBlob', {
        value: function() {
          noisify(this, this.getContext('2d', { willReadFrequently: true }));
          return origToBlob.apply(this, arguments);
        }
      });
      
      // Override getImageData method
      Object.defineProperty(CanvasRenderingContext2D.prototype, 'getImageData', {
        value: function() {
          noisify(this.canvas, this);
          return origGetImageData.apply(this, arguments);
        }
      });
      
      // Also defeat WebGL fingerprinting
      try {
        const getParameter = WebGLRenderingContext.prototype.getParameter;
        Object.defineProperty(WebGLRenderingContext.prototype, 'getParameter', {
          value: function(parameter) {
            // Add noise to vertex and fragment shader precision
            if (parameter === 35724 || parameter === 35725) { // SHADING_LANGUAGE_VERSION
              const result = getParameter.apply(this, arguments);
              return result + ' (' + Math.random().toString(36).substr(2, 8) + ')';
            }
            
            // Return original result for other parameters
            return getParameter.apply(this, arguments);
          }
        });
      } catch (e) {
        console.error("Error applying WebGL protection:", e);
      }
    }
  
    /**
     * Observes dynamically created iframes and applies protection to them
     */
    function observeIFrames() {
      const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
          if (mutation.addedNodes) {
            mutation.addedNodes.forEach((node) => {
              // Check if the added node is an iframe
              if (node.tagName === 'IFRAME') {
                try {
                  // Try to access the iframe's document
                  const iframeWindow = node.contentWindow;
                  const iframeDoc = node.contentDocument || iframeWindow.document;
                  
                  if (iframeDoc) {
                    // Create a script element and inject our protection code
                    const script = iframeDoc.createElement('script');
                    script.textContent = `
                      (${applyCanvasProtectionFunc.toString()})(${JSON.stringify(settings)});
                    `;
                    (iframeDoc.head || iframeDoc.documentElement).appendChild(script);
                    script.remove();
                  }
                } catch (e) {
                  // Cross-origin iframe, cannot access directly
                  // Use the iframe load event as a fallback
                  node.addEventListener('load', () => {
                    try {
                      const iframeWindow = node.contentWindow;
                      if (iframeWindow && !iframeWindow.isProtected) {
                        const script = document.createElement('script');
                        script.textContent = `
                          window.isProtected = true;
                          (${applyCanvasProtectionFunc.toString()})(${JSON.stringify(settings)});
                        `;
                        node.contentDocument.head.appendChild(script);
                        script.remove();
                      }
                    } catch (e) {
                      // Cannot access cross-origin iframe content
                    }
                  });
                }
              }
            });
          }
        });
      });
  
      // Start observing the document with the configured parameters
      observer.observe(document, { childList: true, subtree: true });
    }
  }());
  
  // These are the referenced functions as strings to be injected into the page
  
  function applyCanvasProtectionFunc(settings) {
    // Store original methods before they can be accessed by any script
    const origGetImageData = CanvasRenderingContext2D.prototype.getImageData;
    const origToDataURL = HTMLCanvasElement.prototype.toDataURL;
    const origToBlob = HTMLCanvasElement.prototype.toBlob;
    
    // Override toDataURL method using defineProperty for maximum compatibility
    Object.defineProperty(HTMLCanvasElement.prototype, 'toDataURL', {
      value: function() {
        noisify(this, this.getContext('2d', { willReadFrequently: true }));
        return origToDataURL.apply(this, arguments);
      }
    });
    
    // Override toBlob method
    Object.defineProperty(HTMLCanvasElement.prototype, 'toBlob', {
      value: function() {
        noisify(this, this.getContext('2d', { willReadFrequently: true }));
        return origToBlob.apply(this, arguments);
      }
    });
    
    // Override getImageData method
    Object.defineProperty(CanvasRenderingContext2D.prototype, 'getImageData', {
      value: function() {
        noisify(this.canvas, this);
        return origGetImageData.apply(this, arguments);
      }
    });
    
    // Also defeat WebGL fingerprinting
    try {
      const getParameter = WebGLRenderingContext.prototype.getParameter;
      Object.defineProperty(WebGLRenderingContext.prototype, 'getParameter', {
        value: function(parameter) {
          // Add noise to vertex and fragment shader precision
          if (parameter === 35724 || parameter === 35725) { // SHADING_LANGUAGE_VERSION
            const result = getParameter.apply(this, arguments);
            return result + ' (' + Math.random().toString(36).substr(2, 8) + ')';
          }
          
          // Return original result for other parameters
          return getParameter.apply(this, arguments);
        }
      });
    } catch (e) {
      console.error("Error applying WebGL protection:", e);
    }
  }
  
  function noisifyFunc(canvas, context) {
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
  
  function observeIFramesFunc() {
    const observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        if (mutation.addedNodes) {
          mutation.addedNodes.forEach((node) => {
            // Check if the added node is an iframe
            if (node.tagName === 'IFRAME') {
              try {
                // Try to access the iframe's document
                const iframeWindow = node.contentWindow;
                const iframeDoc = node.contentDocument || iframeWindow.document;
                
                if (iframeDoc) {
                  // Create a script element and inject our protection code
                  const script = iframeDoc.createElement('script');
                  script.textContent = `
                    (${applyCanvasProtectionFunc.toString()})(${JSON.stringify(settings)});
                  `;
                  (iframeDoc.head || iframeDoc.documentElement).appendChild(script);
                  script.remove();
                }
              } catch (e) {
                // Cross-origin iframe, cannot access directly
                // Use the iframe load event as a fallback
                node.addEventListener('load', () => {
                  try {
                    const iframeWindow = node.contentWindow;
                    if (iframeWindow && !iframeWindow.isProtected) {
                      const script = document.createElement('script');
                      script.textContent = `
                        window.isProtected = true;
                        (${applyCanvasProtectionFunc.toString()})(${JSON.stringify(settings)});
                      `;
                      node.contentDocument.head.appendChild(script);
                      script.remove();
                    }
                  } catch (e) {
                    // Cannot access cross-origin iframe content
                  }
                });
              }
            }
          });
        }
      });
    });
  
    // Start observing the document with the configured parameters
    observer.observe(document, { childList: true, subtree: true });
  }