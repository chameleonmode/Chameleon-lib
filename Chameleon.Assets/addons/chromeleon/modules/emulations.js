// Keep track of observers per tab
const tabObservers = new Map();

/**
 * Initialize iframe monitoring for a specific tab
 * @param {number} tabId - The ID of the tab to monitor
 */
async function monitorTabIframes(tabId) {
  try {
    console.log(`Setting up iframe monitoring for tab ${tabId}`);

    // Attach debugger to the tab (as per requirements)
    await chrome.debugger.attach({ tabId }, "1.3");

    // Enable required domains (as per requirements)
    await chrome.debugger.sendCommand({ tabId }, "DOM.enable");
    await chrome.debugger.sendCommand({ tabId }, "Runtime.enable");
    await chrome.debugger.sendCommand({ tabId }, "Page.enable");

    // Initialize the mutation observer
    const observer = new WebpageMutationObserver();
    const success = await observer.initialize(tabId);

    if (!success) {
      console.error(`Failed to initialize WebpageMutationObserver for tab ${tabId}`);
      return null;
    }

    // Add a listener for iframe mutations with detailed logging
    observer.addMutationListener((mutation) => {
      console.log(`Received iframe mutation in tab ${tabId}:`, mutation);

      // Handle different types of mutations
      switch (mutation.type) {
        case "iframe-added":
          console.log(`New iframe added to the page: ${mutation.nodeId}`);
          break;

        case "iframe-removed":
          console.log(`Iframe removed from the page: ${mutation.nodeId}`);
          break;

        case "iframe-content-changed":
          console.log(`Content ${mutation.action} in iframe ${mutation.iframeNodeId}`);
          break;

        case "iframe-src-changed":
          console.log(`Iframe src changed to: ${mutation.newSrc}`);
          break;

        case "iframe-create-element":
          console.log(
            `[IMPORTANT] createElement called in iframe ${
              mutation.iframeNodeId || "unknown"
            } to create a <${mutation.tagName}> element`
          );
          console.log(`  Frame location: ${mutation.frameLocation}`);
          console.log(`  Timestamp: ${new Date(mutation.timestamp).toISOString()}`);

          // Send notifications to your popup or content script
          chrome.runtime
            .sendMessage({
              type: "iframe-activity",
              action: "element-created",
              details: {
                tabId: tabId,
                iframeId: mutation.iframeNodeId,
                tagName: mutation.tagName,
                timestamp: mutation.timestamp,
                url: mutation.frameLocation,
              },
            })
            .catch((err) => {
              // This error is normal if no listener is active
              console.log("Message sending failed (this is normal if popup is closed):", err.message);
            });

          // Test - force manual element creation in iframes to verify events
          testCreateElementInIframes(tabId);

          break;
      }
    });

    // Store the observer instance for later cleanup
    tabObservers.set(tabId, observer);

    // Test - Initial test to force element creation in iframes
    // setTimeout(() => {
    //   testCreateElementInIframes(tabId);
    // }, 2000);

    // Set up the emulation
    const { tzEmulation, timezone, tzSystem, tzLocale, tzRandomize } = await chrome.storage.local.get([
      "tzEmulation",
      "timezone",
      "tzSystem",
      "tzRandomize",
      "tzLocale",
    ]); 
    if (tzEmulation) {
      log.info(`Applying timezone emulation for tab ${tab.id}`);
      log.info(`Timezone: ${timezone}`);
      log.info(`System timezone: ${tzSystem}`);
      log.info(`Randomize timezone: ${tzRandomize}`);
      log.info(`Locale: ${tzLocale}`);
      await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setTimezoneOverride", {
        timezoneId: tzSystem
          ? Intl.DateTimeFormat().resolvedOptions().timeZone
          : tzRandomize
          ? Object.keys(offsets)[Math.floor(Math.random() * Object.keys(offsets).length)]
          : timezone,
      });
      await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setLocaleOverride", {
        locale: tzLocale,
      });
    }

    const { geoEmulation, lat, lon, geoRandomize, geoAccuracy } = await chrome.storage.local.get([
      "geoEmulation",
      "lat",
      "lon",
      "geoRandomize",
      "geoAccuracy",
    ]);
    if (geoEmulation) {
      log.info(`Applying geolocation emulation for tab ${tab.id}`);
      log.info(`Latitude: ${lat}`);
      log.info(`Longitude: ${lon}`);
      log.info(`Randomize geolocation: ${geoRandomize}`);
      log.info(`Accuracy: ${geoAccuracy}`);
      await chrome.debugger.sendCommand({ tabId: tab.id }, "Emulation.setGeolocationOverride", {
        latitude: geoRandomize ? lat + (Math.random() - 0.5) * geoRandomize : lat,
        longitude: geoRandomize ? lon + (Math.random() - 0.5) * geoRandomize : lon,
        accuracy: geoAccuracy,
      });
    }

    console.log(`Successfully set up iframe monitoring for tab ${tabId}`);
    return observer;
  } catch (error) {
    console.error(`Error setting up iframe monitoring for tab ${tabId}:`, error);

    // Try to clean up if we failed
    try {
      chrome.debugger.detach({ tabId }).catch(() => {
        // Ignore errors when detaching
      });
    } catch (e) {
      // Ignore cleanup errors
    }

    return null;
  }
}

