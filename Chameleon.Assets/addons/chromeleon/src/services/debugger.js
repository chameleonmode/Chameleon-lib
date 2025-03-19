//https://privacycheck.sec.lrz.de/active/fp_gcr/fp_getclientrects.html#fpGetClientRects
import PageMutations from "../modules/PageMutations.js";
import PageEmulations from "../modules/PageEmulations.js";
import { log } from "./logger.js";

// Keep track of mutator per tab
const observers = new Map();

// Attach the debugger to the tab
const attach = async (tabId) => {
  try {
    log.log(`Setting up iframe monitoring for tab ${tabId}`);

    // Attach debugger to the tab (as per requirements)
    await chrome.debugger.attach({ tabId }, "1.3");
    // Enable required domains for Chrome Debugger API
    await chrome.debugger.sendCommand({ tabId: tabId }, "DOM.enable");
    await chrome.debugger.sendCommand({ tabId: tabId }, "Runtime.enable");
    await chrome.debugger.sendCommand({ tabId: tabId }, "Page.enable");

    observers.set(tabId, async () => {
      await new PageMutations(tabId).initialize();
      await new PageEmulations(tabId).initialize();
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
  if (changeInfo.status !== "loading" || !tab.url.startsWith("http")) {
    return;
  }
  if (!observers.has(tabId)) await attach(tabId);
  await observers.get(tabId)();
});

chrome.tabs.onRemoved.addListener(async (tabId) => {
  log.log(`Tab ${tabId} was removed`);
  await detach(tabId);
});
