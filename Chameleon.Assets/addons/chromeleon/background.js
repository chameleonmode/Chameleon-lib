import { App } from "./app.js";
import { log, setLogLevel } from "./modules/logger.js";
import { updateSettings, SETTINGS_ARRAY } from "./modules/settings.js";
import { updateLocationRules } from "./modules/uule.js";
import { applyOverrides } from "./modules/emulations.js";
import * as WebRTC from "./modules/webrtc.js";
//import "./modules/canvasing.js";

// Fix the incomplete runtime event listener
chrome.runtime.onInstalled.addListener(async () => {
  log.info("Extension installed");
  // Restore session from storage
  App.session = await chrome.storage.local.get("session");
  App.config = await chrome.storage.local.get("config");
  if (App.session && App.config) {
    log.info("Restored session", { session: App.session, config: App.config });
  }

  App.config.enabled = true;
  App.config.log = "all";
  App.config.dAPI = WebRTC.policies.disable_non_proxied_udp.id;
  App.config.canvasing = true;
  await chrome.storage.sync.set({ ...App.config });

  setLogLevel(App.config.log);
  createContextMenus();

  // await chrome.userScripts.configureWorld({
  //     csp: "script-src 'self'; object-src 'self'",
  // });
  // Register user scripts
});

// Background script approach for bypassing CSP in iframes
// This requires appropriate permissions in manifest.json:
// - "activeTab" or specific site permissions
// - "scripting" permission for Manifest V3

