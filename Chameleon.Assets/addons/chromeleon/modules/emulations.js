import { log } from "./logger.js";
import { offsets } from "./offsets.js";

const activeTabs = new Map(); // Track tabs with active debugger sessions

// Clean up when a tab is closed
chrome.tabs.onRemoved.addListener((tabId) => {
  if (activeTabs.has(tabId)) {
    detachDebugger(tabId);
  }
});


// Listen for debugger events
chrome.debugger.onEvent.addListener((debuggeeId, method, params) => {
  const tabId = debuggeeId.tabId;
  
  if (method === "DOM.documentUpdated") {
    // Re-observe mutations when the document is updated
    setupMutationObserver(tabId);
  } else if (method === "Page.frameAttached") {
    // This fires when new frames are attached to the page
    if (params.frameId) {
      // Setup mutation observer in the new frame
      injectObserverIntoFrame(tabId, params.frameId);
    }
  } else if (method === "Runtime.consoleAPICalled") {
    // Log mutations reported from the page
    if (params.type === "log" && params.args && params.args.length > 0) {
      console.log(`Mutation in ${tabId}:`, params.args[0]);
    }
  }
});

async function attachDebugger(tabId) {
  try {
    // Attach debugger to the tab
    await chrome.debugger.attach({tabId}, "1.3");
    
    // Enable required domains
    await chrome.debugger.sendCommand({tabId}, "DOM.enable");
    await chrome.debugger.sendCommand({tabId}, "Runtime.enable");
    await chrome.debugger.sendCommand({tabId}, "Page.enable");
    
    // Setup mutation observers
    await setupMutationObserver(tabId);
    
    // Discover frames and set up observers for them
    await discoverFrames(tabId);
    
    // Track this tab
    activeTabs.set(tabId, true);
    
    // Update the extension icon to show active state
    chrome.action.setBadgeText({text: "ON", tabId});
    chrome.action.setBadgeBackgroundColor({color: "#4CAF50", tabId});
    
    console.log(`Debugger attached to tab ${tabId}`);
  } catch (error) {
    console.error(`Failed to attach debugger to tab ${tabId}:`, error);
  }
}

async function detachDebugger(tabId) {
  try {
    await chrome.debugger.detach({tabId});
    activeTabs.delete(tabId);
    
    // Update the extension icon to show inactive state
    chrome.action.setBadgeText({text: "", tabId});
    
    console.log(`Debugger detached from tab ${tabId}`);
  } catch (error) {
    console.error(`Failed to detach debugger from tab ${tabId}:`, error);
  }
}

async function setupMutationObserver(tabId) {
  // Inject and execute code to set up a MutationObserver in the main document
  const script = `
    // Clean up any existing observer
    if (window.__domMutationObserver) {
      window.__domMutationObserver.disconnect();
    }
    
    // Create a new MutationObserver
    window.__domMutationObserver = new MutationObserver((mutations) => {
      // Log mutations to console which will be captured by the debugger
      console.log({
        source: 'main_document',
        timestamp: new Date().toISOString(),
        mutations: mutations.map(m => ({
          mutation: m,
          type: m.type,
          target: m.target.nodeName,
          addedNodes: m.addedNodes.length,
          removedNodes: m.removedNodes.length,
          attributeName: m.attributeName || null
        }))
      });
    });
    
    // Start observing with all possible mutation types
    window.__domMutationObserver.observe(document.documentElement || document.body || document, {
      childList: true,
      attributes: true,
      characterData: true,
      subtree: true,
      attributeOldValue: true,
      characterDataOldValue: true
    });
    
    "MutationObserver set up for main document";
  `;
  
  try {
    const result = await chrome.debugger.sendCommand({tabId}, "Runtime.evaluate", {
      expression: script,
      returnByValue: true
    });
    console.log(`Main document observer setup result:`, result);
  } catch (error) {
    console.error(`Failed to set up mutation observer in tab ${tabId}:`, error);
  }
}

