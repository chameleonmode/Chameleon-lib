/**
 * WebpageMutations
 * -----------------
 * Enhanced Canvas Fingerprinting Protection
 * https://privacycheck.sec.lrz.de/active/fp_c/fp_canvas.html
 * https://www.browserleaks.com/canvas
 * https://browserleaks.com/webgl
 * https://privacycheck.sec.lrz.de/active/fp_wg/fp_webgl.html
 * https://gist.github.com/abrahamjuliot/7baf3be8c451d23f7a8693d7e28a35e2
 * https://privacycheck.sec.lrz.de/active/fp_ac/fp_audiocontext.html
 * https://privacycheck.sec.lrz.de/active/fp_gcr/fp_getclientrects.html#fpGetClientRects
 * https://browserleaks.com/rects
 *
 * A module for monitoring element creation in web pages, including:
 * - Main content page
 * - Existing iframes
 * - New iframes as they're created
 *
 * For use with Chrome Extensions Manifest V3 background service workers.
 */

import App from "../app.js";
import canvas from "../scripts/canvas.js";
import rects from "../scripts/rects.js";
import webgl from "../scripts/webgl.js";
import fonts from "../scripts/fonts.js";
import audio from "../scripts/audio.js";
import navigatorize from "../scripts/navigator.js";

class PageMutations {
  constructor(tabId) {
    this.tabId = tabId;
    this.scriptSource = "";

    // Define configurations for all scripts at once
    this.scriptConfigs = {
      // canvi: (() => {
      //   if (!App.config.canvas.pixels) {
      //     App.config.canvas.pixels = {
      //       r: Math.random(),
      //       g: Math.random(),
      //       b: Math.random(),
      //     };
      //   }
      //   if (!App.config.canvas.positions) {
      //     App.config.canvas.positions = {
      //       x: Math.random(),
      //       y: Math.random(),
      //     };
      //   }
      //   if (!App.config.canvas.rects) {
      //     App.config.canvas.rects = {
      //       x: Math.random(),
      //       y: Math.random(),
      //       width: Math.random(),
      //       height: Math.random(),
      //     };
      //   }
      //   const opts = {
      //     noise: App.config.canvas.random
      //       ? App.config.noises[Math.floor(Math.random() * App.config.noises.length)]
      //       : App.config.noise,
      //     positions: App.config.canvas.positions,
      //     rects: App.config.canvas.rects,
      //     pixels: App.config.canvas.pixels,
      //   };
      //   return {
      //     init: async () => {
      //       return App.config.canvas.enabled;
      //     },
      //     script: canvas,
      //     opts,
      //   };
      // })(),
      rects: (() => {
        return {
          script: rects,
          init: async () => {
            return App.config.rects.enabled;
          },
          opts: { 
            random: App.config.rects.random,
            noise: App.config.noise,
          },
        };
      })(),
      // webgl: (() => {
      //   return {
      //     script: webgl,
      //     init: async () => {
      //       return App.config.webgl.enabled;
      //     },
      //     opts: { random: true },
      //   };
      // })(),
      // fonts: (() => {
      //   return {
      //     script: fonts,
      //     init: async () => {
      //       return App.config.fonts.enabled;
      //     },
      //     opts: { random: true },
      //   };
      // })(),
      // audio: (() => {
      //   return {
      //     script: audio,
      //     init: async () => {
      //       return App.config.audio.enabled;
      //     },
      //     opts: { random: true },
      //   };
      // })(),
      // navi: (() => {
      //   const os = App.config.navi.os;
      //   const random = false; // For testing purposes

      //   const RULE_ID_START = 1000;
      //   const chromeVersionMatch = navigator.userAgent.match(/Chrome\/(\d+)/);
      //   const chromeMajorVersion = chromeVersionMatch ? parseInt(chromeVersionMatch[1], 10) : 134;

      //   // Create the configs object first
      //   const configs = {
      //     mac: {
      //       "User-Agent": `Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/${chromeMajorVersion}.0.0.0 Safari/537.36`,
      //       "sec-ch-ua-platform": '"macOS"',
      //       "sec-ch-ua-platform-version": '"15.3.1"',
      //       "sec-ch-ua-model": '""',
      //     },
      //     windows: {
      //       "User-Agent": `Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/${chromeMajorVersion}.0.0.0 Safari/537.36`,
      //       "sec-ch-ua-platform": '"Windows"',
      //       "sec-ch-ua-platform-version": '"10.0.22621"',
      //       "sec-ch-ua-model": '""',
      //     },
      //     linux: {
      //       "User-Agent": `Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/${chromeMajorVersion}.0.0.0 Safari/537.36`,
      //       "sec-ch-ua-platform": '"Linux"',
      //       "sec-ch-ua-platform-version": '"5.15.0"',
      //       "sec-ch-ua-model": '""',
      //     },
      //   };
      //   const config = !random
      //     ? configs[os]
      //     : Object.keys(configs)[Math.floor(Math.random() * Object.keys(configs).length)];

      //   // Return the complete configuration object
      //   return {
      //     init: async () => {
      //       // Remove only existing dynamic rules with IDs >= RULE_ID_START
      //       const rules = await chrome.declarativeNetRequest.getDynamicRules();
      //       await chrome.declarativeNetRequest.updateDynamicRules({
      //         removeRuleIds: rules.filter((rule) => rule.id >= RULE_ID_START).map((rule) => rule.id),
      //       });

      //       return App.config.navi.enabled && App.config.navi.os !== "default";
      //     },
      //     script: navigatorize,
      //     opts: {
      //       os,
      //       configs,
      //       RULE_ID_START,
      //       chromeMajorVersion,
      //       config,
      //     },
      //   };
      // })(),
    };
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

  /**
   * Generate the script source by executing each script and combining the results
   * @returns {Promise<void>}
   */
  async generateScriptSource() {
    const scriptPromises = Object.values(this.scriptConfigs).map(
      async ({ script, init, opts }) => {
        const enabled = await init();
        if (!enabled) return "";
        else return `(${(await script(opts)).toString()})(${JSON.stringify(opts)});`;
      }
    );

    // Wait for all promises to resolve, then join
    this.scriptSource = (await Promise.all(scriptPromises)).filter(script => script).join("");
  }

  /**
   * Set up script injection for new documents
   * @returns {Promise<void>}
   */
  async setupNewDocumentScriptInjection() {
    await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.addScriptToEvaluateOnNewDocument", {
      source: this.scriptSource,
    });

    await chrome.debugger.sendCommand({ tabId: this.tabId }, "Runtime.evaluate", {
      expression: this.scriptSource,
    });
  }

  /**
   * Inject the script into existing frames
   * @returns {Promise<void>}
   */
  async injectIntoExistingFrames() {
    // Get the frame tree
    const { frameTree } = await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.getFrameTree");

    // Recursive function to process frames
    const processFrame = async (frame) => {
      // Inject into this frame
      await chrome.debugger
        .sendCommand({ tabId: this.tabId }, "Page.createIsolatedWorld", {
          frameId: frame.id,
          worldName: `${Math.random().toString(36).substring(7)}`,
        })
        .then(async ({ executionContextId }) => {
          await chrome.debugger.sendCommand({ tabId: this.tabId }, "Page.addScriptToEvaluateOnNewDocument", {
            source: this.scriptSource,
            contextId: executionContextId,
            returnByValue: true,
          });
          await chrome.debugger.sendCommand({ tabId: this.tabId }, "Runtime.evaluate", {
            expression: this.scriptSource,
            contextId: executionContextId,
            returnByValue: true,
          });
        });

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
}

export default PageMutations;
