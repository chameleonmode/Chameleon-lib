import { log } from "./logger.js";
// Description: This file is responsible for setting up mutation observers in the main document and all iframes in the inspected tab.
chrome.tabs.query({}, async (tabs) => {
  for (const tab of tabs) {
    if(tab.url.startsWith("chrome://")) continue;
    await attachDebugger(tab.id);
  }
});

chrome.tabs.onCreated.addListener(async (tab) => {
  await attachDebugger(tab.id);
});

// Clean up when a tab is closed
chrome.tabs.onRemoved.addListener((tab) => {
  if (activeTabs.has(tab.id)) {
    detachDebugger(tab.id);
  }
});
// Clean up when done
chrome.debugger.onDetach.addListener((debuggee, reason) => {
  log.info("Debugger detached:", reason);
});

// chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
//   if (changeInfo.status === "loading") {
//     await onEvent(tab);
//   }
// });

const activeTabs = new Map(); // Track tabs with active debugger sessions

async function attachDebugger(tabId) {
  try {
    if (activeTabs.has(tabId)) return;

    // Attach debugger to the tab
    await chrome.debugger.attach({ tabId }, "1.3");

    // Enable required domains
    await chrome.debugger.sendCommand({ tabId }, "DOM.enable");
    await chrome.debugger.sendCommand({ tabId }, "Runtime.enable");
    await chrome.debugger.sendCommand({ tabId }, "Page.enable");

    // Track this tab
    activeTabs.set(tabId, true);

// Create observer that only monitors iframes, including their document changes and shadow DOM
const observer = new WebpageMutationObserver(tabId, (data) => {
  console.log("Mutation event:", data);
  switch (data.type) {
    case 'iframe-element-added':
      console.log(`New element added to iframe ${data.frameId}:`);
      console.log(`- Element: ${data.element.nodeName}#${data.element.id}`);
      console.log(`- Path: ${data.element.path}`);
      console.log(`- Added to: ${data.parent.path}`);
      console.log(`- Content preview: ${data.element.innerHTML?.substring(0, 100)}...`);
      
      // You can inspect attributes
      if (data.element.attributes && data.element.attributes.length) {
        console.log('- Attributes:', data.element.attributes);
      }
      break;
      
    case 'iframe-element-removed':
      console.log(`Element removed from iframe ${data.frameId}:`);
      console.log(`- Element: ${data.element.nodeName}#${data.element.id}`);
      console.log(`- Path: ${data.element.path}`);
      console.log(`- Removed from: ${data.parent.path}`);
      break;
  }
}, {
  iframesOnly: true,
  includeDocumentChanges: true
});

await observer.start();

    // Start observing
    await observer.start();

    // Later, when you want to stop observing
    // await observer.stop();

    log.log(`Debugger attached to tab ${tabId}`);
  } catch (error) {
    log.error(`Failed to attach debugger to tab ${tabId}:`, error);
  }
}

async function detachDebugger(tabId) {
  try {
    await chrome.debugger.detach({ tabId });
    activeTabs.delete(tabId);

    log.log(`Debugger detached from tab ${tabId}`);
  } catch (error) {
    log.error(`Failed to detach debugger from tab ${tabId}:`, error);
  }
}

/**
 * Module for observing DOM mutations across a webpage, including document, iframes, and shadow DOM.
 * Works with Chrome's debugger API.
 */
export class WebpageMutationObserver {
  /**
   * @param {number} tabId - The Chrome tab ID to observe
   * @param {Function} callback - Optional callback function that receives mutation events
   * @param {Object} options - Configuration options
   * @param {boolean} options.iframesOnly - If true, only monitor iframe content, not the main document or shadow DOM
   * @param {boolean} options.includeDocumentChanges - If true, monitor document changes (default: true)
   * @param {boolean} options.includeShadowDOM - If true, monitor shadow DOM changes (default: true)
   */
  constructor(tabId, callback = null, options = {}) {
    this.tabId = tabId;
    this.callback = callback;
    this.observedFrames = new Set();
    this.isObserving = false;
    this.mainFrameId = null;
    
    // Set default options
    this.options = {
      iframesOnly: false,
      includeDocumentChanges: true,
      includeShadowDOM: true,
      ...options
    };
    
    // If iframesOnly is true, override other options for main document
    if (this.options.iframesOnly) {
      this.options.includeDocumentChanges = false;
      this.options.includeShadowDOM = false;
    }
    
    // Bind methods to maintain 'this' context
    this.onDebuggerEvent = this.onDebuggerEvent.bind(this);
  }