// This function is replaced by injectObserverIntoFrame
async function discoverFrames(tabId) {
  try {
    // Use Page.getResourceTree to get all frames
    const result = await chrome.debugger.sendCommand({tabId}, "Page.getResourceTree");
    
    if (result && result.frameTree) {
      // Set up observers for the main frame's child frames
      if (result.frameTree.childFrames) {
        for (const childFrame of result.frameTree.childFrames) {
          await injectObserverIntoFrame(tabId, childFrame.frame.id);
        }
      }
    }
  } catch (error) {
    console.error(`Failed to discover frames in tab ${tabId}:`, error);
  }
}

async function injectObserverIntoFrame(tabId, frameId) {
  try {
    // Execute script within a specific frame
    const script = `
      // Clean up any existing observer
      if (window.__domMutationObserver) {
        window.__domMutationObserver.disconnect();
      }
      
      // Create a new MutationObserver
      window.__domMutationObserver = new MutationObserver((mutations) => {
        // Log mutations to console which will be captured by the debugger
        console.log({
          source: 'iframe',
          frameId: '${frameId}',
          timestamp: new Date().toISOString(),
          mutations: mutations.map(m => ({
            type: m.type,
            target: m.target.nodeName,
            addedNodes: m.addedNodes.length,
            removedNodes: m.removedNodes.length,
            attributeName: m.attributeName || null
          }))
        });
      });
      
      // Start observing with all possible mutation types
      if (document.documentElement || document.body || document) {
        window.__domMutationObserver.observe(document.documentElement || document.body || document, {
          childList: true,
          attributes: true,
          characterData: true,
          subtree: true,
          attributeOldValue: true,
          characterDataOldValue: true
        });
        return "MutationObserver set up for iframe";
      } else {
        return "No document found in frame";
      }
    `;
    
    const result = await chrome.debugger.sendCommand({tabId}, "Page.createIsolatedWorld", {
      frameId: frameId,
      worldName: "MutationObserverWorld"
    });
    
    const worldId = result.executionContextId;
    
    await chrome.debugger.sendCommand({tabId}, "Runtime.evaluate", {
      expression: script,
      contextId: worldId,
      returnByValue: true
    });
    
    console.log(`Frame observer setup for frameId: ${frameId}`);
  } catch (error) {
    console.error(`Failed to set up mutation observer in frame ${frameId}:`, error);
  }
}

// Listen for frame navigation events to refresh observers
chrome.debugger.onEvent.addListener((debuggeeId, method, params) => {
  if (method === "Page.frameNavigated") {
    // Frame navigated, refresh the observer for this specific frame
    if (params.frame && params.frame.id) {
      injectObserverIntoFrame(debuggeeId.tabId, params.frame.id);
    }
  }
});

async function onEvent(tab) {
  if (!tab.url || tab.url.indexOf("chrome://") >= 0) return;

  // First detach any existing debugger
  if (!activeTabs.has(tab.id)) {
    await attachDebugger(tab.id);
  }

  // Set up the emulation
  const { tzEmulation, timezone, tzSystem, tzLocale, tzRandomize } = await chrome.storage.local.get([
    "tzEmulation",
    "timezone",
    "tzSystem",
    "tzRandomize",
    "tzLocale",
  ]);
  if (tzEmulation) {
    log.info(`Applying timezone emulation for tab ${tab.id}`);
    log.info(`Timezone: ${timezone}`);
    log.info(`System timezone: ${tzSystem}`);
    log.info(`Randomize timezone: ${tzRandomize}`);
    log.info(`Locale: ${tzLocale}`);
    await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setTimezoneOverride", {
      timezoneId: tzSystem
        ? Intl.DateTimeFormat().resolvedOptions().timeZone
        : tzRandomize
        ? Object.keys(offsets)[Math.floor(Math.random() * Object.keys(offsets).length)]
        : timezone,
    });
    await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setLocaleOverride", {
      locale: tzLocale,
    });
  }

  const { geoEmulation, lat, lon, geoRandomize, geoAccuracy } = await chrome.storage.local.get([
    "geoEmulation",
    "lat",
    "lon",
    "geoRandomize",
    "geoAccuracy",
  ]);
  if (geoEmulation) {
    log.info(`Applying geolocation emulation for tab ${tab.id}`);
    log.info(`Latitude: ${lat}`);
    log.info(`Longitude: ${lon}`);
    log.info(`Randomize geolocation: ${geoRandomize}`);
    log.info(`Accuracy: ${geoAccuracy}`);
    await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setGeolocationOverride", {
      latitude: geoRandomize ? lat + (Math.random() - 0.5) * geoRandomize : lat,
      longitude: geoRandomize ? lon + (Math.random() - 0.5) * geoRandomize : lon,
      accuracy: geoAccuracy,
    });
  }
}

