// Standalone Canvas Fingerprint Protection
// This script works independently without background page communication

(function() {
    // Default settings (if storage is not accessible)
    let settings = {
      enabled: true,
      canvasing: true,
      randomCanvasing: true,
      canvasR: 1,
      canvasG: 1,
      canvasB: 1,
      canvasA: 1
    };
  
    // Try to get settings from storage, but don't wait for it
    // This makes the protection work even if storage access fails
    try {
      chrome.storage.sync.get(
        ['enabled', 'canvasing', 'randomCanvasing', 'canvasR', 'canvasG', 'canvasB', 'canvasA'],
        (result) => {
          // Only update settings if values exist
          if (result) {
            settings.enabled = result.enabled !== undefined ? result.enabled : settings.enabled;
            settings.canvasing = result.canvasing !== undefined ? result.canvasing : settings.canvasing;
            settings.randomCanvasing = result.randomCanvasing !== undefined ? result.randomCanvasing : settings.randomCanvasing;
            settings.canvasR = result.canvasR !== undefined ? result.canvasR : settings.canvasR;
            settings.canvasG = result.canvasG !== undefined ? result.canvasG : settings.canvasG;
            settings.canvasB = result.canvasB !== undefined ? result.canvasB : settings.canvasB;
            settings.canvasA = result.canvasA !== undefined ? result.canvasA : settings.canvasA;
          }
        }
      );
    } catch (e) {
      // If storage access fails, continue with default settings
      console.log("Using default canvas protection settings");
    }
  
    // Apply protection immediately with default settings
    // This ensures protection is active from the very beginning
    if (settings.enabled && settings.canvasing) {
      applyCanvasProtection();
    }
  
    // Main protection function
    function applyCanvasProtection() {
      // Store original methods (must be done before any other scripts run)
      const origGetImageData = CanvasRenderingContext2D.prototype.getImageData;
      const origToDataURL = HTMLCanvasElement.prototype.toDataURL;
      const origToBlob = HTMLCanvasElement.prototype.toBlob;
      
      // Noise function - the heart of the fingerprint protection
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
            // Silent fail - we don't want to break legitimate canvas usage
          }
        }
      }
  
      // Override getImageData using direct assignment
      CanvasRenderingContext2D.prototype.getImageData = function() {
        noisify(this.canvas, this);
        return origGetImageData.apply(this, arguments);
      };
      
      // Override toDataURL
      HTMLCanvasElement.prototype.toDataURL = function() {
        noisify(this, this.getContext('2d', { willReadFrequently: true }));
        return origToDataURL.apply(this, arguments);
      };
      
      // Override toBlob
      HTMLCanvasElement.prototype.toBlob = function() {
        noisify(this, this.getContext('2d', { willReadFrequently: true }));
        return origToBlob.apply(this, arguments);
      };
      
      // WebGL protection
      try {
        const getParameter = WebGLRenderingContext.prototype.getParameter;
        WebGLRenderingContext.prototype.getParameter = function(parameter) {
          // Only modify shader version strings
          if (parameter === 35724 || parameter === 35725) { // SHADING_LANGUAGE_VERSION
            const result = getParameter.apply(this, arguments);
            return result + ' (' + Math.random().toString(36).substr(2, 8) + ')';
          }
          
          // Return original result for other parameters
          return getParameter.apply(this, arguments);
        };
        
        // Also protect WebGL2
        if (typeof WebGL2RenderingContext !== 'undefined') {
          const getParameterWebGL2 = WebGL2RenderingContext.prototype.getParameter;
          WebGL2RenderingContext.prototype.getParameter = function(parameter) {
            // Only modify shader version strings
            if (parameter === 35724 || parameter === 35725) {
              const result = getParameterWebGL2.apply(this, arguments);
              return result + ' (' + Math.random().toString(36).substr(2, 8) + ')';
            }
            
            // Return original result for other parameters
            return getParameterWebGL2.apply(this, arguments);
          };
        }
      } catch (e) {
        // Silent fail - WebGL might not be available
      }
    }
    
    // Setup iframe protection
    function setupIframeProtection() {
      // Only proceed if we can access the document
      if (!document || !document.documentElement) return;
      
      // Create a MutationObserver to detect dynamically created iframes
      try {
        const observer = new MutationObserver(function(mutations) {
          for (const mutation of mutations) {
            if (!mutation.addedNodes) continue;
            
            for (const node of mutation.addedNodes) {
              // Check if the added node is an iframe
              if (node.tagName === 'IFRAME') {
                // Try to protect the iframe when it loads
                try {
                  node.addEventListener('load', function() {
                    // Skip if already protected
                    if (node.hasAttribute('data-canvas-protected')) return;
                    
                    try {
                      // For same-origin iframes, we can access their document
                      const iframeDoc = node.contentDocument || 
                                       (node.contentWindow ? node.contentWindow.document : null);
                      
                      if (!iframeDoc) return;
                      
                      // Create a script element to apply our protection
                      const script = iframeDoc.createElement('script');
                      
                      // Create a self-contained version of our protection
                      script.textContent = `
                        (function() {
                          // Apply canvas protection
                          const origGetImageData = CanvasRenderingContext2D.prototype.getImageData;
                          const origToDataURL = HTMLCanvasElement.prototype.toDataURL;
                          const origToBlob = HTMLCanvasElement.prototype.toBlob;
                          
                          // Noise function
                          function noisify(canvas, context) {
                            if (!context) return;
                            
                            const shift = {
                              r: ${settings.randomCanvasing} ? Math.floor(Math.random() * 10) - 5 : ${settings.canvasR},
                              g: ${settings.randomCanvasing} ? Math.floor(Math.random() * 10) - 5 : ${settings.canvasG},
                              b: ${settings.randomCanvasing} ? Math.floor(Math.random() * 10) - 5 : ${settings.canvasB},
                              a: ${settings.randomCanvasing} ? Math.floor(Math.random() * 10) - 5 : ${settings.canvasA}
                            };
                            
                            const width = canvas.width;
                            const height = canvas.height;
                            
                            if (width && height) {
                              try {
                                const imageData = origGetImageData.apply(context, [0, 0, width, height]);
                                
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
                                // Silent fail
                              }
                            }
                          }
                          
                          // Override methods
                          CanvasRenderingContext2D.prototype.getImageData = function() {
                            noisify(this.canvas, this);
                            return origGetImageData.apply(this, arguments);
                          };
                          
                          HTMLCanvasElement.prototype.toDataURL = function() {
                            noisify(this, this.getContext('2d', { willReadFrequently: true }));
                            return origToDataURL.apply(this, arguments);
                          };
                          
                          HTMLCanvasElement.prototype.toBlob = function() {
                            noisify(this, this.getContext('2d', { willReadFrequently: true }));
                            return origToBlob.apply(this, arguments);
                          };
                          
                          // WebGL protection
                          try {
                            const getParameter = WebGLRenderingContext.prototype.getParameter;
                            WebGLRenderingContext.prototype.getParameter = function(parameter) {
                              if (parameter === 35724 || parameter === 35725) {
                                const result = getParameter.apply(this, arguments);
                                return result + ' (' + Math.random().toString(36).substr(2, 8) + ')';
                              }
                              return getParameter.apply(this, arguments);
                            };
                          } catch (e) {}
                        })();
                      `;
                      
                      // Add script to iframe document
                      const head = iframeDoc.head || iframeDoc.documentElement;
                      head.appendChild(script);
                      
                      // Remove script after execution
                      script.remove();
                      
                      // Mark as protected
                      node.setAttribute('data-canvas-protected', 'true');
                    } catch (e) {
                      // Cross-origin iframe - can't access its document
                    }
                  });
                } catch (e) {
                  // Error handling iframe
                }
              }
            }
          }
        });
        
        // Start observing document for iframe creation
        observer.observe(document.documentElement, {
          childList: true,
          subtree: true
        });
      } catch (e) {
        // Error creating observer
      }
    }
    
    // Start observing for iframes
    if (settings.enabled && settings.canvasing) {
      if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setupIframeProtection);
      } else {
        setupIframeProtection();
      }
    }
  })();