// Store your script content
const scriptContent = `
  // Your actual script code here
  console.log("Script executed successfully, bypassing CSP");
  
  // Add your functionality here
      const originalGetContext = HTMLCanvasElement.prototype.getContext;
  const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
  const originalToBlob = HTMLCanvasElement.prototype.toBlob;
  const originalFillText = CanvasRenderingContext2D.prototype.fillText;
  const originalFillRect = CanvasRenderingContext2D.prototype.fillRect;
  const originalGetImageData = CanvasRenderingContext2D.prototype.getImageData;
  const originalPutImageData = CanvasRenderingContext2D.prototype.putImageData;
  const originalDrawImage = CanvasRenderingContext2D.prototype.drawImage;
  const originalCreateLinearGradient = CanvasRenderingContext2D.prototype.createLinearGradient;
  const originalCreateRadialGradient = CanvasRenderingContext2D.prototype.createRadialGradient;
  const originalStrokeText = CanvasRenderingContext2D.prototype.strokeText;
  const originalMeasureText = CanvasRenderingContext2D.prototype.measureText;
  const originalIsPointInPath = CanvasRenderingContext2D.prototype.isPointInPath;

  // Helper function to decide if canvas is likely used for fingerprinting
  function isLikelyFingerprinting(context) {
    const canvas = context.canvas;
    // Small canvases are often used for fingerprinting
    return true; //canvas.width < 300 && canvas.height < 300;
  }

  // Helper function to add subtle noise
  function addNoise(value, factor = 0.04) {
    return value + (Math.random() * factor - factor / 2);
  }

  // Override fillText to add small variations to text rendering
  CanvasRenderingContext2D.prototype.fillText = function (text, x, y, maxWidth) {
    console.log("fillText intercepted:", text);

    if (isLikelyFingerprinting(this)) {
      // Modify coordinates slightly
      const xMod = addNoise(x);
      const yMod = addNoise(y);

      // Slightly modify the font if set
      const originalFont = this.font;
      if (originalFont && Math.random() < 0.3) {
        // 30% chance to modify font slightly
        const fontSize = parseFloat(originalFont);
        if (!isNaN(fontSize)) {
          this.font = originalFont.replace(
            fontSize.toString(),
            (fontSize + (Math.random() * 0.02 - 0.01)).toString()
          );
        }
      }

      const result =
        maxWidth !== undefined
          ? originalFillText.call(this, text, xMod, yMod, maxWidth)
          : originalFillText.call(this, text, xMod, yMod);

      // Restore original font
      if (originalFont) {
        this.font = originalFont;
      }

      return result;
    }

    return maxWidth !== undefined
      ? originalFillText.call(this, text, x, y, maxWidth)
      : originalFillText.call(this, text, x, y);
  };

  // Override strokeText similarly
  CanvasRenderingContext2D.prototype.strokeText = function (text, x, y, maxWidth) {
    console.log("strokeText intercepted");

    if (isLikelyFingerprinting(this)) {
      const xMod = addNoise(x);
      const yMod = addNoise(y);

      return maxWidth !== undefined
        ? originalStrokeText.call(this, text, xMod, yMod, maxWidth)
        : originalStrokeText.call(this, text, xMod, yMod);
    }

    return maxWidth !== undefined
      ? originalStrokeText.call(this, text, x, y, maxWidth)
      : originalStrokeText.call(this, text, x, y);
  };

  // Override measureText to add noise to text measurements
  CanvasRenderingContext2D.prototype.measureText = function (text) {
    console.log("measureText intercepted");
    const measurements = originalMeasureText.call(this, text);

    if (isLikelyFingerprinting(this)) {
      // Add tiny variations to width measurement
      const originalWidth = measurements.width;
      Object.defineProperty(measurements, "width", {
        get: function () {
          return originalWidth + (Math.random() * 0.02 - 0.01);
        },
      });
    }

    return measurements;
  };

  // Override fillRect to add subtle variations
  CanvasRenderingContext2D.prototype.fillRect = function (x, y, width, height) {
    console.log("fillRect intercepted");

    if (isLikelyFingerprinting(this)) {
      // Add subtle variations to rectangle dimensions
      const xMod = addNoise(x);
      const yMod = addNoise(y);
      const widthMod = addNoise(width);
      const heightMod = addNoise(height);
      return originalFillRect.call(this, xMod, yMod, widthMod, heightMod);
    }

    return originalFillRect.call(this, x, y, width, height);
  };

  // Override getImageData to add noise to pixel data
  CanvasRenderingContext2D.prototype.getImageData = function (x, y, width, height) {
    console.log("getImageData intercepted");
    const imageData = originalGetImageData.call(this, x, y, width, height);

    if (isLikelyFingerprinting(this)) {
      const pixels = imageData.data;
      // Add subtle random noise to random pixels
      for (let i = 0; i < pixels.length; i += 4) {
        if (Math.random() < 0.05) {
          // 5% of pixels
          for (let j = 0; j < 3; j++) {
            // Only modify RGB, not alpha
            pixels[i + j] = Math.max(0, Math.min(255, pixels[i + j] + (Math.random() > 0.5 ? 1 : -1)));
          }
        }
      }
    }

    return imageData;
  };

  // Override putImageData
  CanvasRenderingContext2D.prototype.putImageData = function (
    imageData,
    dx,
    dy,
    dirtyX,
    dirtyY,
    dirtyWidth,
    dirtyHeight
  ) {
    console.log("putImageData intercepted");

    if (isLikelyFingerprinting(this) && arguments.length <= 3) {
      // Only modify if it's the simple version of the call
      const dxMod = addNoise(dx, 0.02);
      const dyMod = addNoise(dy, 0.02);
      return originalPutImageData.call(this, imageData, dxMod, dyMod);
    }

    return originalPutImageData.apply(this, arguments);
  };

  // Override drawImage to add subtle variations
  CanvasRenderingContext2D.prototype.drawImage = function (image, ...args) {
    console.log("drawImage intercepted");

    if (isLikelyFingerprinting(this)) {
      // Add subtle variations to positioning parameters
      // Different signatures: (img, dx, dy), (img, dx, dy, dw, dh), (img, sx, sy, sw, sh, dx, dy, dw, dh)
      const modifiedArgs = args.map((arg) => {
        return typeof arg === "number" ? addNoise(arg, 0.03) : arg;
      });

      return originalDrawImage.call(this, image, ...modifiedArgs);
    }

    return originalDrawImage.apply(this, [image, ...args]);
  };

  // Override gradient creation methods
  CanvasRenderingContext2D.prototype.createLinearGradient = function (x0, y0, x1, y1) {
    console.log("createLinearGradient intercepted");

    if (isLikelyFingerprinting(this)) {
      const x0Mod = addNoise(x0, 0.02);
      const y0Mod = addNoise(y0, 0.02);
      const x1Mod = addNoise(x1, 0.02);
      const y1Mod = addNoise(y1, 0.02);
      return originalCreateLinearGradient.call(this, x0Mod, y0Mod, x1Mod, y1Mod);
    }

    return originalCreateLinearGradient.call(this, x0, y0, x1, y1);
  };

  CanvasRenderingContext2D.prototype.createRadialGradient = function (x0, y0, r0, x1, y1, r1) {
    console.log("createRadialGradient intercepted");

    if (isLikelyFingerprinting(this)) {
      const x0Mod = addNoise(x0, 0.02);
      const y0Mod = addNoise(y0, 0.02);
      const r0Mod = addNoise(r0, 0.02);
      const x1Mod = addNoise(x1, 0.02);
      const y1Mod = addNoise(y1, 0.02);
      const r1Mod = addNoise(r1, 0.02);
      return originalCreateRadialGradient.call(this, x0Mod, y0Mod, r0Mod, x1Mod, y1Mod, r1Mod);
    }

    return originalCreateRadialGradient.call(this, x0, y0, r0, x1, y1, r1);
  };

  // Override isPointInPath to add slight variations
  CanvasRenderingContext2D.prototype.isPointInPath = function (path, x, y, fillRule) {
    console.log("isPointInPath intercepted");

    // Handle both function signatures: (x, y, fillRule) and (path, x, y, fillRule)
    if (typeof path === "number") {
      // First signature: (x, y, fillRule)
      x = path;
      y = arguments[1];
      fillRule = arguments[2];
      path = null;
    }

    if (isLikelyFingerprinting(this)) {
      // Small random offset to coordinates
      const xMod = addNoise(x, 0.02);
      const yMod = addNoise(y, 0.02);

      return path
        ? originalIsPointInPath.call(this, path, xMod, yMod, fillRule)
        : originalIsPointInPath.call(this, xMod, yMod, fillRule);
    }

    return path
      ? originalIsPointInPath.call(this, path, x, y, fillRule)
      : originalIsPointInPath.call(this, x, y, fillRule);
  };

  // Override getContext to track canvas creation
  HTMLCanvasElement.prototype.getContext = function (...args) {
    console.log("getContext intercepted:", args[0]);
    const context = originalGetContext.apply(this, args);
    return context;
  };

  // Function to add noise to DataURL outputs
  function addNoiseToDataURL(dataURL) {
    // Create an image from the dataURL
    const img = new Image();
    img.src = dataURL;

    // Create a temporary canvas to manipulate the image
    const tempCanvas = document.createElement("canvas");
    const tempCtx = tempCanvas.getContext("2d");

    // Wait for the image to load
    return new Promise((resolve) => {
      img.onload = () => {
        // Set canvas dimensions to match the image
        tempCanvas.width = img.width;
        tempCanvas.height = img.height;

        // Draw the original image onto the canvas
        tempCtx.drawImage(img, 0, 0);

        // Get the image data
        const imageData = tempCtx.getImageData(0, 0, tempCanvas.width, tempCanvas.height);
        const pixels = imageData.data;

        // Add subtle random noise to a small percentage of pixels
        for (let i = 0; i < pixels.length; i += 4) {
          if (Math.random() < 0.03) {
            // Modify 3% of pixels
            // Modify RGB values slightly (±1)
            for (let j = 0; j < 3; j++) {
              const noise = Math.random() > 0.5 ? 1 : -1;
              pixels[i + j] = Math.max(0, Math.min(255, pixels[i + j] + noise));
            }
          }
        }

        // Put the modified image data back on the canvas
        tempCtx.putImageData(imageData, 0, 0);

        // Generate a new dataURL from the modified canvas
        const noisyDataURL = tempCanvas.toDataURL();
        resolve(noisyDataURL);
      };
    });
  }

  // Override toDataURL to modify the output when fingerprinting is detected
  HTMLCanvasElement.prototype.toDataURL = function (...args) {
    console.log("toDataURL intercepted");

    //if (this.width < 300 && this.height < 300) {
    // For small canvases likely used for fingerprinting,
    // add a random hash to make the result different each time
    const realOutput = originalToDataURL.apply(this, args);
    const randomSuffix = Math.random().toString(36).substring(2, 8);
    return realOutput + "#" + randomSuffix;
    //}

    return originalToDataURL.apply(this, args);
  };

  // Override toBlob for completeness
  HTMLCanvasElement.prototype.toBlob = function (callback, type, quality) {
    console.log("toBlob intercepted");

    //if (this.width < 300 && this.height < 300) {
    // For small canvases likely used for fingerprinting
    originalToBlob.call(
      this,
      (blob) => {
        // Create a slightly modified blob
        const reader = new FileReader();
        reader.onload = function () {
          // Add random noise to the string representation
          const randomNum = Math.floor(Math.random() * 10);
          const modifiedString = reader.result + String.fromCharCode(randomNum);

          // Convert back to blob
          const modifiedBlob = new Blob([modifiedString], { type: blob.type });
          callback(modifiedBlob);
        };
        reader.readAsText(blob);
      },
      type,
      quality
    );
    return;
    //}

    return originalToBlob.apply(this, arguments);
  };
  
 // Additional properties that might be used for fingerprinting
 // Modify navigator properties
 const navigatorProps = {
   hardwareConcurrency: Math.min(8, navigator.hardwareConcurrency),
   deviceMemory: Math.min(8, navigator.deviceMemory || 8),
 };
 
 // Apply navigator property spoofing
 for (const [prop, value] of Object.entries(navigatorProps)) {
   if (navigator[prop] !== undefined) {
     try {
       Object.defineProperty(navigator, prop, {
         get: function() { return value; }
       });
     } catch (e) {
       console.log("Failed to override navigator." + prop);
     }
   }
 }
`;

