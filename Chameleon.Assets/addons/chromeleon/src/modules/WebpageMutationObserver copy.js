// WebpageMutationObserver.js
// A module for monitoring iframe mutations and createElement calls in a Chrome extension

class WebpageMutationObserver {
  /**
   * Create a new WebpageMutationObserver instance
   */
  constructor() {
    // Tab and tracking information
    this.tabId = null;
    this.iframes = new Map(); // Track iframe nodes by nodeId
    this.contentDocumentMap = new Map(); // Map contentDocumentId -> iframeNodeId

    // Event handling
    this.listeners = new Set(); // Store mutation listeners
    this._boundEventHandler = this._handleDebuggerEvent.bind(this);

    // Polling for createElement events
    this._globalPollingIntervalId = null;

    this.canvasProtection = `
  (function () {
    // Store original methods
    const originalMethods = {
      getContext: HTMLCanvasElement.prototype.getContext,
      toDataURL: HTMLCanvasElement.prototype.toDataURL,
      getImageData: CanvasRenderingContext2D.prototype.getImageData,
      fillText: CanvasRenderingContext2D.prototype.fillText,
      fillRect: CanvasRenderingContext2D.prototype.fillRect,
    };
  
    // Helper function to add noise to image data
    function addNoiseToImageData(imageData, noiseLevel = 1) {
      // Create a copy of the data to avoid modifying the original
      const data = new Uint8ClampedArray(imageData.data);
  
      // Add slight random noise to pixel values
      // Only modify a small percentage of pixels to maintain visual similarity
      for (let i = 0; i < data.length; i += 4) {
        // Only modify if random value is less than 0.1 (10% of pixels)
        if (Math.random() < 0.1) {
          // Add small random offset to RGB values
          data[i] = Math.max(0, Math.min(255, data[i] + (Math.random() * 2 - 1) * noiseLevel));
          data[i + 1] = Math.max(0, Math.min(255, data[i + 1] + (Math.random() * 2 - 1) * noiseLevel));
          data[i + 2] = Math.max(0, Math.min(255, data[i + 2] + (Math.random() * 2 - 1) * noiseLevel));
          // Don't modify alpha channel (i+3) to keep transparency intact
        }
      }
  
      return new ImageData(data, imageData.width, imageData.height);
    }
  
    // Override toDataURL to add slight randomization
    HTMLCanvasElement.prototype.toDataURL = function (type, quality) {
      // Get the original image data
      const ctx = this.getContext("2d");
      const imageData = ctx.getImageData(0, 0, this.width, this.height);
  
      // Modify the image data
      const modifiedImageData = addNoiseToImageData(imageData);
  
      // Apply the modified data back to the canvas
      ctx.putImageData(modifiedImageData, 0, 0);
  
      // Call the original method
      return originalMethods.toDataURL.apply(this, arguments);
    };
  
    // Override getImageData to add slight randomization
    CanvasRenderingContext2D.prototype.getImageData = function (x, y, width, height) {
      // Call the original method
      const imageData = originalMethods.getImageData.call(this, x, y, width, height);
  
      return addNoiseToImageData(imageData);
    };
  
    // Add slight offset to text positioning
    CanvasRenderingContext2D.prototype.fillText = function (text, x, y, maxWidth) {
      // Add a small random offset
      const offsetX = x + (Math.random() * 0.2 - 0.1);
      const offsetY = y + (Math.random() * 0.2 - 0.1);
  
      return originalMethods.fillText.call(this, text, offsetX, offsetY, maxWidth);
    };
  
    // Modify rectangle drawing slightly
    CanvasRenderingContext2D.prototype.fillRect = function (x, y, width, height) {
      // Very slight modifications to dimensions
      const newX = x + (Math.random() * 0.2 - 0.1);
      const newY = y + (Math.random() * 0.2 - 0.1);
      const newWidth = width + (Math.random() * 0.4 - 0.2);
      const newHeight = height + (Math.random() * 0.4 - 0.2);
  
      return originalMethods.fillRect.call(this, newX, newY, newWidth, newHeight);
    };
  
    // Add property to indicate the canvas is being protected
    Object.defineProperty(HTMLCanvasElement.prototype, "_protected", {
      value: true,
      enumerable: false,
    });
  
    console.log("[Canvas Fingerprint Protection] Initialized");
  })();
  `;
  }

