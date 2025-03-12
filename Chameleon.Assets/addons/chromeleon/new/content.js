// content.js - Fixed to prevent duplicate variable declaration
// We use an IIFE (Immediately Invoked Function Expression) with a check to prevent multiple executions

// Check if the script has already been executed
if (window.canvasProtectionApplied) {
    console.log('Canvas protection already applied, skipping duplicate injection');
  } 
  else
    window.canvasProtectionApplied = true;
    
    console.log('Applying canvas protection in main world');
    // Now execute the protection script in its own scope
    (function() {
      // Store original functions we'll be modifying
      const originalProtoMethods = {
        getContext: HTMLCanvasElement.prototype.getContext,
        toDataURL: HTMLCanvasElement.prototype.toDataURL,
        toBlob: HTMLCanvasElement.prototype.toBlob,
        getImageData: CanvasRenderingContext2D.prototype.getImageData,
        createElement: Document.prototype.createElement
      };
      
      // Get extension settings
      let settings = {
        enableProxyAPI: true,
        enableCSSInjection: true,
        enableShadowDOM: true,
        noiseLevel: 5
      };
      
      chrome.runtime.sendMessage({type: "getSettings"}, (response) => {
        if (response) {
          settings = response;
        }
        applyProtections();
      });
      
      function applyProtections() {
        // 1. JavaScript Proxy API protection
        if (settings.enableProxyAPI) {
          applyProxyProtection();
        }
        
        // 2. CSS Injection protection
        if (settings.enableCSSInjection) {
          applyCSSProtection();
        }
        
        // 6. Shadow DOM Isolation
        if (settings.enableShadowDOM) {
          applyShadowDOMProtection();
        }
      }
      
      // 1. JavaScript Proxy API protection
      function applyProxyProtection() {
        // Intercept getImageData to modify pixel data
        CanvasRenderingContext2D.prototype.getImageData = new Proxy(originalProtoMethods.getImageData, {
          apply(target, thisArg, args) {
            const result = Reflect.apply(target, thisArg, args);
            
            // Add subtle noise to the image data
            const data = result.data;
            const noiseAmount = settings.noiseLevel * 0.1; // Scale to small values
            
            for (let i = 0; i < data.length; i += 4) {
              // Subtle random noise that doesn't visibly affect the image
              data[i] = Math.max(0, Math.min(255, data[i] + (Math.random() * 2 - 1) * noiseAmount));
              data[i+1] = Math.max(0, Math.min(255, data[i+1] + (Math.random() * 2 - 1) * noiseAmount));
              data[i+2] = Math.max(0, Math.min(255, data[i+2] + (Math.random() * 2 - 1) * noiseAmount));
              // Don't modify alpha channel (i+3)
            }
            
            return result;
          }
        });
        
        // Intercept toDataURL to modify the output
        HTMLCanvasElement.prototype.toDataURL = new Proxy(originalProtoMethods.toDataURL, {
          apply(target, thisArg, args) {
            // Add a tiny random offset to the canvas first
            const ctx = thisArg.getContext('2d');
            if (ctx) {
              const imgData = ctx.getImageData(0, 0, thisArg.width, thisArg.height);
              addNoise(imgData.data, settings.noiseLevel);
              ctx.putImageData(imgData, 0, 0);
            }
            
            return Reflect.apply(target, thisArg, args);
          }
        });
        
        // Intercept toBlob similarly
        HTMLCanvasElement.prototype.toBlob = new Proxy(originalProtoMethods.toBlob, {
          apply(target, thisArg, args) {
            const ctx = thisArg.getContext('2d');
            if (ctx) {
              const imgData = ctx.getImageData(0, 0, thisArg.width, thisArg.height);
              addNoise(imgData.data, settings.noiseLevel);
              ctx.putImageData(imgData, 0, 0);
            }
            
            return Reflect.apply(target, thisArg, args);
          }
        });
        
        // Simple context proxy
        function getProxyForContext(context) {
          return new Proxy(context, {
            get(target, prop) {
              if (typeof target[prop] === 'function') {
                return function(...args) {
                  // For drawing operations, add tiny offsets
                  if (['fillRect', 'strokeRect', 'fillText', 'strokeText', 'drawImage'].includes(prop)) {
                    if (args.length >= 2) {
                      args[0] += (Math.random() * 2 - 1) * 0.001 * settings.noiseLevel;
                      args[1] += (Math.random() * 2 - 1) * 0.001 * settings.noiseLevel;
                    }
                  }
                  
                  return target[prop].apply(target, args);
                };
              }
              
              return target[prop];
            }
          });
        }
        
        // Intercept getContext to apply protections earlier
        HTMLCanvasElement.prototype.getContext = function(...args) {
          const context = originalProtoMethods.getContext.apply(this, args);
          
          // Modify context
          if (context && args[0] === '2d') {
            return getProxyForContext(context);
          }
          
          return context;
        };
      }
      
      // Helper function to add noise to image data
      function addNoise(data, level) {
        const noiseAmount = level * 0.1; // Scale to small values
        
        for (let i = 0; i < data.length; i += 4) {
          data[i] = Math.max(0, Math.min(255, data[i] + (Math.random() * 2 - 1) * noiseAmount));
          data[i+1] = Math.max(0, Math.min(255, data[i+1] + (Math.random() * 2 - 1) * noiseAmount));
          data[i+2] = Math.max(0, Math.min(255, data[i+2] + (Math.random() * 2 - 1) * noiseAmount));
          // Don't modify alpha channel (i+3)
        }
      }
      
      // 2. CSS Injection protection
      function applyCSSProtection() {
        const style = document.createElement('style');
        // Apply subtle transformations to canvas elements
        style.textContent = `
          canvas {
            filter: brightness(0.9999) !important;
            transform: scale(0.9999) !important;
            image-rendering: optimizeSpeed !important;
          }
        `;
        document.head.appendChild(style);
      }
      
      // 6. Shadow DOM Isolation
      function applyShadowDOMProtection() {
        // Replace canvas creation with shadow DOM versions
        Document.prototype.createElement = function(tagName, options) {
          if (typeof tagName === 'string' && tagName.toLowerCase() === 'canvas') {
            // Create a host element
            const host = originalProtoMethods.createElement.call(this, 'div');
            host.style.display = 'inline-block';
            
            // Create shadow DOM and add a canvas inside it
            const shadow = host.attachShadow({mode: 'closed'});
            const canvas = originalProtoMethods.createElement.call(this, 'canvas', options);
            shadow.appendChild(canvas);
            
            // Create a proxy to forward operations to the internal canvas
            return getProxyForCanvas(canvas, host);
          }
          
          // Default behavior for other elements
          return originalProtoMethods.createElement.call(this, tagName, options);
        };
      }
      
      // Helper for Shadow DOM approach
      function getProxyForCanvas(canvas, host) {
        // Forward properties and methods to the internal canvas
        const handler = {
          get: function(target, prop) {
            if (prop === 'style') {
              // Synchronize styles between host and canvas
              return new Proxy(host.style, {
                set: function(styleTarget, styleProp, value) {
                  styleTarget[styleProp] = value;
                  canvas.style[styleProp] = value;
                  return true;
                }
              });
            }
            
            if (typeof canvas[prop] === 'function') {
              // Handle methods
              return function(...args) {
                // Special handling for fingerprinting-related functions
                if (prop === 'toDataURL' || prop === 'toBlob') {
                  const ctx = canvas.getContext('2d');
                  if (ctx) {
                    const imgData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                    addNoise(imgData.data, settings.noiseLevel);
                    ctx.putImageData(imgData, 0, 0);
                  }
                }
                
                const result = canvas[prop].apply(canvas, args);
                return result;
              };
            }
            
            // Normal property access
            return canvas[prop];
          },
          
          set: function(target, prop, value) {
            // Set property on the internal canvas
            canvas[prop] = value;
            
            // Also maintain certain properties on host for consistency
            if (prop === 'width' || prop === 'height') {
              host.style[prop] = value + 'px';
            }
            
            return true;
          }
        };
        
        return new Proxy(host, handler);
      }
      
      // Notify that protection has been applied
      try {
        chrome.runtime.sendMessage({
          type: "protectionActive", 
          world: "main",
          timestamp: new Date().toISOString()
        });
      } catch (e) {
        console.log("Canvas protection active in main world");
      }
    })();
  