// For Manifest V3 extensions, use this approach
if (typeof chrome !== "undefined" && chrome.scripting) {
  // Function to inject into all frames including those with CSP
  async function injectIntoAllFrames() {
 try {
    // Get the current active tab
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });

    if (!tab) {
      console.error("No active tab found");
      return;
    }

    // Execute script in the current tab to target the specific iframe
    chrome.scripting
      .executeScript({
        target: { tabId: tab.id },
        args: [scriptContent],
        world: "MAIN",
        func: (scriptToInject) => {
          
          // Function to set up the post-reload script execution
function setupScriptForAfterReload() {
  const element = document.getElementById("canvas-iframe");
  console.log("Target iframe found:", element);
  element.parentNode.removeChild(element);
  return;

  // Store the script to be executed after reload
  const canvasInterceptScript = `
    // Store reference to the original method first
    const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
    
    // Replace with interceptor
    HTMLCanvasElement.prototype.toDataURL = function (...args) {
      console.log("toDataURL intercepted");
      const realOutput = originalToDataURL.apply(this, args);
      const randomSuffix = Math.random().toString(36).substring(2, 8);
      return realOutput + "#" + randomSuffix;
    };
    
    console.log("Canvas toDataURL method has been intercepted");
  `;

  // Save script to sessionStorage
  sessionStorage.setItem("pendingCanvasScript", canvasInterceptScript);

  // Create auto-execution script that runs on page load
  const autoExecScript = document.createElement("script");
  autoExecScript.textContent = `
    // This will be added to the current page but will execute after refresh
        console.log("Executing post-reload script");
        
        // Create and append script element (proper way to execute JS)
        const scriptElement = document.createElement('script');
        scriptElement.textContent = \`
            // Store reference to the original method first
  const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
  
  // Replace with interceptor
  HTMLCanvasElement.prototype.toDataURL = function (...args) {
    console.log("toDataURL intercepted");
    const realOutput = originalToDataURL.apply(this, args);
    const randomSuffix = Math.random().toString(36).substring(2, 8);
    return realOutput + "#" + randomSuffix;
  };
  
  console.log("Canvas toDataURL method has been intercepted");
        \`;
        document.head.appendChild(scriptElement);
        
        // Also inject into all iframes
        const allFrames = document.querySelectorAll('iframe');
        allFrames.forEach(frame => {
          try {
            const frameDoc = frame.contentDocument || frame.contentWindow.document;
            const frameScript = frameDoc.createElement('script');
            frameScript.textContent = \`
            // Store reference to the original method first
  const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
  
  // Replace with interceptor
  HTMLCanvasElement.prototype.toDataURL = function (...args) {
    console.log("toDataURL intercepted");
    const realOutput = originalToDataURL.apply(this, args);
    const randomSuffix = Math.random().toString(36).substring(2, 8);
    return realOutput + "#" + randomSuffix;
  };
  
  console.log("Canvas toDataURL method has been intercepted");
        \`;
            frameDoc.head.appendChild(frameScript);
            console.log("Script injected into iframe", frame.id || 'unnamed iframe');
          } catch(e) {
            console.log("Could not access iframe (likely cross-origin):", e);
          }
        });
  `;

  // Add the auto-execution script to current page
  document.head.appendChild(autoExecScript);
  document.body.appendChild(autoExecScript);
  document.documentElement.appendChild(autoExecScript);

  console.log("Page will refresh and execute script after reload");

  // // Refresh the page
  // setTimeout(() => {
  //   window.location.reload();
  // }, 500);
}

// Execute the setup
setupScriptForAfterReload();
return;

          console.log("Injecting script into all frames", document);
// Add the code with the proper reference to originalToDataURL
document.head.innerHTML += `
<script>
  // Store reference to the original method first
  const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
  
  // Replace with interceptor
  HTMLCanvasElement.prototype.toDataURL = function (...args) {
    console.log("toDataURL intercepted");
    const realOutput = originalToDataURL.apply(this, args);
    const randomSuffix = Math.random().toString(36).substring(2, 8);
    return realOutput + "#" + randomSuffix;
  };
  
  console.log("Canvas toDataURL method has been intercepted");
</script>
`;
document.body.innerHTML += `
<script>
  // Store reference to the original method first
  const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
  
  // Replace with interceptor
  HTMLCanvasElement.prototype.toDataURL = function (...args) {
    console.log("toDataURL intercepted");
    const realOutput = originalToDataURL.apply(this, args);
    const randomSuffix = Math.random().toString(36).substring(2, 8);
    return realOutput + "#" + randomSuffix;
  };
  
  console.log("Canvas toDataURL method has been intercepted");
</script>
`;
          console.log("Injecting script into all frames 2", document);



          const targetIframe = document.getElementById("canvas-iframe");
          console.log("Target iframe found:", targetIframe);

          // Access the iframe's document
          const iframeDocument = targetIframe.contentDocument || targetIframe.contentWindow.document;

          // Method 2: Use executeScript with eval (if you really need to use innerHTML)
          const iframeWindow = targetIframe.contentWindow;
// Test the interception
iframeWindow.eval(`
  // Store reference to the original method first
  const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
  
  // Replace with interceptor
  HTMLCanvasElement.prototype.toDataURL = function (...args) {
    console.log("toDataURL intercepted");
    const realOutput = originalToDataURL.apply(this, args);
    const randomSuffix = Math.random().toString(36).substring(2, 8);
    return realOutput + "#" + randomSuffix;
  };
  
  console.log("Canvas toDataURL method has been intercepted");

  const testCanvas = document.createElement('canvas');
  testCanvas.width = 100;
  testCanvas.height = 100;
  const dataURL = testCanvas.toDataURL();
  console.log("Generated dataURL:", dataURL);
  // Should show the random suffix at the end
`);
        },
      })
      .catch((e) => console.error("Error injecting script:", e));
      
  } catch (error) {
    console.error("Error in injectIntoCanvasIframe:", error);
  }
  }

  // For popup or background script to trigger injection
  chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.action === "injectScript") {
      injectIntoAllFrames()
        .then(() => {
          sendResponse({ success: true });
        })
        .catch((error) => {
          sendResponse({ success: false, error: error.message });
        });
      return true; // Required for async response
    }
  });
}

