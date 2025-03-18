/**
 * DOMContentLoadedManager
 * 
 * A module for handling DOMContentLoaded events and executing actions at the right moment
 * in the page lifecycle. Works with Chrome Extensions Manifest V3 background service workers.
 */

import { logger } from './Logger.js';

class DOMContentLoadedManager {
  /**
   * Creates a new DOMContentLoadedManager
   * @param {number} tabId - The ID of the tab to observe
   */
  constructor(tabId) {
    this.tabId = tabId;
    this._eventHandler = null;
    this._frameCallbacks = new Map();
    this._initialized = false;
    this._processedFrames = new Set(); // Track processed frames to avoid duplicates
    this._processingFrames = new Set(); // Track frames currently being processed
    
    // Create a module-specific logger
    this._logger = logger.createChild('DOMContentLoadedManager');
  }

  /**
   * Initialize the manager
   * @returns {Promise<boolean>} - Whether initialization was successful
   */
  async initialize() {
    if (this._initialized) {
      return true;
    }

    try {
      // Enable required domains
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.enable");
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Debugger.enable");
      
      // Enable lifecycle events - critical for detecting DOMContentLoaded
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.setLifecycleEventsEnabled", {
        enabled: true
      });

      // Set up the event handler
      this._setupEventHandler();
      
      this._initialized = true;
      this._logger.info("Initialization complete");
      return true;
    } catch (error) {
      this._logger.error("Initialization failed:", error);
      return false;
    }
  }

  /**
   * Set up the Chrome debugger event handler
   * @private
   */
  _setupEventHandler() {
    // Remove any existing handler
    if (this._eventHandler) {
      chrome.debugger.onEvent.removeListener(this._eventHandler);
    }

    // Create the new handler
    this._eventHandler = async (debuggeeId, message, params) => {
      if (debuggeeId.tabId !== this.tabId) return;

      if (message === "Page.lifecycleEvent" && params.name === "DOMContentLoaded") {
        const frameId = params.frameId;
        this._logger.debug(`DOMContentLoaded event fired for frame: ${frameId}`);
        
        // If we've already processed this frame or are currently processing it, skip
        if (this._processedFrames.has(frameId) || this._processingFrames.has(frameId)) {
          this._logger.debug(`Frame ${frameId} already processed or processing, skipping`);
          return;
        }
        
        this._processingFrames.add(frameId);
        await this._handleDOMContentLoaded(frameId);
        this._processedFrames.add(frameId);
        this._processingFrames.delete(frameId);
      }
    };

    // Register the handler
    chrome.debugger.onEvent.addListener(this._eventHandler);
    this._logger.debug("Event handler registered");
  }

  /**
   * Handle a DOMContentLoaded event for a specific frame
   * @private
   * @param {string} frameId - ID of the frame that triggered DOMContentLoaded
   */
  async _handleDOMContentLoaded(frameId) {
    const callbacks = this._frameCallbacks.get('*') || [];
    const frameSpecificCallbacks = this._frameCallbacks.get(frameId) || [];
    const allCallbacks = [...callbacks, ...frameSpecificCallbacks];
    
    if (allCallbacks.length === 0) {
      this._logger.debug(`No callbacks registered for frame ${frameId}`);
      return;
    }

    let debuggerPaused = false;
    
    try {
      // Pause script execution
      try {
        await chrome.debugger.sendCommand({ tabId: this.tabId }, "Debugger.pause");
        debuggerPaused = true;
        this._logger.debug(`Paused execution for frame ${frameId}`);
      } catch (pauseError) {
        this._logger.warn(`Error pausing execution for frame ${frameId}:`, pauseError);
        // Continue with execution even if pausing fails
      }

      // Execute all registered callbacks
      for (const callback of allCallbacks) {
        try {
          await callback(frameId);
        } catch (callbackError) {
          this._logger.error(`Callback error for frame ${frameId}:`, callbackError);
        }
      }
    } catch (error) {
      this._logger.error(`Error handling DOMContentLoaded for frame ${frameId}:`, error);
    } finally {
      // Only try to resume if we successfully paused
      if (debuggerPaused) {
        try {
          await chrome.debugger.sendCommand({ tabId: this.tabId }, "Debugger.resume");
          this._logger.debug(`Resumed execution for frame ${frameId}`);
        } catch (resumeError) {
          // Don't throw error, just log it as a warning
          this._logger.warn(`Could not resume execution for frame ${frameId} - it may have already resumed`, resumeError);
        }
      }
    }
  }

  /**
   * Register a callback to be executed when DOMContentLoaded fires for a specific frame
   * @param {Function} callback - Async function to execute when DOMContentLoaded fires
   * @param {string} [frameId='*'] - Frame ID to target, or '*' for all frames
   * @returns {Function} - Function to unregister this callback
   */
  onDOMContentLoaded(callback, frameId = '*') {
    if (typeof callback !== 'function') {
      throw new Error('Callback must be a function');
    }

    if (!this._frameCallbacks.has(frameId)) {
      this._frameCallbacks.set(frameId, []);
    }

    this._frameCallbacks.get(frameId).push(callback);
    this._logger.debug(`Registered callback for frame ${frameId}`);

    // Return unsubscribe function
    return () => {
      const callbacks = this._frameCallbacks.get(frameId) || [];
      const index = callbacks.indexOf(callback);
      if (index !== -1) {
        callbacks.splice(index, 1);
        this._logger.debug(`Unregistered callback for frame ${frameId}`);
      }
    };
  }

  /**
   * Check if a frame has fired DOMContentLoaded already
   * @param {string} frameId - Frame ID to check
   * @returns {Promise<boolean>} - Whether DOMContentLoaded has fired
   */
  async hasFrameLoaded(frameId) {
    try {
      const result = await chrome.debugger.sendCommand(
        { tabId: this.tabId },
        "Runtime.evaluate",
        {
          expression: "document.readyState",
          frameId: frameId
        }
      );

      return result && result.result && result.result.value === "complete";
    } catch (error) {
      this._logger.error(`Error checking frame loaded state: ${frameId}`, error);
      return false;
    }
  }

  /**
   * Clean up resources used by the manager
   */
  async cleanup() {
    if (this._eventHandler) {
      chrome.debugger.onEvent.removeListener(this._eventHandler);
      this._eventHandler = null;
    }

    this._frameCallbacks.clear();
    this._processedFrames.clear();
    this._processingFrames.clear();

    try {
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.setLifecycleEventsEnabled", {
        enabled: false
      });
    } catch (error) {
      // Ignore errors if tab already closed
    }

    this._logger.info("Cleanup complete");
    this._initialized = false;
  }
}

export default DOMContentLoadedManager;