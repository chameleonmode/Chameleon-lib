/**
 * WebpageMutationObserver
 *
 * A module for monitoring element creation in web pages, including:
 * - Main content page
 * - Existing iframes
 * - New iframes as they're created
 *
 * For use with Chrome Extensions Manifest V3 background service workers.
 */
class WebpageMutationObserver {
  constructor() {
    this.tabId = null;
    this.initialized = false;
    this._events = {};
    this._messageListener = null;

    // Define injectable scripts as functions
    this._scripts = {
      // Script to monitor element creation
      monitorElementCreation: this._createMonitorScript(),
      // Content script function for relaying events
      contentScriptRelay: this._createContentRelayScript(),
    };
  }

  /**
   * Creates the monitoring script as a function
   * @private
   * @returns {Function} The monitoring script function
   */
  _createMonitorScript() {
    return function monitorElementCreation() {
      try {
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
                data[i + 1] = Math.max(
                  0,
                  Math.min(255, data[i + 1] + (Math.random() * 2 - 1) * noiseLevel)
                );
                data[i + 2] = Math.max(
                  0,
                  Math.min(255, data[i + 2] + (Math.random() * 2 - 1) * noiseLevel)
                );
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
        console.log("[WebpageMutationObserver] Injecting createElement monitor");

        // Only patch once per document
        if (!Document.prototype._createElementMonitored) {
          // Store original method
          const originalCreateElement = Document.prototype.createElement;
          Document.prototype._createElementMonitored = true;

          // Override createElement
          Document.prototype.createElement = function (tagName, options) {
            // Call original method
            const element = originalCreateElement.call(this, tagName, options);
            console.log(`[WebpageMutationObserver] createElement called for <${tagName}>`);

            // Notify about this element creation
            try {
              // Check if this is an iframe or main page
              if (window !== window.top) {
                // In iframe, send to parent
                window.parent.postMessage(
                  {
                    source: "webpage-mutation-observer",
                    type: "iframe-create-element",
                    frameLocation: window.location.href,
                    tagName: tagName,
                    timestamp: Date.now(),
                  },
                  "*"
                );
              } else {
                // In main page, use custom event that can be captured by content script
                const event = new CustomEvent("main-page-create-element", {
                  detail: {
                    source: "webpage-mutation-observer",
                    type: "main-page-create-element",
                    frameLocation: window.location.href,
                    tagName: tagName,
                    timestamp: Date.now(),
                  },
                });
                document.dispatchEvent(event);
              }
            } catch (e) {
              console.error("[WebpageMutationObserver] Error sending createElement event:", e);
            }

            return element;
          };

          // Set up listener for iframe messages (to relay them to content script)
          if (window === window.top) {
            window.addEventListener("message", function (event) {
              if (
                event.data &&
                event.data.source === "webpage-mutation-observer" &&
                event.data.type === "iframe-create-element"
              ) {
                const relayEvent = new CustomEvent("iframe-relay-event", {
                  detail: event.data,
                });
                document.dispatchEvent(relayEvent);
              }
            });
          }

          console.log("[WebpageMutationObserver] createElement monitoring injected");
        }
      } catch (e) {
        console.error("[WebpageMutationObserver] Error in script:", e);
      }
      return true;
    };
  }

  /**
   * Creates the content script relay function
   * @private
   * @returns {Function} The content script relay function
   */
  _createContentRelayScript() {
    return function contentScriptRelay() {
      console.log("[WebpageMutationObserver] Content script injected");

      // Listen for main page element creation events
      document.addEventListener("main-page-create-element", (event) => {
        chrome.runtime.sendMessage({
          source: "webpage-mutation-observer",
          action: "element-created",
          data: event.detail,
        });
      });

      // Listen for iframe element creation events relayed by the main page
      document.addEventListener("iframe-relay-event", (event) => {
        chrome.runtime.sendMessage({
          source: "webpage-mutation-observer",
          action: "element-created",
          data: event.detail,
        });
      });

      console.log("[WebpageMutationObserver] Content script event listeners set up");
    };
  }