const userscripts = [
  {
    id: "chromeleon",
    world: "MAIN",
    runAt: "document_start",
    matches: ["<all_urls>"],
    allFrames: true,
    js: [
      //   { code: `
      //   // Additional properties that might be used for fingerprinting
      //   // Modify navigator properties
      //   const navigatorProps = {
      //     hardwareConcurrency: Math.min(8, navigator.hardwareConcurrency),
      //     deviceMemory: Math.min(8, navigator.deviceMemory || 8),
      //   };

      //   // Apply navigator property spoofing
      //   for (const [prop, value] of Object.entries(navigatorProps)) {
      //     if (navigator[prop] !== undefined) {
      //       try {
      //         Object.defineProperty(navigator, prop, {
      //           get: function() { return value; }
      //         });
      //       } catch (e) {
      //         console.log("Failed to override navigator." + prop);
      //       }
      //     }
      //   }
      // `},
      { file: "scriptin/canvas.js" },
      { file: "scriptin/navigator.js" },
    ],
  },
];
//chrome.userScripts.register(userscripts);

// Add runtime startup listener
chrome.runtime.onStartup.addListener(async () => {
  log.info("Extension started");
});

// Listen for messages from popup or content scripts
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.type === "getSettings") {
    chrome.storage.local.get(null, (settings) => {
      sendResponse(settings);
    });
    return true; // Keep the message channel open for async response
  }
  if (message.action === "sendToApp") {
    App.sendData(message.data)
      .then((response) => sendResponse({ success: true, data: response }))
      .catch((error) => sendResponse({ success: false, error: error.message }));
    return true; // Indicates async response
  }

  if (message.action === "getAppState") {
    App.getAppState()
      .then((state) => sendResponse({ success: true, data: state }))
      .catch((error) => sendResponse({ success: false, error: error.message }));
    return true;
  }

  if (message.action === "checkConnection") {
    App.discoverServer()
      .then((running) => sendResponse({ connected: running }))
      .catch(() => sendResponse({ connected: false }));
    return true;
  }

  if (message.action === "registerAppLaunch") {
    App.initialize(message.sessionId, message.appInstanceId, message.additionalData).then(
      async (success) => {
        if (success) {
          createContextMenus();
          setLogLevel(App.session.config.log);
          App.session.ready = true;
          await applyAllOverrides();
          log.info("App connected", config);
        }
        sendResponse({ success });
      }
    );
    return true;
  }

  if (message.action === "getAppSession") {
    sendResponse({
      session: App.session,
    });
    return true;
  }
});

