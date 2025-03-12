// canvas-protection.js - Canvas Fingerprint Protection
// This script runs directly in the web page context and applies protections
// It's loaded as an external resource to bypass CSP restrictions

(function() {
    // Exit if protection is already applied
    if (window.canvasProtectionApplied) {
      return;
    }
    
    // Mark as applied to prevent duplicate protection
    window.canvasProtectionApplied = true;
    
    // Get settings from the global variable set by content script
    const settings = window.canvasProtectionSettings || {
      randomCanvasing: true,
      canvasR: 1,
      canvasG: 1,
      canvasB: 1,
      canvasA: 1,
      enabled: true
    };
    
    // Early exit if protection is disabled
    if (!settings.enabled || !settings.canvasing) {
      return;
    }
    
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
      
      // Also apply to WebGL2
      if (window.WebGL2RenderingContext) {
        const getParameterWebGL2 = WebGL2RenderingContext.prototype.getParameter;
        Object.defineProperty(WebGL2RenderingContext.prototype, 'getParameter', {
          value: function(parameter) {
            // Add noise to vertex and fragment shader precision
            if (parameter === 35724 || parameter === 35725) { // SHADING_LANGUAGE_VERSION
              const result = getParameterWebGL2.apply(this, arguments);
              return result + ' (' + Math.random().toString(36).substr(2, 8) + ')';
            }
            
            // Return original result for other parameters
            return getParameterWebGL2.apply(this, arguments);
          }
        });
      }
    } catch (e) {
      console.error("Error applying WebGL protection:", e);
    }
    
    // Monitor for dynamically created iframes
    function observeIFrames() {
      const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
          if (mutation.addedNodes) {
            mutation.addedNodes.forEach((node) => {
              // Check if the added node is an iframe
              if (node.tagName === 'IFRAME') {
                try {
                  // Listen for the iframe to load
                  node.addEventListener('load', () => {
                    try {
                      // Try to access the iframe's document
                      const iframeDocument = node.contentDocument;
                      
                      if (iframeDocument && !iframeDocument._canvasProtected) {
                        iframeDocument._canvasProtected = true;
                        
                        // Transfer our settings
                        const settingsCode = `
                          window.canvasProtectionSettings = ${JSON.stringify(settings)};
                          window.canvasProtectionApplied = false; // Reset this for the child frame
                        `;
                        
                        // Create and inject settings script
                        const settingsScript = iframeDocument.createElement('script');
                        settingsScript.textContent = settingsCode;
                        iframeDocument.head.appendChild(settingsScript);
                        settingsScript.remove();
                        
                        // Now load our protection script in the iframe
                        // Find the extension ID
                        const scriptTags = document.getElementsByTagName('script');
                        let extensionId = '';
                        for (let i = 0; i < scriptTags.length; i++) {
                          const src = scriptTags[i].src;
                          if (src && src.includes('chrome-extension://') && src.includes('canvas-protection.js')) {
                            extensionId = src.split('chrome-extension://')[1].split('/')[0];
                            break;
                          }
                        }
                        
                        if (extensionId) {
                          const protectionScript = iframeDocument.createElement('script');
                          protectionScript.src = `chrome-extension://${extensionId}/canvas-protection.js`;
                          iframeDocument.head.appendChild(protectionScript);
                        }
                      }
                    } catch (e) {
                      // Cannot access cross-origin iframe content, which is expected
                    }
                  });
                } catch (e) {
                  // Error on initial access
                }
              }
            });
          }
        });
      });
  
      // Start observing the document with the configured parameters
      observer.observe(document, { childList: true, subtree: true });
    }
    
    // Start observing for iframes
    observeIFrames();
    
    // Log that protection has been applied (useful for debugging)
    console.debug("Canvas fingerprint protection applied to page");
  })();