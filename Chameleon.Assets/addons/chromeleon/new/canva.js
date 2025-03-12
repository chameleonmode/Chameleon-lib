// content-script.js - This runs in each frame
(async function() {
  // Get settings from background script
  const settings = await getSettingsFromBackground();
  
  // Apply protection in this frame
  applyCanvasProtection(settings);
  
  // Also inject directly into page context to bypass isolation
  injectPageScript(settings);
  
  // Monitor for dynamically created iframes
  observeIFrames(settings);
  
  /**
   * Get settings from background script via messaging
   */
  async function getSettingsFromBackground() {
    return new Promise((resolve) => {
      chrome.runtime.sendMessage({action: "getCanvasSettings"}, (response) => {
        if (response && response.settings) {
          resolve(response.settings);
        } else {
          // Default settings if communication fails
          resolve({
            canvasing: true,
            randomCanvasing: true,
            canvasR: 1,
            canvasG: 1,
            canvasB: 1,
            canvasA: 1,
            enabled: true
          });
        }
      });
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
        applyCanvasProtectionFunc(settings);
        
        // Monitor for dynamically created iframes
        ${observeIFramesFunc.toString()}
        observeIFramesFunc();
      })();
    `;
    
    try {
      // Try to inject directly first with inline script
      const script = document.createElement('script');
      script.textContent = scriptContent;
      (document.head || document.documentElement).appendChild(script);
      script.remove();
    } catch (e) {
      console.error("Direct injection failed:", e);
      
      // Try alternative injection methods if CSP blocks the first attempt
      try {
        // Create a blob URL (might work in some contexts)
        const blob = new Blob([scriptContent], {type: 'text/javascript'});
        const url = URL.createObjectURL(blob);
        const blobScript = document.createElement('script');
        blobScript.src = url;
        (document.head || document.documentElement).appendChild(blobScript);
        
        // Clean up
        blobScript.onload = () => {
          URL.revokeObjectURL(url);
          blobScript.remove();
        };
      } catch (e2) {
        console.error("All injection methods failed:", e2);
      }
    }
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
 * Function that gets stringified and injected into the page context
 */
function applyCanvasProtectionFunc(settings) {
  // Set a flag to avoid duplicate injections
  if (window.canvasProtectionApplied) {
    return;
  }
  window.canvasProtectionApplied = true;
  
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
 * Helper function for stringification that just contains the noisify logic
 */
function noisifyFunc() {
  // This function is just for stringification
  // The actual implementation is inside applyCanvasProtection and applyCanvasProtectionFunc
}

/**
 * Observes dynamically created iframes and applies protection to them
 */
function observeIFrames(settings) {
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
                  // This is expected for cross-origin iframes due to browser security restrictions
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

/**
 * Function that gets stringified and injected to observe iframes in the page context
 */
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
}());