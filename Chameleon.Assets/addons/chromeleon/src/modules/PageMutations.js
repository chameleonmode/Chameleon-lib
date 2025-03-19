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

import canvas from "../scripts/canvas.js";
import rects from "../scripts/rects.js";
import webgl from "../scripts/webgl.js";
import fonts from "../scripts/fonts.js";
import audio from "../scripts/audio.js";
import navigatorize from "../scripts/navigator.js";

const chromeVersionMatch = navigator.userAgent.match(/Chrome\/(\d+)/);
const chromeMajorVersion = chromeVersionMatch ? parseInt(chromeVersionMatch[1], 10) : 134;
const defaults = {
  level: "medium",
  chromeMajorVersion,
};

class PageMutations {
  constructor(tabId) {
    this.tabId = tabId;
    this.scriptSource = "";

    // Define configurations for all scripts at once
    this.scriptConfigs = {
      canvas: {
        script: canvas,
        opts: { ...defaults, random: false },
      },
      rects: {
        script: rects,
        opts: { ...defaults, random: false },
      },
      webgl: {
        script: webgl,
        opts: { ...defaults, random: true },
      },
      fonts: {
        script: fonts,
        opts: { ...defaults, random: true },
      },
      audio: {
        script: audio,
        opts: { ...defaults, random: true },
      },
      navigatorize: {
        script: navigatorize,
        opts: {
          ...defaults,
          random: true,
          os: "default",
          configs: {
            mac: {
              "User-Agent": `Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/${chromeMajorVersion}.0.0.0 Safari/537.36`,
              "sec-ch-ua-platform": '"macOS"',
              "sec-ch-ua-platform-version": '"15.3.1"',
              "sec-ch-ua-model": '""',
            },
            windows: {
              "User-Agent": `Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/${chromeMajorVersion}.0.0.0 Safari/537.36`,
              "sec-ch-ua-platform": '"Windows"',
              "sec-ch-ua-platform-version": '"10.0.22621"',
              "sec-ch-ua-model": '""',
            },
            linux: {
              "User-Agent": `Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/${chromeMajorVersion}.0.0.0 Safari/537.36`,
              "sec-ch-ua-platform": '"Linux"',
              "sec-ch-ua-platform-version": '"5.15.0"',
              "sec-ch-ua-model": '""',
            },
          },
        },
      },
    };
  }

  // Instead of using map and join directly, use Promise.all
  async generateScriptSource() {
    const scriptPromises = Object.values(this.scriptConfigs).map(async ({ script, opts }) => {
      return `(${(await script(opts)).toString()})(${JSON.stringify(opts)});`;
    });

    // Wait for all promises to resolve, then join
    this.scriptSource = (await Promise.all(scriptPromises)).join("\n");
  }

  /**
   * Initialize the observer for a specific tab
   * @param {number} tabId - The ID of the tab to observe
   * @returns {Promise<boolean>} - Whether initialization was successful
   */
  async initialize() {
    await this.generateScriptSource();

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