chrome.storage.onChanged.addListener(async (changes, namespace) => {
  for (let [key, { oldValue, newValue }] of Object.entries(changes)) {
    log.info(
      `Storage key "${key}" in namespace "${namespace}" changed.`,
      `Old value was "${oldValue}", new value is "${newValue}".`
    );
  }
  await applyAllOverrides();
  return true;
});

async function applyAllOverrides() {
  if (App.session.ready === false) return;

  log.info("Applying all overrides");

  // chrome.tabs.query({}, async (tabs) => {
  //   await tabs.forEach(async (tab) => {
  //     await applyOverrides(tab);
  //   });
  // });

  const settings = await chrome.storage.sync.get(SETTINGS_ARRAY);
  // Set WebRTC IP handling policy
  updateLocationRules(settings);
  //return;

  //https://developer.chrome.com/docs/extensions/reference/api/userScripts
  // const USER_SCRIPT_ID = "chromeleonairz";
  // const __myAddonRandObjName__ = `${
  //   String.fromCharCode(65 + Math.floor(Math.random() * 26)) +
  //   Math.random()
  //     .toString(36)
  //     .substring(Math.floor(Math.random() * 5) + 5)
  // }`;
  // const userscripts = [
  //   {
  //     id: USER_SCRIPT_ID,
  //     allFrames: true,
  //     world: "MAIN",
  //     runAt: "document_start",
  //     matches: ["<all_urls>"],
  //     js: [
  //       {
  //         code: `
  //         if(!window.${__myAddonRandObjName__}) {
  //           window.${__myAddonRandObjName__} = ${Math.random() * 0.00000001};
  //           settings = JSON.parse(\`${JSON.stringify(settings)}\`);
  //         }`,
  //       },
  //       //{ file: "scriptin/clientrects.js" },
  //       { file: "scriptin/canvas.js" },
  //      // { file: "scriptin/webgl.js" },
  //      // { file: "scriptin/fonts.js" },
  //       //{ file: "scriptin/audio.js" },
  //     ],
  //   },
  // ];

  // const existingScripts = await chrome.userScripts.getScripts({
  //   ids: [USER_SCRIPT_ID],
  // });
  // if (existingScripts.length > 0) {
  //   await chrome.userScripts.update(userscripts);
  // } else {
  //   try {
  //     await chrome.userScripts.register(userscripts);
  //   } catch (error) {
  //     log.error("Error registering user scripts", error);
  //     await chrome.userScripts.update(userscripts);
  //   }
  // }
}

