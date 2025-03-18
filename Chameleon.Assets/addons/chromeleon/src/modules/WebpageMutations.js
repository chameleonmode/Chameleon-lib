/**
 * WebpageMutations
 *
 * A module for monitoring element creation in web pages, including:
 * - Main content page
 * - Existing iframes
 * - New iframes as they're created
 *
 * For use with Chrome Extensions Manifest V3 background service workers.
 */

class WebpageMutations {
  constructor(tabId) {
    this.tabId = tabId;

    // Define injectable scripts as functions
    this._scripts = {
      monitorElementCreation: this._createMonitorScript(),
    };
  }

  /**
   * Initialize the observer for a specific tab
   * @param {number} tabId - The ID of the tab to observe
   * @returns {Promise<boolean>} - Whether initialization was successful
   */
  async initialize() {
    // Set up script injection for new documents/frames
    await this._setupNewDocumentScriptInjection();

    // Inject script into all existing frames
    await this._injectIntoExistingFrames();
  }

  /**
   * Creates the monitoring script as a function
   * @private
   * @returns {Function} The monitoring script function
   */
  _createMonitorScript() {
    return function monitorElementCreation() {
      //(() => {
      // Store original method
      const originalCreateElement = Document.prototype.createElement;

      // Override createElement
      Document.prototype.createElement = function (tagName, options) {
        console.log("[WebpageMutations] createElement:", tagName);
        // Call original method
        const element = originalCreateElement.call(this, tagName, options);

        return element;
      };

      console.log("[WebpageMutations] createElement monitoring injected");
      //})();
      return true;
    };
  }

  /**
   * Set up script injection for new documents that will be created
   * @private
   */
  async _setupNewDocumentScriptInjection() {
    // Convert the function to an IIFE string
    const scriptFunction = this._scripts.monitorElementCreation;
    const scriptSource = `(${scriptFunction.toString()})();`;

    await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.addScriptToEvaluateOnNewDocument", {
      source: scriptSource,
    });

    await chrome.debugger.sendCommand({ tabId: this.tabId }, "Runtime.evaluate", {
      expression: scriptSource,
    });
  }

  /**
   * Inject monitoring script into all existing frames including the main page
   * @private
   */
  async _injectIntoExistingFrames() {
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
  }

  /**
   * Inject script into a specific frame
   * @private
   * @param {string} frameId - The frame ID
   */
  async _injectScriptIntoFrame(frameId) {
    // Convert the function to an IIFE string
    const scriptFunction = this._scripts.monitorElementCreation;
    const scriptSource = `(${scriptFunction.toString()})();`;

    await chrome.debugger
      .sendCommand({ tabId: this.tabId }, "Page.createIsolatedWorld", {
        frameId: frameId,
        worldName: `${Math.random().toString(36).substring(7)}`,
      })
      .then(async ({ executionContextId }) => {
        await chrome.debugger.sendCommand({ tabId: this.tabId }, "Runtime.evaluate", {
          expression: scriptSource,
          contextId: executionContextId,
          returnByValue: true,
        });
      });
  }

  /**
   * Clean up resources when observer is no longer needed
   */
  async cleanup() {
    if (this.tabId) {
      try {
        // Clear all event listeners
        this._events = {};

        // Try to detach debugger
        await chrome.debugger.detach({ tabId: this.tabId });
        this._logger.info("Observer cleaned up and debugger detached");
      } catch (error) {
        this._logger.error("Error during cleanup:", error);
      }

      this.tabId = null;
      this.initialized = false;
    }
  }
}

export default WebpageMutations;