  /**
   * Start observing mutations across the webpage
   * @returns {Promise<boolean>} Whether the operation was successful
   */
  async start() {
    if (this.isObserving) {
      console.log("Already observing mutations");
      return true;
    }

    try {
      // Add debugger event listener
      chrome.debugger.onEvent.addListener(this.onDebuggerEvent);
      
      // Add binding for mutation reporting from page contexts
      await chrome.debugger.sendCommand(
        { tabId: this.tabId },
        "Runtime.addBinding",
        { name: "reportMutation" }
      );
      
      // Get the frame tree to find all frames
      const { frameTree } = await chrome.debugger.sendCommand(
        { tabId: this.tabId },
        "Page.getFrameTree"
      );
      
      // Store the main frame ID to help filter events in iframesOnly mode
      this.mainFrameId = frameTree.frame.id;
      
      // Start with the main frame if we're not in iframesOnly mode or we need to detect iframes
      await this.observeFrame(frameTree.frame.id);
      
      // Process all child frames recursively (we always process iframes)
      if (frameTree.childFrames && frameTree.childFrames.length > 0) {
        await this.processChildFrames(frameTree.childFrames);
      }
      
      // Set up event listener for frame navigation/attachment
      this.isObserving = true;
      
      return true;
    } catch (error) {
      console.error("Failed to start observing:", error);
      chrome.debugger.onEvent.removeListener(this.onDebuggerEvent);
      return false;
    }
  }
  
  /**
   * Process child frames recursively
   * @param {Array} childFrames - Array of child frame objects
   */
  async processChildFrames(childFrames) {
    for (const frameInfo of childFrames) {
      // Pass parent info when observing a child frame
      await this.observeFrame(frameInfo.frame.id, { 
        parentFrameId: frameInfo.frame.parentId 
      });
      
      // After observing each frame, check if we need to monitor its iframes' contents
      if (frameInfo.childFrames && frameInfo.childFrames.length > 0) {
        await this.processChildFrames(frameInfo.childFrames);
      }
    }
  }
  