/**
 * Test function to force element creation in iframes
 * @param {number} tabId - The ID of the tab to test
 */
async function testCreateElementInIframes(tabId) {
  try {
    console.log(`Testing createElement in iframes for tab ${tabId}`);

    // Get all frames in the page
    const response = await chrome.debugger.sendCommand({ tabId }, "Page.getFrameTree");

    if (!response || !response.frameTree) {
      console.log("No frame tree found");
      return;
    }

    // Helper to extract all frames from the tree
    const extractFrames = (tree) => {
      const frames = [];

      // Add the current frame
      frames.push({
        id: tree.frame.id,
        url: tree.frame.url,
        parentId: tree.frame.parentId || null,
      });

      // Add child frames recursively
      if (tree.childFrames) {
        for (const child of tree.childFrames) {
          frames.push(...extractFrames(child));
        }
      }

      return frames;
    };

    const frames = extractFrames(response.frameTree);
    console.log(`Found ${frames.length} frames in the page for testing`);

    // Skip the main frame, we only want to test in iframes
    const childFrames = frames.filter((frame) => frame.parentId !== null);
    console.log(`Will test in ${childFrames.length} child frames`);

    // For each child frame, create a test element
    for (const frame of childFrames) {
      try {
        console.log(`Testing createElement in frame ${frame.id} (${frame.url})`);

        await chrome.debugger.sendCommand({ tabId }, "Runtime.evaluate", {
          expression: `
              (function() {
                try {
                  console.log("Forcing test element creation in iframe");
                  // Create a test element
                  const testElement = document.createElement('div');
                  testElement.id = 'test-element-' + Date.now();
                  testElement.innerText = 'Test Element';
                  
                  // Optionally add it to the DOM
                  if (document.body) {
                    document.body.appendChild(testElement);
                  }
                  
                  return "Test element created: " + testElement.id;
                } catch(e) {
                  console.error("Error creating test element:", e);
                  return "Error: " + e.message;
                }
              })();
            `,
          frameId: frame.id,
          returnByValue: true,
        });
      } catch (error) {
        // This is expected for cross-origin iframes
        console.log(`Could not test in frame ${frame.id}: ${error.message}`);
      }
    }
  } catch (error) {
    console.error("Error in test function:", error);
  }
}

/**
 * Clean up monitoring for a specific tab
 * @param {number} tabId - The ID of the tab to clean up
 */
function cleanupTabMonitoring(tabId) {
  try {
    console.log(`Cleaning up iframe monitoring for tab ${tabId}`);

    const observer = tabObservers.get(tabId);
    if (observer) {
      observer.dispose();
      tabObservers.delete(tabId);
    }

    // Detach debugger
    chrome.debugger.detach({ tabId }).catch(() => {
      // Ignore errors when detaching (tab might be gone already)
    });

    console.log(`Successfully cleaned up iframe monitoring for tab ${tabId}`);
  } catch (error) {
    console.error(`Error cleaning up iframe monitoring for tab ${tabId}:`, error);
  }
}

// Example: Start monitoring when a tab is activated
chrome.tabs.onActivated.addListener(async (activeInfo) => {
  const { tabId } = activeInfo;

  // Only attach if not already monitoring this tab
  if (!tabObservers.has(tabId)) {
    await monitorTabIframes(tabId);
  }
});

// Cleanup when a tab is closed
chrome.tabs.onRemoved.addListener((tabId) => {
  cleanupTabMonitoring(tabId);
});

// Optional: Provide a way to toggle monitoring from your extension's UI
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.action === "toggleIframeMonitoring") {
    const { tabId, enable } = message;

    if (enable && !tabObservers.has(tabId)) {
      monitorTabIframes(tabId)
        .then(() => {
          sendResponse({ success: true });
        })
        .catch((error) => {
          sendResponse({ success: false, error: error.message });
        });
      return true; // Indicate we'll send response asynchronously
    } else if (!enable && tabObservers.has(tabId)) {
      cleanupTabMonitoring(tabId);
      sendResponse({ success: true });
    } else {
      sendResponse({ success: true, message: "No change needed" });
    }
  }
  return false;
});