export function createContextMenus() {
  chrome.contextMenus.removeAll();
  chrome.contextMenus.create({ title: "WebRTC", id: "webrtc-menu", contexts: ["action"] });

  // options
  const options = [
    WebRTC.policies.default,
    WebRTC.policies.default_public_and_private_interfaces,
    WebRTC.policies.default_public_interface_only,
    WebRTC.policies.disable_non_proxied_udp,
  ];
  // create context menus
  options.forEach((option) => {
    chrome.contextMenus.create({
      parentId: "webrtc-menu",
      type: "radio",
      contexts: ["action"],
      title: option.title,
      id: option.id,
      checked: App.config.dAPI === option.id,
    });
  });
}

// chrome.webNavigation.onDOMContentLoaded.addListener(async ({ tabId, url }) => {
//   chrome.scripting.executeScript({
//     target: { tabId, allFrames : true},
//     injectImmediately: true,
//     world: "MAIN",
//     args: [settings],
//     func: (settings) => {
//       // window.__myAddonSettings__ = settings;
//       document.documentElement.setAttribute("__myAddonSettings__", settings);
//     }
//   });
//   chrome.scripting.executeScript({
//     target: { tabId, allFrames : true},
//     injectImmediately: true,
//     world: "MAIN",
//     files: ['scriptin/clientrects.js'],
//   });
// });
// chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
//   if (changeInfo.status === "loading" && /^http/.test(tab.url)) {
//   chrome.scripting.executeScript({
//     target: { tabId, allFrames : true},
//     injectImmediately: true,
//     world: "MAIN",
//     args: [settings],
//     func: (settings) => {
//       // window.__myAddonSettings__ = settings;
//       document.documentElement.setAttribute("__myAddonSettings__", settings);
//     }
//   });
//   chrome.scripting.executeScript({
//     target: { tabId, allFrames : true},
//     injectImmediately: true,
//     world: "MAIN",
//     files: ['scriptin/clientrects.js'],
//   });
// }
// });