  /**
   * Set up mutation observation for a specific frame
   * @param {string} frameId - The ID of the frame to observe
   * @param {Object} params - Optional parameters like parentFrameId for iframes
   */
  async observeFrame(frameId, params = {}) {
    if (this.observedFrames.has(frameId)) {
      return true; // Already observing this frame
    }
    
    try {
      // Set up observers for this frame based on options
      const result = await chrome.debugger.sendCommand(
        { tabId: this.tabId },
        "Runtime.evaluate",
        {
          expression: `
            (function() {
              // Helper to get a node's path for identification
              function getNodePath(node) {
                if (!node || node.nodeType !== 1) return '';
                if (node === document) return 'document';
                
                const path = [];
                let current = node;
                
                while (current && current !== document) {
                  let identifier = current.nodeName.toLowerCase();
                  
                  if (current.id) {
                    identifier += '#' + current.id;
                  } else if (current.className && typeof current.className === 'string') {
                    identifier += '.' + current.className.trim().replace(/\\s+/g, '.');
                  }
                  
                  path.unshift(identifier);
                  current = current.parentElement;
                }
                
                return path.join(' > ');
              }
              
              // Function to observe a shadow root - only if includeShadowDOM is true
              function observeShadowRoot(element) {
                if (!element || !element.shadowRoot) return;
                
                // Skip shadow DOM observation if not enabled
                if (${!this.options.includeShadowDOM}) return;
                
                // Generate ID for this shadow root
                const shadowId = 'shadow_' + Math.random().toString(36).substring(2, 9);
                
                // Report shadow root found
                window.reportMutation(JSON.stringify({
                  type: 'shadow-root-detected',
                  frameId: '${frameId}',
                  url: document.location.href,
                  timestamp: Date.now(),
                  shadowRoot: {
                    id: shadowId,
                    hostNodeName: element.nodeName,
                    hostNodeId: element.id || null,
                    hostNodePath: getNodePath(element)
                  }
                }));
                
                // Create observer for shadow DOM
                const shadowObserver = new MutationObserver(mutations => {
                  window.reportMutation(JSON.stringify({
                    type: 'shadow-dom-mutation',
                    shadowId: shadowId,
                    frameId: '${frameId}',
                    url: document.location.href,
                    timestamp: Date.now(),
                    host: {
                      nodeName: element.nodeName,
                      id: element.id || null,
                      path: getNodePath(element)
                    },
                    mutations: mutations.map(m => ({
                      type: m.type,
                      target: m.target.nodeName,
                      targetPath: getNodePath(m.target),
                      addedNodes: Array.from(m.addedNodes).map(n => ({
                        nodeName: n.nodeName,
                        nodeType: n.nodeType,
                        id: n.id || null
                      })),
                      removedNodes: Array.from(m.removedNodes).map(n => ({
                        nodeName: n.nodeName,
                        nodeType: n.nodeType,
                        id: n.id || null
                      })),
                      attributeName: m.attributeName || null,
                      oldValue: m.oldValue || null,
                      newValue: m.attributeName ? m.target.getAttribute(m.attributeName) : null
                    }))
                  }));
                  
                  // Check for new shadow roots in added nodes
                  mutations.forEach(mutation => {
                    if (mutation.type === 'childList') {
                      Array.from(mutation.addedNodes).forEach(node => {
                        if (node.nodeType === 1) { // Element node
                          observeShadowRoot(node);
                          
                          try {
                            // Check descendants for shadow roots
                            const elements = node.querySelectorAll('*');
                            elements.forEach(el => observeShadowRoot(el));
                          } catch (e) {
                            // Silently ignore errors for cross-origin elements
                          }
                        }
                      });
                    }
                  });
                });
                
                // Observe all changes in the shadow DOM
                shadowObserver.observe(element.shadowRoot, {
                  childList: true,
                  attributes: true,
                  characterData: true,
                  subtree: true,
                  attributeOldValue: true,
                  characterDataOldValue: true
                });
                
                // Store observer for cleanup
                window.__shadowObservers = window.__shadowObservers || [];
                window.__shadowObservers.push(shadowObserver);
                
                // Check for nested shadow roots
                try {
                  const nestedElements = element.shadowRoot.querySelectorAll('*');
                  nestedElements.forEach(el => observeShadowRoot(el));
                } catch (e) {
                  // Silently ignore errors for cross-origin elements
                }
              }
              
              // Function to set up iframe detection
              function setupIframeDetection() {
                // This is a minimal observer just to detect iframes and their mutations
                const iframeDetector = new MutationObserver(mutations => {
                  mutations.forEach(mutation => {
                    // Track added iframes
                    if (mutation.type === 'childList') {
                      Array.from(mutation.addedNodes).forEach(node => {
                        if (node.nodeType === 1) { // Element node
                          // Check if it's an iframe
                          if (node.nodeName === 'IFRAME') {
                            window.reportMutation(JSON.stringify({
                              type: 'iframe-detected',
                              frameId: '${frameId}',
                              url: document.location.href,
                              timestamp: Date.now(),
                              iframe: {
                                id: node.id || null,
                                name: node.name || null,
                                src: node.src || null,
                                path: getNodePath(node)
                              },
                              mutation: {
                                type: mutation.type,
                                target: mutation.target.nodeName,
                                targetPath: getNodePath(mutation.target),
                                addedNodes: Array.from(mutation.addedNodes).map(n => ({
                                  nodeName: n.nodeName,
                                  nodeType: n.nodeType,
                                  id: n.id || null
                                })),
                                removedNodes: Array.from(mutation.removedNodes).map(n => ({
                                  nodeName: n.nodeName,
                                  nodeType: n.nodeType,
                                  id: n.id || null
                                }))
                              }
                            }));
                          }
                          
                          try {
                            // Check descendants for iframes
                            const iframes = node.querySelectorAll('iframe');
                            iframes.forEach(iframe => {
                              window.reportMutation(JSON.stringify({
                                type: 'iframe-detected',
                                frameId: '${frameId}',
                                url: document.location.href,
                                timestamp: Date.now(),
                                iframe: {
                                  id: iframe.id || null,
                                  name: iframe.name || null,
                                  src: iframe.src || null,
                                  path: getNodePath(iframe)
                                },
                                mutation: {
                                  type: mutation.type,
                                  target: mutation.target.nodeName,
                                  targetPath: getNodePath(mutation.target),
                                  parentNode: getNodePath(node),
                                  nestedDiscovery: true
                                }
                              }));
                            });
                          } catch (e) {
                            // Silently ignore errors for cross-origin elements
                          }
                        }
                      });
                      
                      // Track removed iframes
                      Array.from(mutation.removedNodes).forEach(node => {
                        if (node.nodeType === 1 && node.nodeName === 'IFRAME') {
                          window.reportMutation(JSON.stringify({
                            type: 'iframe-removed',
                            frameId: '${frameId}',
                            url: document.location.href,
                            timestamp: Date.now(),
                            iframe: {
                              id: node.id || null,
                              name: node.name || null,
                              src: node.src || null,
                              path: getNodePath(node)
                            },
                            mutation: {
                              type: mutation.type,
                              target: mutation.target.nodeName,
                              targetPath: getNodePath(mutation.target)
                            }
                          }));
                        }
                      });
                    }
                    
                    // Track attribute changes to iframes
                    if (mutation.type === 'attributes' && mutation.target.nodeName === 'IFRAME') {
                      window.reportMutation(JSON.stringify({
                        type: 'iframe-attribute-changed',
                        frameId: '${frameId}',
                        url: document.location.href,
                        timestamp: Date.now(),
                        iframe: {
                          id: mutation.target.id || null,
                          name: mutation.target.name || null,
                          src: mutation.target.src || null,
                          path: getNodePath(mutation.target)
                        },
                        mutation: {
                          type: mutation.type,
                          attributeName: mutation.attributeName,
                          oldValue: mutation.oldValue,
                          newValue: mutation.target.getAttribute(mutation.attributeName)
                        }
                      }));
                    }
                  });
                });
                
                // Start observing for iframes and their mutations
                iframeDetector.observe(document, {
                  childList: true,
                  attributes: true,
                  attributeOldValue: true,
                  subtree: true
                });
                
                // Store detector for cleanup
                window.__iframeDetector = iframeDetector;
                
                // Initial scan for existing iframes
                try {
                  const iframes = document.querySelectorAll('iframe');
                  iframes.forEach(iframe => {
                    window.reportMutation(JSON.stringify({
                      type: 'iframe-exists',
                      frameId: '${frameId}',
                      url: document.location.href,
                      timestamp: Date.now(),
                      iframe: {
                        id: iframe.id || null,
                        name: iframe.name || null,
                        src: iframe.src || null,
                        path: getNodePath(iframe)
                      },
                      initialScan: true
                    }));
                  });
                } catch (e) {
                  console.error('Error scanning for iframes:', e);
                }
              }
              
              // If we're only monitoring iframes, set up just the iframe detection for main document
              if (${this.options.iframesOnly && !params.parentFrameId}) {
                setupIframeDetection();
                return true;
              }
              
              // Main document observer - only if includeDocumentChanges is true for non-iframes
              // Or always for iframe frames (if document changes are enabled)
              if ((${this.options.includeDocumentChanges} && ${!params.parentFrameId}) || 
                  (${params.parentFrameId} && ${this.options.includeDocumentChanges})) {
                const docObserver = new MutationObserver(mutations => {
                  // First report the general document mutation
                  window.reportMutation(JSON.stringify({
                    type: ${params.parentFrameId} ? '"iframe-document-mutation"' : '"document-mutation"',
                    frameId: '${frameId}',
                    url: document.location.href,
                    timestamp: Date.now(),
                    mutations: mutations.map(m => ({
                      type: m.type,
                      target: m.target.nodeName,
                      targetPath: getNodePath(m.target),
                      addedNodes: Array.from(m.addedNodes).map(n => ({
                        nodeName: n.nodeName,
                        nodeType: n.nodeType,
                        id: n.id || null
                      })),
                      removedNodes: Array.from(m.removedNodes).map(n => ({
                        nodeName: n.nodeName,
                        nodeType: n.nodeType,
                        id: n.id || null
                      })),
                      attributeName: m.attributeName || null,
                      oldValue: m.oldValue || null,
                      newValue: m.attributeName ? m.target.getAttribute(m.attributeName) : null
                    }))
                  }));
                  
                  // Then send specific notifications for element additions and removals in iframes
                  if (${params.parentFrameId}) {
                    mutations.forEach(mutation => {
                      if (mutation.type === 'childList') {
                        // Report added elements in iframe
                        if (mutation.addedNodes.length > 0) {
                          Array.from(mutation.addedNodes).forEach(node => {
                            if (node.nodeType === 1) { // Element node
                              window.reportMutation(JSON.stringify({
                                type: 'iframe-element-added',
                                frameId: '${frameId}',
                                url: document.location.href,
                                timestamp: Date.now(),
                                element: {
                                  nodeName: node.nodeName,
                                  nodeType: node.nodeType,
                                  id: node.id || null,
                                  className: node.className || null,
                                  path: getNodePath(node),
                                  innerHTML: node.outerHTML ? node.outerHTML.substring(0, 500) : null, // First 500 chars for context
                                  childElementCount: node.childElementCount || 0,
                                  attributes: node.hasAttributes ? Array.from(node.attributes || []).map(attr => ({
                                    name: attr.name,
                                    value: attr.value
                                  })) : []
                                },
                                parent: {
                                  nodeName: mutation.target.nodeName,
                                  nodeType: mutation.target.nodeType,
                                  id: mutation.target.id || null,
                                  path: getNodePath(mutation.target)
                                }
                              }));
                            }
                          });
                        }
                        
                        // Report removed elements from iframe
                        if (mutation.removedNodes.length > 0) {
                          Array.from(mutation.removedNodes).forEach(node => {
                            if (node.nodeType === 1) { // Element node
                              window.reportMutation(JSON.stringify({
                                type: 'iframe-element-removed',
                                frameId: '${frameId}',
                                url: document.location.href,
                                timestamp: Date.now(),
                                element: {
                                  nodeName: node.nodeName,
                                  nodeType: node.nodeType,
                                  id: node.id || null,
                                  className: node.className || null,
                                  path: getNodePath(node)
                                },
                                parent: {
                                  nodeName: mutation.target.nodeName,
                                  nodeType: mutation.target.nodeType,
                                  id: mutation.target.id || null,
                                  path: getNodePath(mutation.target)
                                }
                              }));
                            }
                          });
                        }
                      }
                    });
                  }
                  
                  // Check for new iframes and shadow roots
                  mutations.forEach(mutation => {
                    if (mutation.type === 'childList') {
                      Array.from(mutation.addedNodes).forEach(node => {
                        if (node.nodeType === 1) { // Element node
                          // Check if it's an iframe
                          if (node.nodeName === 'IFRAME') {
                            window.reportMutation(JSON.stringify({
                              type: 'iframe-detected',
                              frameId: '${frameId}',
                              url: document.location.href,
                              timestamp: Date.now(),
                              iframe: {
                                id: node.id || null,
                                name: node.name || null,
                                src: node.src || null,
                                path: getNodePath(node)
                              },
                              mutation: {
                                type: mutation.type,
                                target: mutation.target.nodeName,
                                targetPath: getNodePath(mutation.target),
                                addedNodes: Array.from(mutation.addedNodes).map(n => ({
                                  nodeName: n.nodeName,
                                  nodeType: n.nodeType,
                                  id: n.id || null
                                })),
                                removedNodes: Array.from(mutation.removedNodes).map(n => ({
                                  nodeName: n.nodeName,
                                  nodeType: n.nodeType,
                                  id: n.id || null
                                }))
                              }
                            }));
                          }
                          
                          // Check for shadow root
                          if (${this.options.includeShadowDOM} || ${params.parentFrameId && this.options.includeShadowDOM}) {
                            observeShadowRoot(node);
                            
                            try {
                              // Check descendants for shadow roots
                              const elements = node.querySelectorAll('*');
                              elements.forEach(el => observeShadowRoot(el));
                            } catch (e) {
                              // Silently ignore errors for cross-origin elements
                            }
                          }
                        }
                      });
                    }
                  });
                });
                
                // Start observing the document
                docObserver.observe(document, {
                  childList: true,
                  attributes: true,
                  characterData: true,
                  subtree: true,
                  attributeOldValue: true,
                  characterDataOldValue: true
                });
                
                // Store observer for cleanup
                window.__docObserver = docObserver;
              } else {
                // If we're not monitoring document changes but need iframe detection
                setupIframeDetection();
              }
              
              // Initial scan for existing shadow roots
              if ((${this.options.includeShadowDOM} && ${!params.parentFrameId}) || 
                  (${params.parentFrameId} && ${this.options.includeShadowDOM})) {
                try {
                  const elements = document.querySelectorAll('*');
                  elements.forEach(el => observeShadowRoot(el));
                } catch (e) {
                  console.error('Error scanning for shadow roots:', e);
                }
              }
              
              return true;
            })()
          `,
          frameId,
          returnByValue: true
        }
      );
      
      if (result?.result?.value === true) {
        this.observedFrames.add(frameId);
        return true;
      }
      
      return false;
    } catch (error) {
      console.error(`Error observing frame ${frameId}:`, error);
      return false;
    }
  }
  