  /**
   * Initialize the observer for a specific tab
   * @param {number} tabId - The ID of the tab to observe
   * @returns {Promise<boolean>} - Whether initialization was successful
   */
  async initialize(tabId) {
    this.tabId = tabId;

    try {
      // Note: We assume the debugger is already attached and domains enabled as per requirements

      // Set up event listeners for DOM mutations
      chrome.debugger.onEvent.addListener(this._boundEventHandler);

      // 1. Set up parent page listener for iframe messages
      await this._setupParentPageListener();

      // 2. Find and monitor existing iframes
      await this._findAndMonitorExistingIframes();

      // 3. Monitor future iframes with script injection
      await this._setupNewDocumentScriptInjection();

      // 4. Set up polling for createElement events
      this._setupPollingForEvents();

      this._log("Observer successfully initialized");
      return true;
    } catch (error) {
      console.error("[WebpageMutationObserver] Initialization failed:", error);
      this.dispose(); // Clean up if initialization fails
      return false;
    }
  }

  /**
   * Add a listener for iframe mutations
   * @param {Function} callback - Function to call when mutations occur
   * @returns {boolean} - Whether the listener was added successfully
   */
  addMutationListener(callback) {
    if (typeof callback === "function") {
      this.listeners.add(callback);
      this._log(`Added mutation listener, now ${this.listeners.size} listeners`);
      return true;
    }
    return false;
  }

  /**
   * Remove a previously added mutation listener
   * @param {Function} callback - The listener to remove
   * @returns {boolean} - Whether the listener was found and removed
   */
  removeMutationListener(callback) {
    return this.listeners.delete(callback);
  }

  /**
   * Clean up all resources used by the observer
   */
  dispose() {
    try {
      // Remove event listener
      chrome.debugger.onEvent.removeListener(this._boundEventHandler);

      // Clean up polling interval
      if (this._globalPollingIntervalId) {
        clearInterval(this._globalPollingIntervalId);
        this._globalPollingIntervalId = null;
      }

      // Clear all data structures
      this.iframes.clear();
      this.contentDocumentMap.clear();
      this.listeners.clear();
      this.tabId = null;

      this._log("Successfully disposed resources");
    } catch (error) {
      console.error("[WebpageMutationObserver] Error during disposal:", error);
    }
  }

  // ------------------------------------------------------------------------
  // PRIVATE METHODS - LOGGING
  // ------------------------------------------------------------------------

  /**
   * Log a message if debug mode is enabled
   * @param {string} message - The message to log
   * @private
   */
  _log(message) {
    if (this.debug) {
      console.log(`[WebpageMutationObserver] ${message}`);
    }
  }

  // ------------------------------------------------------------------------
  // PRIVATE METHODS - SETUP
  // ------------------------------------------------------------------------

  /**
   * Set up listener in the parent page for messages from iframes
   * @private
   */
  async _setupParentPageListener() {
    try {
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Runtime.evaluate", {
        expression: `
              (function() {
                // Set up message listener in the parent page if not already done
                if (!window._iframeCreateElementListenerAdded) {
                  // Add event listener for postMessage from iframes
                  window.addEventListener('message', function(event) {
                    if (event.data && event.data.type === 'iframe-create-element') {
                      console.log('[WebpageMutationObserver] Detected createElement in iframe:', event.data);
                      
                      // Store events directly in window object for easier polling
                      if (!window._iframeCreateElementEvents) {
                        window._iframeCreateElementEvents = [];
                      }
                      
                      window._iframeCreateElementEvents.push(event.data);
                    }
                  });
                  
                  // Clear any previous events
                  window._iframeCreateElementEvents = [];
                  
                  window._iframeCreateElementListenerAdded = true;
                  console.log('[WebpageMutationObserver] Parent page listener initialized');
                }
                return window._iframeCreateElementListenerAdded;
              })();
            `,
        returnByValue: true,
      });