// Background script for monitoring canvas operations across all frames
async function monitorCanvasOperations(tabId) {
  try {
    // Enable necessary domains
    await chrome.debugger.sendCommand({ tabId: tabId }, "DOM.enable");
    await chrome.debugger.sendCommand({ tabId: tabId }, "Runtime.enable");
    await chrome.debugger.sendCommand({ tabId: tabId }, "Page.enable");

    // Function to inject canvas monitoring script into a frame
    async function injectCanvasMonitor(frameId = null) {
      // Get the document in the specified frame
      const params = {
        depth: -1,
        pierce: true,
      };

      // Add frameId parameter only when specified
      if (frameId) {
        params.frameId = frameId;
      }

      const response = await chrome.debugger.sendCommand({ tabId: tabId }, "DOM.getDocument", params);

      if (!response || !response.root || !response.root.nodeId) {
        throw new Error("Invalid DOM document response");
      }

      // Convert the DOM nodeId to an objectId
      const resolveResponse = await chrome.debugger.sendCommand({ tabId: tabId }, "DOM.resolveNode", {
        nodeId: response.root.nodeId,
      });

      if (!resolveResponse || !resolveResponse.object || !resolveResponse.object.objectId) {
        throw new Error("Failed to resolve node to object");
      }

      const { object } = resolveResponse;

      // Inject the monitor function
      const { result } = await chrome.debugger.sendCommand({ tabId: tabId }, "Runtime.callFunctionOn", {
        objectId: object.objectId,
        functionDeclaration: `
          function() {
            // Log that we're starting canvas monitoring
            console.log('[CanvasMonitor] Starting canvas monitoring in', window.location.href);
            
            // Store original canvas prototype methods
            if (!window._canvasMonitorInitialized) {
              window._canvasMonitorInitialized = true;
              
              // Track all created canvases
              window._monitoredCanvases = new Set();
              
              // Original methods we want to intercept
              const originalHTMLCanvasElementProto = HTMLCanvasElement.prototype;
              const originalCanvasRenderingContext2DProto = CanvasRenderingContext2D.prototype;
              
              // Store original methods
              const originalGetContext = originalHTMLCanvasElementProto.getContext;
              const originalToDataURL = originalHTMLCanvasElementProto.toDataURL;
              const originalToBlob = originalHTMLCanvasElementProto.toBlob;
              const originalGetImageData = originalCanvasRenderingContext2DProto.getImageData;
              const originalPutImageData = originalCanvasRenderingContext2DProto.putImageData;
              const originalDrawImage = originalCanvasRenderingContext2DProto.drawImage;
              const originalFillText = originalCanvasRenderingContext2DProto.fillText;
              const originalFillRect = originalCanvasRenderingContext2DProto.fillRect;
              
              // Keep track of operations on each canvas
              function logOperation(canvas, operation, args) {
                const canvasInfo = {
                  id: canvas.id || 'unnamed',
                  width: canvas.width,
                  height: canvas.height,
                  operation: operation,
                  args: Array.from(args).map(arg => {
                    if (typeof arg === 'string') return arg;
                    if (arg instanceof ImageData) return 'ImageData(' + arg.width + 'x' + arg.height + ')';
                    if (arg instanceof HTMLImageElement) return 'Image(' + arg.src.substring(0, 30) + '...)';
                    if (arg instanceof HTMLCanvasElement) return 'Canvas(' + arg.width + 'x' + arg.height + ')';
                    return String(arg);
                  }),
                  timestamp: new Date().toISOString(),
                  url: window.location.href,
                  stackTrace: new Error().stack
                };
                
                console.log('[CanvasMonitor] Operation:', JSON.stringify(canvasInfo));
                
                // If this is a fingerprinting-like operation, log more details
                if (
                  (operation === 'fillText' && args[0] && args[0].includes('Leak')) ||
                  (operation === 'toDataURL' && canvas.width === 220 && canvas.height === 30)
                ) {
                  console.log('[CanvasMonitor] POTENTIAL FINGERPRINTING DETECTED!', canvasInfo);
                }
              }
              
              // Intercept canvas creation
              const originalCreateElement = Document.prototype.createElement;
              Document.prototype.createElement = function(tagName, options) {
                const element = originalCreateElement.call(this, tagName, options);
                if (tagName.toLowerCase() === 'canvas') {
                  console.log('[CanvasMonitor] Canvas created:', element);
                  window._monitoredCanvases.add(element);
                }
                return element;
              };
              
              // Intercept getContext
              HTMLCanvasElement.prototype.getContext = function() {
                const context = originalGetContext.apply(this, arguments);
                logOperation(this, 'getContext', arguments);
                return context;
              };
              
              // Intercept toDataURL
              HTMLCanvasElement.prototype.toDataURL = function() {
                logOperation(this, 'toDataURL', arguments);
                return originalToDataURL.apply(this, arguments);
              };
              
              // Intercept toBlob
              HTMLCanvasElement.prototype.toBlob = function() {
                logOperation(this, 'toBlob', arguments);
                return originalToBlob.apply(this, arguments);
              };
              
              // Intercept getImageData
              CanvasRenderingContext2D.prototype.getImageData = function() {
                logOperation(this.canvas, 'getImageData', arguments);
                return originalGetImageData.apply(this, arguments);
              };
              
              // Intercept putImageData
              CanvasRenderingContext2D.prototype.putImageData = function() {
                logOperation(this.canvas, 'putImageData', arguments);
                return originalPutImageData.apply(this, arguments);
              };
              
              // Intercept drawImage
              CanvasRenderingContext2D.prototype.drawImage = function() {
                logOperation(this.canvas, 'drawImage', arguments);
                return originalDrawImage.apply(this, arguments);
              };
              
              // Intercept fillText
              CanvasRenderingContext2D.prototype.fillText = function() {
                logOperation(this.canvas, 'fillText', arguments);
                return originalFillText.apply(this, arguments);
              };
              
              // Intercept fillRect
              CanvasRenderingContext2D.prototype.fillRect = function() {
                logOperation(this.canvas, 'fillRect', arguments);
                return originalFillRect.apply(this, arguments);
              };
              
              // Set up MutationObserver to watch for dynamically added canvases
              const observer = new MutationObserver(mutations => {
                mutations.forEach(mutation => {
                  if (mutation.type === 'childList') {
                    mutation.addedNodes.forEach(node => {
                      // Check if node is a canvas
                      if (node.nodeName === 'CANVAS') {
                        console.log('[CanvasMonitor] Canvas added to DOM:', node);
                        window._monitoredCanvases.add(node);
                      }
                      
                      // Check for canvases within added nodes
                      if (node.querySelectorAll) {
                        const canvases = node.querySelectorAll('canvas');
                        canvases.forEach(canvas => {
                          console.log('[CanvasMonitor] Canvas found in added node:', canvas);
                          window._monitoredCanvases.add(canvas);
                        });
                      }
                    });
                  }
                });
              });
              
              // Start observing
              observer.observe(document.documentElement || document, {
                childList: true,
                subtree: true
              });
              
              // Look for existing canvases
              const existingCanvases = document.querySelectorAll('canvas');
              existingCanvases.forEach(canvas => {
                console.log('[CanvasMonitor] Existing canvas found:', canvas);
                window._monitoredCanvases.add(canvas);
              });
              
              console.log('[CanvasMonitor] Initialized, found', existingCanvases.length, 'existing canvases');
            }
            
            return 'Canvas monitoring active in ' + window.location.href;
          }
        `,
        returnByValue: true,
      });

      console.log("Canvas monitor injected into frame, result:", result.value);
      return result.value;
    }

    // First inject into main frame
    await injectCanvasMonitor();

    // Get all frames in the page
    const { frameTree } = await chrome.debugger.sendCommand({ tabId: tabId }, "Page.getFrameTree");

    // Function to process all frames recursively
    async function processFrames(frame) {
      // Skip the main frame as we already injected into it
      if (frame.frame.parentId) {
        try {
          await injectCanvasMonitor(frame.frame.id);
        } catch (error) {
          console.error("Error injecting into frame", frame.frame.id, error);
        }
      }

      // Process child frames
      if (frame.childFrames) {
        for (const childFrame of frame.childFrames) {
          await processFrames(childFrame);
        }
      }
    }

    // Process all frames
    await processFrames(frameTree);

    // Listen for new frames being created
    chrome.debugger.onEvent.addListener((source, method, params) => {
      if (source.tabId === tabId && method === "Page.frameAttached") {
        console.log("New frame attached:", params.frameId);

        // Track frame loading state
        const frameId = params.frameId;

        // First wait for the frame to navigate
        chrome.debugger.onEvent.addListener(function frameNavigatedListener(source, method, navParams) {
          if (
            source.tabId === tabId &&
            method === "Page.frameNavigated" &&
            navParams.frame &&
            navParams.frame.id === frameId
          ) {
            console.log("Frame navigated:", frameId, navParams.frame.url);

            // Remove this listener since we got the navigation event
            chrome.debugger.onEvent.removeListener(frameNavigatedListener);

            // Give the frame more time to fully load its DOM
            setTimeout(() => {
              // Try to inject the monitor
              injectCanvasMonitor(frameId).catch((error) => {
                console.log("Initial injection into frame failed, will retry:", frameId, error);

                // If it fails, try again with an even longer delay
                setTimeout(() => {
                  injectCanvasMonitor(frameId).catch((error) => {
                    console.error("Error injecting into frame after retry:", frameId, error);
                  });
                }, 1500);
              });
            }, 1000);
          }
        });
      }
    });

    console.log("Canvas monitor setup complete for tab", tabId);

    // Return a detach function for cleanup
    return {
      detach: async () => {
        try {
          await chrome.debugger.detach({ tabId: tabId });
          console.log("Debugger detached from tab", tabId);
        } catch (error) {
          console.error("Error detaching debugger:", error);
        }
      },
    };
  } catch (error) {
    console.error("Error setting up canvas monitor:", error);
    throw error;
  }
}

chrome.tabs.query({}, async (tabs) => {
  for (const tab of tabs) {
    await onEvent(tab);
  }
});

chrome.tabs.onCreated.addListener(async (tab) => {
  await onEvent(tab);
});

chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
  if (changeInfo.status === "loading") {
    await onEvent(tab);
  }
});

// Clean up when done
chrome.debugger.onDetach.addListener((debuggee, reason) => {
  log.info("Debugger detached:", reason);
});

// chrome.storage.onChanged.addListener((changes, namespace) => {
//   if (namespace === 'local') {
//     chrome.tabs.query({}, async (tabs) => {
//       for (const tab of tabs) {
//         if (!tab.url || tab.url.indexOf("chrome://") >= 0) continue;
//         await onEvent(tab);
//       }
//     });
//   }
//   return true;
// });
