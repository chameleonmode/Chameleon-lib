// background.js
browser.runtime.onInstalled.addListener(() => {
  console.log("Geckoleon extension installed");
  
  // Initialize default configuration
  browser.storage.local.set({ 
    config: { 
      noise: "mid", 
      rects: { enabled: true, random: false } 
    }
  });
});

// Function to inject our script into the page
async function injectNoiseScript(tabId) {
  try {
    const result = await browser.storage.local.get("config");
    const config = result.config || { 
      noise: "max", 
      rects: { enabled: true, random: false } 
    };
    
    if (!config.rects.enabled) return;
    
    console.log("Injecting noise script with config", config);
    
    // Inject into ALL frames including iframes
    await browser.scripting.executeScript({
      target: { tabId: tabId, allFrames: true },
      world: "MAIN",
      func: setupDirectOverride,
      args: [config.noise, config.rects.random]
    });
  } catch (error) {
    console.error("Error injecting script:", error);
  }
}

// This function directly overrides the getClientRects and getBoundingClientRect methods
function setupDirectOverride(noiseLevel, randomNoise) {
  try {
    console.log("Geckoleon: Direct method override starting", noiseLevel, randomNoise);
    
    // Only run once per frame
    if (window.__geckoleon_methods_overridden) {
      console.log("Geckoleon: Methods already overridden in this frame");
      return;
    }
    window.__geckoleon_methods_overridden = true;
    
    // Map different noise levels
    const noises = {
      nano: Number.EPSILON * 5, // 2.22e-15.5
      mini: Number.EPSILON * 10, // 2.22e-15
      low: Number.EPSILON * 100, // 2.22e-14
      mid: Number.EPSILON * 1000, // 2.22e-13
      bold: Number.EPSILON * 10000, // 2.22e-12
      high: Number.EPSILON * 100000, // 2.22e-11
      ultra: Number.EPSILON * 1000000, // 2.22e-10
      super: 0.000000001, // 1e-9
      max: 0.00000001, // 1e-8
    };
    
    function getNoise() {
      if (randomNoise) {
        const keys = Object.keys(noises);
        return noises[keys[Math.floor(Math.random() * keys.length)]];
      }
      return noises[noiseLevel] || noises.max;
    }
    
    // Store original methods
    const original = {
      getBoundingClientRect: Element.prototype.getBoundingClientRect,
      getClientRects: Element.prototype.getClientRects
    };
    
    // CRITICAL: Override the getClientRects method directly
    Element.prototype.getClientRects = function() {
      // Call the original method
      const originalRects = original.getClientRects.apply(this, arguments);
      
      // Apply noise to each rect
      const noise = getNoise();
      
      // Create a completely new DOMRectList-like object
      const noisyRects = {};
      
      // Set the length property
      Object.defineProperty(noisyRects, 'length', {
        value: originalRects.length
      });
      
      // Add the item method
      noisyRects.item = function(index) {
        if (index >= originalRects.length) return null;
        
        const rect = originalRects[index];
        return createNoisyRect(rect, noise);
      };
      
      // Add index access
      for (let i = 0; i < originalRects.length; i++) {
        noisyRects[i] = createNoisyRect(originalRects[i], noise);
      }
      
      // Make it iterable
      noisyRects[Symbol.iterator] = function*() {
        for (let i = 0; i < originalRects.length; i++) {
          yield noisyRects[i];
        }
      };
      
      if (Math.random() < 0.001) {
        console.log("Geckoleon: Applied noise to getClientRects", noise);
      }
      
      return noisyRects;
    };
    
    // Override getBoundingClientRect
    Element.prototype.getBoundingClientRect = function() {
      const originalRect = original.getBoundingClientRect.apply(this, arguments);
      const noise = getNoise();
      
      return createNoisyRect(originalRect, noise);
    };
    
    // Helper function to create a noisy rect
    function createNoisyRect(originalRect, noise) {
      // Create a new object with the same properties
      const noisyRect = {};
      
      // Apply noise to each property
      ['x', 'y', 'width', 'height', 'top', 'right', 'bottom', 'left'].forEach(prop => {
        if (prop in originalRect) {
          // Use Object.defineProperty to ensure the property appears in Object.keys
          Object.defineProperty(noisyRect, prop, {
            value: originalRect[prop] * (1 + noise),
            enumerable: true
          });
        }
      });
      
      // Copy any other properties
      Object.getOwnPropertyNames(originalRect).forEach(prop => {
        if (!noisyRect.hasOwnProperty(prop) && typeof originalRect[prop] !== 'function') {
          noisyRect[prop] = originalRect[prop];
        }
      });
      
      // Copy methods
      ['toJSON'].forEach(method => {
        if (typeof originalRect[method] === 'function') {
          noisyRect[method] = originalRect[method].bind(originalRect);
        }
      });
      
      return noisyRect;
    }
    
    // Add a test function
    window.geckoleonTest = function() {
      // Create a test element
      const div = document.createElement('div');
      div.style.width = '100px';
      div.style.height = '100px';
      document.body.appendChild(div);
      
      // Test getClientRects
      const rects = div.getClientRects();
      console.log("Test getClientRects:", rects[0]);
      
      // Test getBoundingClientRect
      const rect = div.getBoundingClientRect();
      console.log("Test getBoundingClientRect:", rect);
      
      document.body.removeChild(div);
    };
    
    // Run a test
    setTimeout(window.geckoleonTest, 500);
    
    console.log("Geckoleon: Direct method override complete");
  } catch (error) {
    console.error("Geckoleon override error:", error);
  }
}

// Run on navigation
browser.webNavigation.onCommitted.addListener(details => {
  if (details.url.startsWith('http')) {
    injectNoiseScript(details.tabId);
  }
});

// Run on page load
browser.webNavigation.onDOMContentLoaded.addListener(details => {
  if (details.url.startsWith('http')) {
    injectNoiseScript(details.tabId);
  }
});

// Run for existing tabs
browser.tabs.query({url: ["http://*/*", "https://*/*"]}, tabs => {
  for (const tab of tabs) {
    injectNoiseScript(tab.id);
  }
});