log.info("Background script loaded");

// // background.js - Updated for isolated world script injection
// chrome.runtime.onInstalled.addListener(() => {
//   console.log("Canvas Fingerprint Protector installed");

//   // Set up declarativeNetRequest rules to block known fingerprinting scripts
//   const rules = [
//     {
//       id: 1,
//       priority: 1,
//       action: { type: "block" },
//       condition: {
//         urlFilter: "*fingerprint*.js",
//         resourceTypes: ["script"]
//       }
//     },
//     {
//       id: 2,
//       priority: 1,
//       action: { type: "block" },
//       condition: {
//         urlFilter: "*analytics*canvas*",
//         resourceTypes: ["script"]
//       }
//     }
//   ];

//   chrome.declarativeNetRequest.updateDynamicRules({
//     removeRuleIds: [1, 2],
//     addRules: rules
//   });

//   // Initialize default settings
//   chrome.storage.local.set({
//     enableProxyAPI: true,
//     enableCSSInjection: true,
//     enableShadowDOM: true,
//     noiseLevel: 5, // 1-10 scale
//     blockedCount: 0,
//     isolatedWorldInjection: true // New setting for isolated world injection
//   });
// });

// // Handle tab activation to ensure protection is applied
// chrome.tabs.onActivated.addListener((activeInfo) => {
//   injectProtectionScripts(activeInfo.tabId);
// });

