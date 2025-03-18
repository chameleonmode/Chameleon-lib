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

import DOMContentLoadedManager from './DOMContentLoadedManager.js';
import ContentScriptRelay from './ContentScriptRelay.js';
import { logger } from './Logger.js';

class WebpageMutations {
  /**
   * Create a new WebpageMutations instance
   */
  constructor() {
    this.tabId = null;
    this.initialized = false;
    this._events = {};
    this._messageListenerUnsubscribe = null;
    this._domContentLoadedManager = null;
    this._domContentLoadedUnsubscribe = null;
    this._contentScriptRelay = null;
    
    // Set up module-specific logger
    this._logger = logger.createChild('WebpageMutations');
    
    // Define injectable scripts as functions
    this._scripts = {
      monitorElementCreation: this._createMonitorScript()
    };
  }

  /**
   * Initialize the observer for a specific tab
   * @param {number} tabId - The ID of the tab to observe
   * @returns {Promise<boolean>} - Whether initialization was successful
   */
  async initialize(tabId) {
    if (!tabId) {
      this._logger.error("No tab ID provided");
      return false;
    }
    
    this.tabId = tabId;
    
    try {
      // Enable required domains for Chrome Debugger API
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "DOM.enable");
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Runtime.enable");
      await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.enable");
      
      // Set up script injection for new documents/frames
      await this._setupNewDocumentScriptInjection();
      
      // Initialize the DOMContentLoaded manager
      this._domContentLoadedManager = new DOMContentLoadedManager(this.tabId);
      await this._domContentLoadedManager.initialize();
      
      // Initialize the content script relay
      this._contentScriptRelay = new ContentScriptRelay(this.tabId);
      
      // Set up message listener for events from content script
      this._setupMessageListener();
      
      // Setup content loaded handlers
      this._setupContentLoadedHandlers();
      
      this._logger.info("Initialization complete - waiting for DOMContentLoaded events");
      this.initialized = true;
      return true;
    } catch (error) {
      this._logger.error("Initialization failed:", error);
      return false;
    }
  }

  /**
   * Creates the monitoring script as a function
   * @private
   * @returns {Function} The monitoring script function
   */
  _createMonitorScript() {
    return function monitorElementCreation() {
      try {
        console.log('[WebpageMutations] Injecting createElement monitor');
        
        // Only patch once per document
        if (!Document.prototype._createElementMonitored) {
          // Store original method
          const originalCreateElement = Document.prototype.createElement;
          Document.prototype._createElementMonitored = true;
          
          // Override createElement
          Document.prototype.createElement = function(tagName, options) {
            console.log('[WebpageMutations] createElement:', tagName);
            // Call original method
            const element = originalCreateElement.call(this, tagName, options);
            
            // Notify about this element creation
            try {
              // Check if this is an iframe or main page
              if (window !== window.top) {
                // In iframe, send to parent
                window.parent.postMessage({
                  source: 'webpage-mutations',
                  type: 'iframe-create-element',
                  frameLocation: window.location.href,
                  tagName: tagName,
                  timestamp: Date.now()
                }, '*');
              } else {
                // In main page, use custom event that can be captured by content script
                const event = new CustomEvent('main-page-create-element', {
                  detail: {
                    source: 'webpage-mutations',
                    type: 'main-page-create-element',
                    frameLocation: window.location.href,
                    tagName: tagName,
                    timestamp: Date.now()
                  }
                });
                document.dispatchEvent(event);
              }
            } catch(e) {
              console.error('[WebpageMutations] Error sending createElement event:', e);
            }
            
            return element;
          };
          
          // Set up listener for iframe messages (to relay them to content script)
          if (window === window.top) {
            window.addEventListener('message', function(event) {
              if (event.data && 
                  event.data.source === 'webpage-mutations' && 
                  event.data.type === 'iframe-create-element') {
                const relayEvent = new CustomEvent('iframe-relay-event', {
                  detail: event.data
                });
                document.dispatchEvent(relayEvent);
              }
            });
          }
          
          console.log('[WebpageMutations] createElement monitoring injected');
        }
      } catch(e) {
        console.error('[WebpageMutations] Error in script:', e);
      }
      return true;
    };
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
      
      await chrome.debugger.sendCommand(
        { tabId: this.tabId },
        "Page.addScriptToEvaluateOnNewDocument",
        {
          source: scriptSource
        }
      );

      this._logger.debug("New document script injection set up");
    } catch (error) {
      this._logger.error("Failed to set up script injection:", error);
      throw error;
    }
  }

  /**
   * Set up handlers for DOM content loaded events
   * @private 
   */
  _setupContentLoadedHandlers() {
    // Unsubscribe from any existing handlers
    if (this._domContentLoadedUnsubscribe) {
      this._domContentLoadedUnsubscribe();
    }
    
    // Register handler for all frames
    this._domContentLoadedUnsubscribe = this._domContentLoadedManager.onDOMContentLoaded(
      async (frameId) => {
        this._logger.debug(`Handling DOMContentLoaded for frame: ${frameId}`);
        
        // Inject monitoring script into this frame
        await this._injectScriptIntoFrame(frameId);
        
        // If this is the main frame, also inject the content script
        try {
          const { frameTree } = await chrome.debugger.sendCommand(
            { tabId: this.tabId },
            "Page.getFrameTree"
          );
          
          if (frameId === frameTree.frame.id) {
            this._logger.debug("Main frame detected, injecting content script");
            await this._contentScriptRelay.inject();
          }
        } catch (error) {
          this._logger.error(`Error checking if frame ${frameId} is main:`, error);
        }
      }
    );
    
    this._logger.debug("Content loaded handlers set up");
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
      
      await chrome.debugger.sendCommand(
        { tabId: this.tabId },
        "Page.createIsolatedWorld",
        {
          frameId: frameId,
          worldName: "WebpageMutationsWorld"
        }
      ).then(async ({ executionContextId }) => {
        await chrome.debugger.sendCommand(
          { tabId: this.tabId },
          "Runtime.evaluate",
          {
            expression: scriptSource,
            contextId: executionContextId,
            returnByValue: true
          }
        );
      });
      
      this._logger.debug(`Injected script into frame ${frameId}`);
    } catch (error) {
      this._logger.error(`Failed to inject into frame ${frameId}:`, error);
      // Continue with other frames
    }
  }

  /**
   * Set up message listener for events from content script
   * @private
   */
  _setupMessageListener() {
    // Remove existing listener if there is one
    if (this._messageListenerUnsubscribe) {
      this._messageListenerUnsubscribe();
    }
    
    // Create and register new listener
    this._messageListenerUnsubscribe = this._contentScriptRelay.createMessageListener((data) => {
      // Emit an event for subscribers
      this._emitEvent('element-created', data);
    });
    
    this._logger.debug("Message listener set up");
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
    this._logger.debug(`Added event listener for ${type}`);
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
      this._logger.debug(`Removed event listener for ${type}`);
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
      data
    };
    
    for (const listener of this._events[type]) {
      listener(event);
    }
  }

  /**
   * Subscribe to element creation events
   * @param {Function} callback - Function to be called when an element is created
   * @returns {Function} - Unsubscribe function
   */
  onElementCreated(callback) {
    const listener = (event) => {
      if (event.type === 'element-created') {
        callback(event.data);
      }
    };
    
    this._addEventListener('element-created', listener);
    this._logger.debug("Subscribed to element-created events");
    
    // Return an unsubscribe function
    return () => {
      this._removeEventListener('element-created', listener);
      this._logger.debug("Unsubscribed from element-created events");
    };
  }

  /**
   * Clean up resources when observer is no longer needed
   */
  async cleanup() {
    if (this.tabId) {
      try {
        // Remove message listener
        if (this._messageListenerUnsubscribe) {
          this._messageListenerUnsubscribe();
          this._messageListenerUnsubscribe = null;
        }
        
        // Unsubscribe from DOMContentLoaded events
        if (this._domContentLoadedUnsubscribe) {
          this._domContentLoadedUnsubscribe();
          this._domContentLoadedUnsubscribe = null;
        }
        
        // Clean up the DOMContentLoadedManager
        if (this._domContentLoadedManager) {
          await this._domContentLoadedManager.cleanup();
          this._domContentLoadedManager = null;
        }
        
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