/**
 * ContentScriptRelay
 * 
 * A module for injecting content scripts that relay events from web pages to the extension background.
 * Works with Chrome Extensions Manifest V3 background service workers.
 */

class ContentScriptRelay {
  /**
   * Creates a new ContentScriptRelay instance
   * @param {number} tabId - The ID of the tab to inject into
   */
  constructor(tabId) {
    this.tabId = tabId;
  }
  /**
   * Create the content script relay function
   * @returns {Function} The content script relay function
   */
  createRelayScript() {
    return function contentScriptRelay() {
      console.log('[ContentScriptRelay] Content script injected');
      
      // Listen for main page element creation events
      document.addEventListener('main-page-create-element', (event) => {
        chrome.runtime.sendMessage({
          source: 'webpage-mutations',
          action: 'element-created',
          data: event.detail
        });
      });
      
      // Listen for iframe element creation events relayed by the main page
      document.addEventListener('iframe-relay-event', (event) => {
        chrome.runtime.sendMessage({
          source: 'webpage-mutations',
          action: 'element-created',
          data: event.detail
        });
      });
      
      // Report successful setup back to the content script
      console.log('[ContentScriptRelay] Content script event listeners set up');
    };
  }

  /**
   * Inject the relay script into the tab
   * @returns {Promise<void>}
   */
  async inject() {
    try {
      // Inject content script using chrome.scripting API
      await chrome.scripting.executeScript({
        target: { tabId: this.tabId },
        func: this.createRelayScript()
      });
    } catch (error) {
      throw error;
    }
  }

  /**
   * Create a message listener for events from the content script
   * @param {Function} callback - Function to call when a message is received
   * @returns {Function} - Unsubscribe function
   */
  createMessageListener(callback) {
    if (typeof callback !== 'function') {
      throw new Error("Callback must be a function");
    }

    const messageListener = (message, sender, sendResponse) => {
      if (message.source === 'webpage-mutation-observer' && 
          sender.tab && 
          sender.tab.id === this.tabId) {
        callback(message.data);
        sendResponse({ status: 'received' });
      }
    };

    chrome.runtime.onMessage.addListener(messageListener);

    // Return unsubscribe function
    return () => {
      chrome.runtime.onMessage.removeListener(messageListener);
    };
  }
}

export default ContentScriptRelay;