  /**
   * Handle Chrome debugger events
   * @param {Object} debuggeeId - The debuggee identifier
   * @param {string} method - The method name
   * @param {Object} params - The event parameters
   */
  onDebuggerEvent(debuggeeId, method, params) {
    console.log("Debugger event:", method, params);
    if (debuggeeId.tabId !== this.tabId) return;
    
    switch (method) {
      case "DOM.documentUpdated":
        // Document has been refreshed or navigated
        if (this.isObserving) {
          console.log("Document updated, re-observing...");
          this.observedFrames.clear();
          this.start();
        }
        break;
        
      case "Page.frameAttached":
        // New iframe attached
        if (this.isObserving && params.frameId) {
          console.log("Frame attached:", params.frameId);
          this.observeFrame(params.frameId, { parentFrameId: params.parentFrameId });
        }
        break;
        
      case "Page.frameNavigated":
        // Frame navigated to a new URL
        if (this.isObserving && params.frame && params.frame.id) {
          console.log("Frame navigated:", params.frame.id);
          // Only re-observe if it's an iframe or if we're not in iframesOnly mode
          if (!this.options.iframesOnly || params.frame.parentId) {
            this.observeFrame(params.frame.id, { parentFrameId: params.frame.parentId });
          }
        }
        break;
        
      case "Runtime.bindingCalled":
        // Handle binding calls from our injected code
        if (params.name === "reportMutation" && params.payload) {
          try {
            const data = JSON.parse(params.payload);
            // If we're in iframesOnly mode, only process iframe-related events
            if (!this.options.iframesOnly || 
                data.type.includes('iframe') || 
                (data.frameId && data.frameId !== this.mainFrameId)) {
              this.processMutation(data);
            }
          } catch (e) {
            console.error("Error processing mutation data:", e);
          }
        }
        break;
    }
  }
  