      this._log("Parent page listener set up successfully");
    } catch (error) {
      console.error("[WebpageMutationObserver] Failed to set up parent page listener:", error);
      throw error;
    }
  }

  /**
   * Set up script injection for new frames that will be created
   * @private
   */
  async _setupNewDocumentScriptInjection() {
    try {
      const scriptId = await chrome.debugger.sendCommand(
        { tabId: this.tabId },
        "Page.addScriptToEvaluateOnNewDocument",
        {
          source: `
              (function() {
                // Only run in iframe contexts, not the main page
                if (window !== window.top) {
                  try {
                    console.log('[WebpageMutationObserver] New iframe detected, injecting createElement monitor');
                    ${this.canvasProtection}
                    
                    // Only patch once per document
                    if (!Document.prototype._createElementMonitored) {
                      // Store original method
                      const originalCreateElement = Document.prototype.createElement;
                      Document.prototype._createElementMonitored = true;
                      
                      // Override createElement
                      Document.prototype.createElement = function(tagName, options) {
                        // Call original method
                        const element = originalCreateElement.call(this, tagName, options);
                        
                        // Notify parent about this element creation
                        try {
                          window.parent.postMessage({
                            type: 'iframe-create-element',
                            frameLocation: window.location.href,
                            tagName: tagName,
                            timestamp: Date.now()
                          }, '*');
                          
                          console.log('[WebpageMutationObserver] Created element:', tagName);
                        } catch(e) {
                          console.error('[WebpageMutationObserver] Error sending createElement event:', e);
                        }
                        
                        return element;
                      };
                      
                      console.log('[WebpageMutationObserver] createElement monitoring injected in new iframe');
                    }
                  } catch(e) {
                    console.error('[WebpageMutationObserver] Error in new iframe script:', e);
                  }
                }
              })();
            `,
        }
      );

      this._log("New document script injection set up");
    } catch (error) {
      console.error("[WebpageMutationObserver] Failed to set up script injection:", error);
      throw error;
    }
  }

  /**
   * Find and monitor all existing iframes in the page
   * @private
   */
  async _findAndMonitorExistingIframes() {
    try {
      // 1. First find all iframe DOM nodes
      await this._findIframeNodes();

      // 2. Then inject monitoring into all existing frames
      await this._injectIntoExistingFrames();

      this._log("Existing iframes found and monitored");
    } catch (error) {
      console.error("[WebpageMutationObserver] Failed to find and monitor existing iframes:", error);
      throw error;
    }
  }

  /**
   * Find all iframe DOM nodes in the page
   * @private
   */
  async _findIframeNodes() {
    try {
      // Clear existing iframe tracking
      this.iframes.clear();
      this.contentDocumentMap.clear();

      // Get the document node with full subtree
      const { root } = await chrome.debugger.sendCommand(
        { tabId: this.tabId },
        "DOM.getDocument",
        { depth: -1 } // Get the full tree
      );

      // Find all iframe nodes in the DOM tree
      this._findIframesInNodeRecursively(root);

      this._log(`Found ${this.iframes.size} iframe nodes in DOM`);
    } catch (error) {
      console.error("[WebpageMutationObserver] Failed to find iframe nodes:", error);
      throw error;
    }
  }

  /**
   * Recursively search for iframe elements in the DOM tree
   * @param {Object} node - The current DOM node to check
   * @private
   */
  _findIframesInNodeRecursively(node) {
    if (!node) return;

    // Check if this node is an iframe
    if (node.nodeName && node.nodeName.toLowerCase() === "iframe") {
      this._trackIframeNode(node);
    }

    // Check children
    if (node.children) {
      for (const child of node.children) {
        this._findIframesInNodeRecursively(child);
      }
    }

    // Check contentDocument if this is an iframe
    if (node.contentDocument) {
      this._findIframesInNodeRecursively(node.contentDocument);
    }
  }

  /**
   * Track an iframe DOM node
   * @param {Object} node - The iframe DOM node
   * @private
   */
  _trackIframeNode(node) {
    try {
      // Extract src attribute if available
      const { attributes } = node;
      let src = null;

      if (attributes) {
        for (let i = 0; i < attributes.length; i += 2) {
          if (attributes[i] === "src") {
            src = attributes[i + 1];
            break;
          }
        }
      }

      // Store iframe information
      this.iframes.set(node.nodeId, {
        nodeId: node.nodeId,
        contentDocumentId: null,
        src: src,
        url: src || "about:blank", // Default to about:blank if no src
        frameId: null, // Will be populated later if possible
      });

      this._log(`Tracking iframe: ${node.nodeId}, src: ${src || "none"}`);
    } catch (error) {
      console.error("[WebpageMutationObserver] Failed to track iframe node:", error);
    }
  }

  /**
   * Inject createElement monitoring into all existing frames
   * @private
   */
  async _injectIntoExistingFrames() {
    try {
      // Get all frames in the page
      const { frameTree } = await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.getFrameTree");

      // Helper to extract all frames from the tree
      const extractFrames = (tree) => {
        const frames = [];

        // Add the current frame
        frames.push({
          id: tree.frame.id,
          url: tree.frame.url,
          parentId: tree.frame.parentId || null,
        });

        // Add child frames recursively
        if (tree.childFrames) {
          for (const child of tree.childFrames) {
            frames.push(...extractFrames(child));
          }
        }

        return frames;
      };

      const frames = extractFrames(frameTree);
      this._log(`Found ${frames.length} frames in the page`);

      // Skip the main frame (it has no parentId)
      const childFrames = frames.filter((frame) => frame.parentId !== null);
      this._log(`Will inject into ${childFrames.length} child frames`);

      // Map frame IDs to DOM node IDs if possible
      for (const [nodeId, iframe] of this.iframes.entries()) {
        for (const frame of childFrames) {
          if (iframe.src && frame.url && (iframe.src === frame.url || frame.url.startsWith(iframe.src))) {
            iframe.frameId = frame.id;
            this._log(`Matched iframe ${nodeId} to frame ${frame.id}`);
            break;
          }
        }
      }

      // For each child frame, inject our monitoring script
      for (const frame of childFrames) {
        try {
          this._log(`Injecting into frame: ${frame.url}`);

          const result = await chrome.debugger.sendCommand({ tabId: this.tabId }, "Runtime.evaluate", {
            expression: `
                  (function() {
                    try {
                      console.log('[WebpageMutationObserver] Injecting createElement monitor in existing frame');
                      
                      // Only patch once per document
                      if (!Document.prototype._createElementMonitored) {
                        // Store original method
                        const originalCreateElement = Document.prototype.createElement;
                        Document.prototype._createElementMonitored = true;
                        
                        // Override createElement
                        Document.prototype.createElement = function(tagName, options) {
                          // Call original method
                          const element = originalCreateElement.call(this, tagName, options);
                          
                          // Notify parent about this element creation
                          try {
                            window.parent.postMessage({
                              type: 'iframe-create-element',
                              frameLocation: window.location.href,
                              tagName: tagName,
                              timestamp: Date.now()
                            }, '*');
                            
                            console.log('[WebpageMutationObserver] Created element:', tagName);
                          } catch(e) {
                            console.error('[WebpageMutationObserver] Error sending createElement event:', e);
                          }
                          
                          return element;
                        };
                        
                        console.log('[WebpageMutationObserver] createElement monitoring injected in existing frame');
                        return true;
                      } else {
                        console.log('[WebpageMutationObserver] createElement already monitored in this frame');
                        return false;
                      }
                    } catch(e) {
                      console.error('[WebpageMutationObserver] Error injecting createElement monitor:', e);
                      return false;
                    }
                  })();
                `,
            frameId: frame.id,
            returnByValue: true,
          });

          this._log(`Injection result for frame ${frame.id}: ${JSON.stringify(result)}`);
          this._log(`Successfully injected into frame: ${frame.url}`);
        } catch (error) {
          // This is expected for cross-origin iframes
          this._log(`Could not inject into frame ${frame.id}: ${error.message}`);
        }
      }
    } catch (error) {
      console.error("[WebpageMutationObserver] Failed to inject into existing frames:", error);
      throw error;
    }
  }

  /**
   * Set up polling for createElement events
   * @private
   */
  _setupPollingForEvents() {
    // Clear any existing polling
    if (this._globalPollingIntervalId) {
      clearInterval(this._globalPollingIntervalId);
    }

    // Set up new polling interval
    this._globalPollingIntervalId = setInterval(async () => {
      try {
        if (!this.tabId) return; // Skip if we've been disposed

        // Retrieve any pending events
        const response = await chrome.debugger.sendCommand({ tabId: this.tabId }, "Runtime.evaluate", {
          expression: `
                (function() {
                  // Check if events array exists
                  if (!window._iframeCreateElementEvents || !Array.isArray(window._iframeCreateElementEvents)) {
                    window._iframeCreateElementEvents = [];
                    return { count: 0, events: [] };
                  }
                  
                  // Get current events
                  const events = window._iframeCreateElementEvents;
                  
                  // Clear the array
                  window._iframeCreateElementEvents = [];
                  
                  return { 
                    count: events.length,
                    events: events
                  };
                })();
              `,
          returnByValue: true,
        });

        // Check if we have any events to process
        if (response && response.result && response.result.value) {
          const { count, events } = response.result.value;

          if (count > 0) {
            this._log(`Retrieved ${count} createElement events`);

            // Process each event
            for (const eventData of events) {
              this._processCreateElementEvent(eventData);
            }
          }
        }
      } catch (error) {
        console.error("[WebpageMutationObserver] Error polling for createElement events:", error);
      }
    }, 100); // Poll every 100ms for better responsiveness

    this._log("Event polling set up");
  }

  /**
   * Process a createElement event from an iframe
   * @param {Object} eventData - The event data
   * @private
   */
  _processCreateElementEvent(eventData) {
    try {
      this._log(`Processing createElement event: ${JSON.stringify(eventData)}`);

      // Try to find the iframe this event came from based on URL
      let sourceIframeNodeId = null;
      let bestMatch = { nodeId: null, matchScore: 0 };

      // Find the best matching iframe based on URL
      for (const [nodeId, iframe] of this.iframes.entries()) {
        if (iframe.url && eventData.frameLocation) {
          // Exact match
          if (iframe.url === eventData.frameLocation) {
            bestMatch = { nodeId, matchScore: 100 };
            break;
          }

          // Partial match (URL starts with iframe src)
          if (eventData.frameLocation.startsWith(iframe.url)) {
            const matchScore = iframe.url.length;
            if (matchScore > bestMatch.matchScore) {
              bestMatch = { nodeId, matchScore };
            }
          }

          // Match for about:blank
          if (iframe.url === "about:blank" && eventData.frameLocation === "about:blank") {
            bestMatch = { nodeId, matchScore: 50 };
          }
        }
      }

      sourceIframeNodeId = bestMatch.nodeId;

      // If we couldn't find a match but have iframes, use the first one
      if (!sourceIframeNodeId && this.iframes.size > 0) {
        sourceIframeNodeId = [...this.iframes.keys()][0];
        this._log(`No iframe match found for ${eventData.frameLocation}, using first iframe as fallback`);
      }

      // Create the mutation event
      const mutationEvent = {
        type: "iframe-create-element",
        iframeNodeId: sourceIframeNodeId,
        tagName: eventData.tagName,
        frameLocation: eventData.frameLocation,
        timestamp: eventData.timestamp,
      };

      this._log(`Notifying listeners about createElement: ${JSON.stringify(mutationEvent)}`);

      // Notify listeners
      this._notifyListeners(mutationEvent);
    } catch (error) {
      console.error("[WebpageMutationObserver] Error processing createElement event:", error);
    }
  }

  // ------------------------------------------------------------------------
  // PRIVATE METHODS - EVENT HANDLING
  // ------------------------------------------------------------------------

  /**
   * Handle debugger events from Chrome's debugging protocol
   * @param {Object} debuggeeId - The debuggee ID
   * @param {string} method - The event method
   * @param {Object} params - The event parameters
   * @private
   */
  async _handleDebuggerEvent(debuggeeId, method, params) {
    // Only process events for our tab
    if (debuggeeId.tabId !== this.tabId) return;

    switch (method) {
      case "DOM.childNodeInserted":
        await this._handleNodeInserted(params);
        break;

      case "DOM.childNodeRemoved":
        await this._handleNodeRemoved(params);
        break;

      case "DOM.attributeModified":
        await this._handleAttributeModified(params);
        break;

      case "DOM.documentUpdated":
        this._log("DOM document updated, rescanning iframes");
        // Re-scan the document when it's updated
        await this._findAndMonitorExistingIframes();
        break;
    }
  }

  /**
   * Handle node insertion events
   * @param {Object} params - The event parameters
   * @private
   */
  async _handleNodeInserted(params) {
    const { node, parentNodeId } = params;

    // Check if the inserted node is an iframe
    if (node.nodeName && node.nodeName.toLowerCase() === "iframe") {
      this._trackIframeNode(node);

      // Check if the iframe has a src attribute
      let src = null;
      if (node.attributes) {
        for (let i = 0; i < node.attributes.length; i += 2) {
          if (node.attributes[i] === "src") {
            src = node.attributes[i + 1];
            break;
          }
        }
      }

      // Notify listeners about the new iframe
      this._notifyListeners({
        type: "iframe-added",
        nodeId: node.nodeId,
        parentId: parentNodeId,
        src: src,
      });
    }

    // Check if insertion happened inside an iframe content document
    const iframeNodeId = this.contentDocumentMap.get(parentNodeId);
    if (iframeNodeId) {
      this._notifyListeners({
        type: "iframe-content-changed",
        action: "node-added",
        iframeNodeId: iframeNodeId,
        parentNodeId: parentNodeId,
        nodeId: node.nodeId,
        node: node,
      });
    }
  }

  /**
   * Handle node removal events
   * @param {Object} params - The event parameters
   * @private
   */
  async _handleNodeRemoved(params) {
    const { nodeId, parentNodeId } = params;

    // Check if the removed node was an iframe
    if (this.iframes.has(nodeId)) {
      const iframe = this.iframes.get(nodeId);

      // Remove the contentDocument mapping
      if (iframe.contentDocumentId) {
        this.contentDocumentMap.delete(iframe.contentDocumentId);
      }

      this.iframes.delete(nodeId);

      this._notifyListeners({
        type: "iframe-removed",
        nodeId: nodeId,
        parentId: parentNodeId,
      });
    }

    // Check if removal happened inside an iframe content document
    const iframeNodeId = this.contentDocumentMap.get(parentNodeId);
    if (iframeNodeId) {
      this._notifyListeners({
        type: "iframe-content-changed",
        action: "node-removed",
        iframeNodeId: iframeNodeId,
        parentNodeId: parentNodeId,
        nodeId: nodeId,
      });
    }
  }

  /**
   * Handle attribute modification events
   * @param {Object} params - The event parameters
   * @private
   */
  async _handleAttributeModified(params) {
    const { nodeId, name, value } = params;

    // Check if an iframe's src attribute changed
    if (this.iframes.has(nodeId) && name === "src") {
      // Update the iframe's src and URL
      const iframe = this.iframes.get(nodeId);
      iframe.src = value;
      iframe.url = value || "about:blank";

      this._notifyListeners({
        type: "iframe-src-changed",
        nodeId: nodeId,
        newSrc: value,
      });
    }
  }

  /**
   * Notify all listeners about a mutation
   * @param {Object} mutationInfo - Information about the mutation
   * @private
   */
  _notifyListeners(mutationInfo) {
    this._log(`Notifying ${this.listeners.size} listeners about mutation: ${mutationInfo.type}`);

    for (const listener of this.listeners) {
      try {
        listener(mutationInfo);
      } catch (error) {
        console.error("[WebpageMutationObserver] Error in mutation listener:", error);
      }
    }
  }
}

export default WebpageMutationObserver;