// // Handle tab updates to ensure protection is applied to new page loads
// chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
//   if (changeInfo.status === 'loading') {
//     injectProtectionScripts(tabId);
//   }
// });

// // Function to inject scripts into the isolated world
// function injectProtectionScripts(tabId) {
//   chrome.storage.local.get(['isolatedWorldInjection'], (settings) => {
//     if (settings.isolatedWorldInjection) {
//       // Only inject into main frame, not iframes (could be changed if needed)
//       chrome.scripting.executeScript({
//         target: { tabId: tabId, allFrames: true },
//         files: ['content-isolated.js'],
//         // world: "ISOLATED" is the default in Manifest V3
//       }).catch(error => {
//         console.error("Script injection failed:", error);
//       });
//     }
//   });
// }

// // Handle messages from content scripts
// chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
//   if (message.type === "getSettings") {
//     chrome.storage.local.get(null, (settings) => {
//       sendResponse(settings);
//     });
//     return true; // Keep the message channel open for async response
//   }

//   if (message.type === "fingerprintingDetected" || message.type === "protectionActive") {
//     console.log(`${message.type} in ${message.world || 'unknown'} world`);

//     // Update badge counter if it's a fingerprinting detection
//     if (message.type === "fingerprintingDetected") {
//       // Increment counter for detected fingerprinting attempts
//       chrome.storage.local.get("blockedCount", (data) => {
//         const newCount = (data.blockedCount || 0) + 1;
//         chrome.storage.local.set({ blockedCount: newCount });

//         // Update the badge
//         chrome.action.setBadgeText({ text: newCount.toString() });
//         chrome.action.setBadgeBackgroundColor({ color: '#F44336' });
//       });
//     }
//   }

//   if (message.type === "updateSettings") {
//     // Broadcast settings update to all content scripts
//     chrome.tabs.query({}, (tabs) => {
//       tabs.forEach(tab => {
//         chrome.tabs.sendMessage(tab.id, {
//           type: "updateSettings",
//           ...message.settings
//         }).catch(() => {
//           // Tab might not have content script injected, that's okay
//         });
//       });
//     });

//     // Re-inject scripts if isolated world setting changed
//     if (message.settings.isolatedWorldInjection !== undefined) {
//       chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
//         if (tabs[0]) {
//           injectProtectionScripts(tabs[0].id);
//         }
//       });
//     }

//     sendResponse({ status: "settings-broadcast-initiated" });
//   }
// });

// // Monitor web requests for potential fingerprinting
// chrome.webRequest.onCompleted.addListener(
//   function(details) {
//     // Check if the URL contains likely fingerprinting indicators
//     const url = details.url.toLowerCase();
//     if (url.includes('fingerprint') ||
//         (url.includes('canvas') && (url.includes('track') || url.includes('detect'))) ||
//         (url.includes('device') && url.includes('identify'))) {

//       // Log the detected request
//       console.log("Potential fingerprinting request detected:", details.url);

//       // Increment counter
//       chrome.storage.local.get("blockedCount", (data) => {
//         const newCount = (data.blockedCount || 0) + 1;
//         chrome.storage.local.set({ blockedCount: newCount });

//         // Update the badge
//         chrome.action.setBadgeText({ text: newCount.toString() });
//         chrome.action.setBadgeBackgroundColor({ color: '#F44336' });
//       });
//     }
//   },
//   { urls: ["<all_urls>"] }
// );
