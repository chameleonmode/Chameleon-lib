// This file is responsible for managing the Chrome Debugger API

import App from "../app.js";
import { log } from "./logger.js";

// Keep track of mutator per tab
const observers = new Map();

// Subscribe to "dataUpdated" event
App.eventSystem.subscribe("configUpdated", async (data) => {
  log.log("Confg updated:", data);

  for (const [tabId, observer] of observers.entries()) {
    try {
      await chrome.debugger.detach({ tabId });
    } catch {}
    await chrome.debugger.attach({ tabId }, "1.3");
  }
});

// Attach the debugger to the tab
const attach = async (tabId, tab) => {
  try {
    log.log(`Setting up iframe monitoring for tab ${tabId}`);
    // Attach debugger to the tab (as per requirements)
    await chrome.debugger.attach({ tabId }, "1.3");
    
    observers.set(tabId, {
      initialize: async () => {
        // Enable required domains for Chrome Debugger API
        await chrome.debugger.sendCommand({ tabId }, "Page.enable");

        // await new PageMutations(tabId).initialize();
        // await new PageEmulations(tabId).initialize();
      },
    });
  } catch (error) {
    log.error(`Error setting up iframe monitoring for tab ${tabId}:`, error);
    await detach(tabId);
  }
};

// Detach the debugger from the tab
const detach = async (tabId) => {
  try {
    await chrome.debugger.detach({ tabId });
  } catch (e) {
    // Ignore errors when detaching
  }
  if (observers.get(tabId)) {
    observers.delete(tabId);
  }
};

chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
  log.info(`Tab ${tabId} is loading with URL: ${tab.url}`);
  // if (changeInfo.status !== "loading" || !tab.url.startsWith("http")) {
  //   return;
  // }
  // if (!App.config.enabled || App.config.bypass.some((bypassUrl) => tab.url.startsWith(bypassUrl))) {
  //   log.log(`Tab ${tabId} is bypassed`);
  //   await detach(tabId);
  //   return;
  // }
  // if (!observers.has(tabId)) await attach(tabId, tab);
  // await observers.get(tabId).initialize();
});

// chrome.tabs.onRemoved.addListener(async (tabId) => {
//   log.log(`Tab ${tabId} was removed`);
//   await detach(tabId);
// });