  /**
   * Process mutation data and invoke callback
   * @param {Object} data - The mutation data
   */
  processMutation(data) {
    if (this.callback) {
      this.callback(data);
    }
  }
  
  /**
   * Stop observing mutations
   * @returns {Promise<boolean>} Whether stopping was successful
   */
  async stop() {
    if (!this.isObserving) {
      return false;
    }
    
    try {
      // Remove event listener
      chrome.debugger.onEvent.removeListener(this.onDebuggerEvent);
      
      // Disconnect all observers in all frames
      for (const frameId of this.observedFrames) {
        try {
          await chrome.debugger.sendCommand(
            { tabId: this.tabId },
            "Runtime.evaluate",
            {
              expression: `
                (function() {
                  // Disconnect document observer
                  if (window.__docObserver) {
                    window.__docObserver.disconnect();
                    window.__docObserver = null;
                  }
                  
                  // Disconnect iframe detector
                  if (window.__iframeDetector) {
                    window.__iframeDetector.disconnect();
                    window.__iframeDetector = null;
                  }
                  
                  // Disconnect shadow DOM observers
                  if (window.__shadowObservers) {
                    window.__shadowObservers.forEach(observer => observer.disconnect());
                    window.__shadowObservers = [];
                  }
                  
                  // Make sure to also clean up iframe-specific observers
                  if (window.__frameDocObserver) {
                    window.__frameDocObserver.disconnect();
                    window.__frameDocObserver = null;
                  }
                  
                  if (window.__frameShadowRootDetector) {
                    window.__frameShadowRootDetector.disconnect();
                    window.__frameShadowRootDetector = null;
                  }
                  
                  if (window.__frameShadowObservers) {
                    window.__frameShadowObservers.forEach(observer => observer.disconnect());
                    window.__frameShadowObservers = [];
                  }
                  
                  return true;
                })()
              `,
              frameId,
              returnByValue: true
            }
          );
        } catch (error) {
          console.warn(`Failed to clean up observers in frame ${frameId}:`, error);
        }
      }
      
      this.observedFrames.clear();
      this.isObserving = false;
      
      return true;
    } catch (error) {
      console.error("Failed to stop observation:", error);
      return false;
    }
  }
}