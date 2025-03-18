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

import elements from "../scripts/elements.js";
import canvas from "../scripts/canvas.js";

class PageMutations {
  constructor(tabId) {
    this.tabId = tabId;

    // Define injectable scripts as functions
    this.scriptSource = `(${elements().toString()})();` + `(${canvas().toString()})();`;
  }

  /**
   * Initialize the observer for a specific tab
   * @param {number} tabId - The ID of the tab to observe
   * @returns {Promise<boolean>} - Whether initialization was successful
   */
  async initialize() {
    // Set up script injection for new/current documents/frames
    await this.setupNewDocumentScriptInjection();

    // Inject script into all existing frames
    await this.injectIntoExistingFrames();
  }

  async setupNewDocumentScriptInjection() {
    await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.addScriptToEvaluateOnNewDocument", {
      source: this.scriptSource,
    });

    await chrome.debugger.sendCommand({ tabId: this.tabId }, "Runtime.evaluate", {
      expression: this.scriptSource,
    });
  }

  async injectIntoExistingFrames() {
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
    await chrome.debugger
      .sendCommand({ tabId: this.tabId }, "Page.createIsolatedWorld", {
        frameId: frameId,
        worldName: `${Math.random().toString(36).substring(7)}`,
      })
      .then(async ({ executionContextId }) => {
        await chrome.debugger.sendCommand({ tabId: this.tabId }, "Runtime.evaluate", {
          expression: this.scriptSource,
          contextId: executionContextId,
          returnByValue: true,
        });
      });
  }
}

export default PageMutations;
