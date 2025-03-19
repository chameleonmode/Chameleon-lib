/**
 * WebpageMutations
 * -----------------
 * Enhanced Canvas Fingerprinting Protection
 * https://privacycheck.sec.lrz.de/active/fp_c/fp_canvas.html
 * https://www.browserleaks.com/canvas
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

const defaults = {
  level: "medium",
};

class PageMutations {
  constructor(tabId) {
    this.tabId = tabId;
    this.scriptSource = "";

    // Define configurations for all scripts at once
    this.scriptConfigs = {
      canvi: (() => {
        const { enabled, random } = App.config.canvas;
        const amplitutdes = {
          micro: 0.1, // Very subtle changes
          mini: 0.4, // Minor changes
          low: 0.8, // Low noise level
          medium: 0.14, // Standard protection
          bold: 0.18, // Stronger noise
          high: 0.24, // High protection
          ultra: 0.25, // Very high protection
          super: 0.34, // Super high protection
          max: 0.38, // Maximum protection
        };
        const noiseLevel = amplitutdes[App.config.noise] || amplitutdes.medium;
        if (!App.config.canvas.pixels || random) {
          App.config.canvas.pixels = {
            r: Math.random(),
            g: Math.random(),
            b: Math.random(),
          };
        }
        if (!App.config.canvas.positions || random) {
          App.config.canvas.positions = {
            x: Math.random(),
            y: Math.random(),
          };
        }
        if (!App.config.canvas.rects || random) {
          App.config.canvas.rects = {
            x: Math.random(),
            y: Math.random(),
            width: Math.random(),
            height: Math.random(),
          };
        }
        return {
          enabled,
          init: async () => {},
          script: canvas,
          opts: {
            rects: App.config.canvas.rects,
            pixels: App.config.canvas.pixels,
            positions: App.config.canvas.positions,
          },
        };
      })(),
      // rects: (() => {
      //   return {
      //     script: rects,
      //     init: async () => {},
      //     enabled: true,
      //     opts: { ...defaults, random: false },
      //   };
      // })(),
      // webgl: (() => {
      //   return {
      //     script: webgl,
      //     init: async () => {},
      //     enabled: true,
      //     opts: { ...defaults, random: true },
      //   };
      // })(),
      // fonts: (() => {
      //   return {
      //     script: fonts,
      //     init: async () => {},
      //     enabled: true,
      //     opts: { ...defaults, random: true },
      //   };
      // })(),
      // audio: (() => {
      //   return {
      //     script: audio,
      //     init: async () => {},
      //     enabled: true,
      //     opts: { ...defaults, random: true },
      //   };
      // })(),
      // navi: (() => {
      //   //const { naviOS, naviRandomize: random } = App.config;
      //   const naviOS = "windows"; // For testing purposes
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
      //   //const config = !random ? configs[naviOS] : Object.keys(configs)[Math.floor(Math.random() * Object.keys(configs).length)];
      //   // Return the complete configuration object
      //   return {
      //     init: async () => {
      //       // Remove only existing dynamic rules with IDs >= RULE_ID_START
      //       const rules = await chrome.declarativeNetRequest.getDynamicRules();
      //       await chrome.declarativeNetRequest.updateDynamicRules({
      //         removeRuleIds: rules.filter((rule) => rule.id >= RULE_ID_START).map((rule) => rule.id),
      //       });
      //     },
      //     script: navigatorize,
      //     enabled: true, //random || naviOS !== "default",
      //     opts: {
      //       os: naviOS,
      //       configs,
      //       RULE_ID_START,
      //       chromeMajorVersion,
      //       config:
      //         configs[
      //           random
      //             ? Object.keys(configs)[Math.floor(Math.random() * Object.keys(configs).length)]
      //             : naviOS
      //         ],
      //     },
      //   };
      // })(),
    };
  }

  // Instead of using map and join directly, use Promise.all
  async generateScriptSource() {
    const scriptPromises = Object.values(this.scriptConfigs).map(
      async ({ script, init, opts, enabled }) => {
        await init();
        if (!enabled) return "";
        else return `(${(await script(opts)).toString()})(${JSON.stringify(opts)});`;
      }
    );

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