  /**
   * Initialize the observer for a specific tab
   * @param {number} tabId - The ID of the tab to observe
   * @returns {Promise<boolean>} - Whether initialization was successful
   */
  async initialize(tabId) {
    if (!tabId) {
      console.error("[WebpageMutationObserver] No tab ID provided");
      return false;
    }

    this.tabId = tabId;

    try {
      // Make sure the required domains are enabled
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "DOM.enable");
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Runtime.enable");
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.enable");

      // Set up script injection for new documents/frames
      await this._setupNewDocumentScriptInjection();

      // Inject script into all existing frames
      await this._injectIntoExistingFrames();

      // Set up messaging between content scripts and background
      this._setupMessageListener();

      // Inject content script to relay events from the page to background
      await this._injectContentScript();

      this.initialized = true;
      this._log("Initialization complete");
      return true;
    } catch (error) {
      console.error("[WebpageMutationObserver] Initialization failed:", error);
      return false;
    }
  }

  /**
   * Set up script injection for new documents that will be created
   * @private
   */
  async _setupNewDocumentScriptInjection() {
    try {
      // Convert the function to an IIFE string
      const scriptFunction = this._scripts.monitorElementCreation;
      const scriptSource = `(${scriptFunction.toString()})();`;

      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.addScriptToEvaluateOnNewDocument", {
        source: scriptSource,
      });

      this._log("New document script injection set up");
    } catch (error) {
      console.error("[WebpageMutationObserver] Failed to set up script injection:", error);
      throw error;
    }
  }

  /**
   * Inject monitoring script into all existing frames including the main page
   * @private
   */
  async _injectIntoExistingFrames() {
    try {
      // Get the frame tree
      const { frameTree } = await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.getFrameTree");

      // Recursive function to process frames
      const processFrame = async (frame) => {
        // Inject into this frame
        await this._injectScriptIntoFrame(frame.id);

        // Process child frames if any
        if (frame.childFrames) {
          for (const childFrame of frame.childFrames) {
            await processFrame(childFrame);
          }
        }
      };

      // Start with the main frame
      await processFrame(frameTree.frame);

      this._log("Injected script into existing frames");
    } catch (error) {
      console.error("[WebpageMutationObserver] Failed to inject into existing frames:", error);
      throw error;
    }
  }

  /**
   * Inject script into a specific frame
   * @private
   * @param {string} frameId - The frame ID
   */
  async _injectScriptIntoFrame(frameId) {
    try {
      // Convert the function to an IIFE string
      const scriptFunction = this._scripts.monitorElementCreation;
      const scriptSource = `(${scriptFunction.toString()})();`;

      // Execute in the specific frame
      await chrome.debugger
        .sendCommand({ tabId: this.tabId }, "Page.createIsolatedWorld", {
          frameId: frameId,
          worldName: "WebpageMutationObserverWorld",
        })
        .then(async ({ executionContextId }) => {
          await chrome.debugger.sendCommand({ tabId: this.tabId }, "Runtime.evaluate", {
            expression: scriptSource,
            contextId: executionContextId,
            returnByValue: true,
          });
        });

      this._log(`Injected script into frame ${frameId}`);
    } catch (error) {
      console.error(`[WebpageMutationObserver] Failed to inject into frame ${frameId}:`, error);
      // Continue with other frames
    }
  }

  /**
   * Set up listener for messages from content script
   * @private
   */
  _setupMessageListener() {
    // Remove existing listener if there is one
    if (this._messageListener) {
      chrome.runtime.onMessage.removeListener(this._messageListener);
    }

    // Create and register new listener
    this._messageListener = (message, sender, sendResponse) => {
      if (message.source === "webpage-mutation-observer" && sender.tab && sender.tab.id === this.tabId) {
        // Emit an event for subscribers
        this._emitEvent("element-created", message.data);
        sendResponse({ status: "received" });
      }
    };

    chrome.runtime.onMessage.addListener(this._messageListener);
    this._log("Message listener set up");
  }

  /**
   * Inject content script to relay events from the page
   * @private
   */
  async _injectContentScript() {
    try {
      // Inject content script using chrome.scripting API
      await chrome.scripting.executeScript({
        target: { tabId: this.tabId },
        func: this._scripts.contentScriptRelay,
      });

      this._log("Content script injected");
    } catch (error) {
      console.error("[WebpageMutationObserver] Failed to inject content script:", error);
      throw error;
    }
  }

  /**
   * Subscribe to element creation events
   * @param {Function} callback - Function to be called when an element is created
   * @returns {Function} - Unsubscribe function
   */
  onElementCreated(callback) {
    const listener = (event) => {
      if (event.type === "element-created") {
        callback(event.data);
      }
    };

    this._addEventListener("element-created", listener);

    // Return an unsubscribe function
    return () => {
      this._removeEventListener("element-created", listener);
    };
  }

  /**
   * Add an event listener
   * @private
   * @param {string} type - Event type
   * @param {Function} listener - Event listener
   */
  _addEventListener(type, listener) {
    if (!this._events[type]) {
      this._events[type] = [];
    }

    this._events[type].push(listener);
  }

  /**
   * Remove an event listener
   * @private
   * @param {string} type - Event type
   * @param {Function} listener - Event listener
   */
  _removeEventListener(type, listener) {
    if (!this._events[type]) {
      return;
    }

    const index = this._events[type].indexOf(listener);
    if (index !== -1) {
      this._events[type].splice(index, 1);
    }
  }

  /**
   * Emit an event
   * @private
   * @param {string} type - Event type
   * @param {Object} data - Event data
   */
  _emitEvent(type, data) {
    if (!this._events[type]) {
      return;
    }

    const event = {
      type,
      data,
    };

    for (const listener of this._events[type]) {
      listener(event);
    }
  }

  /**
   * Clean up resources when observer is no longer needed
   */
  async cleanup() {
    if (this.tabId) {
      try {
        // Remove chrome.runtime.onMessage listener
        if (this._messageListener) {
          chrome.runtime.onMessage.removeListener(this._messageListener);
          this._messageListener = null;
        }

        // Clear all event listeners
        this._events = {};

        // Try to detach debugger
        await chrome.debugger.detach({ tabId: this.tabId });
        this._log("Observer cleaned up and debugger detached");
      } catch (error) {
        console.error("[WebpageMutationObserver] Error during cleanup:", error);
      }

      this.tabId = null;
      this.initialized = false;
    }
  }

  /**
   * Simple logging helper
   * @private
   * @param {string} message - Message to log
   */
  _log(message) {
    console.log(`[WebpageMutationObserver] ${message}`);
  }
}

// Export for use in background script
export default WebpageMutationObserver;
