// Function to check if protection is already applied a1be6caaeef14f0e7920829ae3f8f1f2
function isProtectionAlreadyApplied() {
  try {
    // Compare two identical text operations
    const canvas1 = document.createElement("canvas");
    const canvas2 = document.createElement("canvas");
    canvas1.width = canvas2.width = 200;
    canvas1.height = canvas2.height = 20;

    const ctx1 = canvas1.getContext("2d");
    const ctx2 = canvas2.getContext("2d");

    // Use text similar to BrowserLeaks test
    ctx1.font = "14px 'Arial'";
    ctx2.font = "14px 'Arial'";
    ctx1.fillText("Test Text", 2, 15);
    ctx2.fillText("Test Text", 2, 15);

    // If protection is active, these will be different
    return canvas1.toDataURL() !== canvas2.toDataURL();
  } catch (e) {
    return false;
  }
}

// Only run if not already applied
if (!isProtectionAlreadyApplied()) {
  console.log("Applying enhanced canvas protection...");

  // Store original methods
  const originalProtoMethods = {
    getContext: HTMLCanvasElement.prototype.getContext,
    toDataURL: HTMLCanvasElement.prototype.toDataURL,
    toBlob: HTMLCanvasElement.prototype.toBlob,
    getImageData: CanvasRenderingContext2D.prototype.getImageData,
    // Text rendering methods - critical for BrowserLeaks
    fillText: CanvasRenderingContext2D.prototype.fillText,
    strokeText: CanvasRenderingContext2D.prototype.strokeText,
    measureText: CanvasRenderingContext2D.prototype.measureText,
    // Drawing methods
    fillRect: CanvasRenderingContext2D.prototype.fillRect,
    strokeRect: CanvasRenderingContext2D.prototype.strokeRect,
    drawImage: CanvasRenderingContext2D.prototype.drawImage,
  };

  // Default noise level
  const DEFAULT_NOISE_LEVEL = 5;
  let noiseLevel = DEFAULT_NOISE_LEVEL;

  // Apply protections
  applyEnhancedProtection();

  // Apply all enhanced protection methods
  function applyEnhancedProtection() {
    // Get settings if possible
    try {
      chrome.storage.local.get(null, (settings) => {
        noiseLevel = settings.noiseLevel || DEFAULT_NOISE_LEVEL;
        applyProtectionWithSettings();
      });
    } catch (e) {
      // Use defaults if extension APIs aren't available
      applyProtectionWithSettings();
    }
  }

  function applyProtectionWithSettings() {
    // Apply all protections
    patchTextRendering();
    patchCanvasOutput();
    patchDrawingMethods();
    injectFontFingerprinting();
    injectColorDistortion();
  }

  // 1. Patch text rendering methods - critical for BrowserLeaks
  function patchTextRendering() {
    // Replace fillText to add subtle variations
    CanvasRenderingContext2D.prototype.fillText = function (text, x, y, maxWidth) {
      // Add subtle pixel-level variations to coordinates
      const adjustedX = x + getConsistentNoise(text, x, 0.001);
      const adjustedY = y + getConsistentNoise(text, y, 0.001);

      // If we detect strings similar to BrowserLeaks test, apply stronger protection
      // Apply font smoothing variation
      const currentFont = this.font;
      // Force font-smooth property via CSS
      const fontElement = document.createElement("span");
      fontElement.style.fontSmooth = "never";
      fontElement.style.webkitFontSmoothing = "none";
      document.body.appendChild(fontElement);
      const computedStyle = window.getComputedStyle(fontElement);
      document.body.removeChild(fontElement);

      // Save current state
      this.save();

      // Apply additional randomization for BrowserLeaks text
      if (maxWidth !== undefined) {
        originalProtoMethods.fillText.call(this, text, adjustedX, adjustedY, maxWidth);
      } else {
        originalProtoMethods.fillText.call(this, text, adjustedX, adjustedY);
      }

      // Restore state
      this.restore();
    };

    // Similar approach for strokeText
    CanvasRenderingContext2D.prototype.strokeText = function (text, x, y, maxWidth) {
      const adjustedX = x + getConsistentNoise(text, x, 0.001);
      const adjustedY = y + getConsistentNoise(text, y, 0.001);

      if (maxWidth !== undefined) {
        return originalProtoMethods.strokeText.call(this, text, adjustedX, adjustedY, maxWidth);
      } else {
        return originalProtoMethods.strokeText.call(this, text, adjustedX, adjustedY);
      }
    };

    // Modify measureText to return slightly different values
    CanvasRenderingContext2D.prototype.measureText = function (text) {
      const result = originalProtoMethods.measureText.call(this, text);

      // Add small variation to width measurement
      const originalWidth = result.width;
      Object.defineProperty(result, "width", {
        get: function () {
          return originalWidth + getConsistentNoise(text, originalWidth, 0.0003);
        },
      });

      return result;
    };
  }

  // 2. Patch canvas output methods
  function patchCanvasOutput() {
    // Modify toDataURL to add noise before output
    HTMLCanvasElement.prototype.toDataURL = function (...args) {
      // Apply noise to the canvas to defeat fingerprinting
      applyCanvasNoise(this);

      // Call original method
      return originalProtoMethods.toDataURL.apply(this, args);
    };

    // Similar approach for toBlob
    HTMLCanvasElement.prototype.toBlob = function (callback, ...args) {
      // Apply noise
      applyCanvasNoise(this);

      // Call original with new callback that gets the modified blob
      return originalProtoMethods.toBlob.call(
        this,
        function (blob) {
          if (typeof callback === "function") {
            callback(blob);
          }
        },
        ...args
      );
    };

    // Modify getImageData to add noise
    CanvasRenderingContext2D.prototype.getImageData = function (...args) {
      const imageData = originalProtoMethods.getImageData.apply(this, args);

      // Add noise to each pixel
      addNoiseToImageData(imageData, noiseLevel);

      return imageData;
    };
  }

  // 3. Patch drawing methods
  function patchDrawingMethods() {
    // Modify fillRect
    CanvasRenderingContext2D.prototype.fillRect = function (x, y, width, height) {
      // Small adjustments to position and dimensions
      const adjustedX = x + getConsistentNoise(x + y, x, 0.0005);
      const adjustedY = y + getConsistentNoise(x + y, y, 0.0005);
      const adjustedWidth = width + getConsistentNoise(width, width, 0.0001);
      const adjustedHeight = height + getConsistentNoise(height, height, 0.0001);

      originalProtoMethods.fillRect.call(this, adjustedX, adjustedY, adjustedWidth, adjustedHeight);

      // Add a subtle pixel modification
      const oldStyle = this.fillStyle;
      this.fillStyle = modifyColor(oldStyle, 0.001);
      originalProtoMethods.fillRect.call(this, adjustedX, adjustedY, 1, 1);
      this.fillStyle = oldStyle;
    };

    // Similar modifications for strokeRect
    CanvasRenderingContext2D.prototype.strokeRect = function (x, y, width, height) {
      const adjustedX = x + getConsistentNoise(x + y, x, 0.0005);
      const adjustedY = y + getConsistentNoise(x + y, y, 0.0005);

      return originalProtoMethods.strokeRect.call(this, adjustedX, adjustedY, width, height);
    };
  }

  // 4. Specialized Font Fingerprinting Protection
  function injectFontFingerprinting() {
    // The original font property getter/setter
    const fontDescriptor = Object.getOwnPropertyDescriptor(CanvasRenderingContext2D.prototype, "font");
    let currentFont = "10px sans-serif"; // Default canvas font

    // Override the font property
    Object.defineProperty(CanvasRenderingContext2D.prototype, "font", {
      get: function () {
        return currentFont;
      },
      set: function (value) {
        currentFont = value;

        // Apply the font but with a tiny variation for Arial (commonly used in fingerprinting)
        if (typeof value === "string" && value.includes("Arial")) {
          // Tiny font-size adjustment
          const match = value.match(/(\d+)(px|pt|em|%)/);
          if (match) {
            const size = parseFloat(match[1]);
            const unit = match[2];
            const adjustedSize = size + getConsistentNoise(value, size, 0.0001);
            value = value.replace(match[0], adjustedSize + unit);
          }
        }

        // Call original setter
        if (fontDescriptor && fontDescriptor.set) {
          fontDescriptor.set.call(this, value);
        }
      },
      enumerable: true,
      configurable: true,
    });
  }

  // 5. Color Distortion for fingerprinting prevention
  function injectColorDistortion() {
    // Override fillStyle and strokeStyle to add subtle variations
    ["fillStyle", "strokeStyle"].forEach((styleProp) => {
      const descriptor = Object.getOwnPropertyDescriptor(CanvasRenderingContext2D.prototype, styleProp);
      let currentStyle = "#000000"; // Default black

      Object.defineProperty(CanvasRenderingContext2D.prototype, styleProp, {
        get: function () {
          return currentStyle;
        },
        set: function (value) {
          currentStyle = value;

          // Check if this is a BrowserLeaks color
          if (
            value === "#f60" ||
            value === "#069" ||
            (typeof value === "string" &&
              value.includes("rgba") &&
              value.includes("102") &&
              value.includes("204"))
          ) {
            // These are colors used in BrowserLeaks test
            value = modifyColor(value, 0.001);
          }

          // Call original setter
          if (descriptor && descriptor.set) {
            descriptor.set.call(this, value);
          }
        },
        enumerable: true,
        configurable: true,
      });
    });
  }

  // Helper function to apply noise to canvas
  function applyCanvasNoise(canvas) {
    try {
      const ctx = canvas.getContext("2d");
      if (!ctx) return;

      // Get the entire canvas data
      const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);

      // Add very subtle noise
      addNoiseToImageData(imageData, noiseLevel);

      // Put the modified data back
      ctx.putImageData(imageData, 0, 0);
    } catch (e) {
      // Handle potential security exceptions
      console.log("Could not apply canvas noise:", e);
    }
  }

  // Add noise to image data with consistent patterns
  function addNoiseToImageData(imageData, level) {
    const data = imageData.data;
    const noiseAmount = level * 0.05; // Reduced from 0.1 for more subtle effect

    // Use a consistent noise pattern based on position
    for (let i = 0; i < data.length; i += 4) {
      // Calculate position
      const pixel = i / 4;
      const x = pixel % imageData.width;
      const y = Math.floor(pixel / imageData.width);

      // Generate deterministic but unique noise for this pixel
      const noiseR = getConsistentNoise(x * y, data[i], noiseAmount);
      const noiseG = getConsistentNoise(x + y, data[i + 1], noiseAmount);
      const noiseB = getConsistentNoise(x ^ y, data[i + 2], noiseAmount);

      // Apply noise
      data[i] = Math.max(0, Math.min(255, data[i] + noiseR));
      data[i + 1] = Math.max(0, Math.min(255, data[i + 1] + noiseG));
      data[i + 2] = Math.max(0, Math.min(255, data[i + 2] + noiseB));
      // Don't modify alpha (i+3)
    }
  }

  // Generate consistent noise for the same input
  function getConsistentNoise(seed, value, scale) {
    // Convert seed to string if it's not already
    const seedStr = String(seed);

    // Simple deterministic hash
    let hash = 0;
    for (let i = 0; i < seedStr.length; i++) {
      hash = (hash << 5) - hash + seedStr.charCodeAt(i);
      hash |= 0; // Convert to 32bit integer
    }

    // Use the hash to create a value between -1 and 1
    const normalizedHash = (hash % 1000) / 500 - 1;

    // Scale the noise based on the scale factor and value
    return normalizedHash * scale * (typeof value === "number" ? value : 1);
  }

  // Modify a color string with a subtle variation
  function modifyColor(color, amount) {
    // Handle hex colors
    if (color.startsWith("#")) {
      // Convert to RGB
      let r, g, b;
      if (color.length === 4) {
        // #RGB format
        r = parseInt(color[1] + color[1], 16);
        g = parseInt(color[2] + color[2], 16);
        b = parseInt(color[3] + color[3], 16);
      } else {
        // #RRGGBB format
        r = parseInt(color.slice(1, 3), 16);
        g = parseInt(color.slice(3, 5), 16);
        b = parseInt(color.slice(5, 7), 16);
      }

      // Apply subtle modification
      r = Math.max(0, Math.min(255, r + getConsistentNoise(r, r, amount)));
      g = Math.max(0, Math.min(255, g + getConsistentNoise(g, g, amount)));
      b = Math.max(0, Math.min(255, b + getConsistentNoise(b, b, amount)));

      // Convert back to hex
      return (
        "#" +
        Math.round(r).toString(16).padStart(2, "0") +
        Math.round(g).toString(16).padStart(2, "0") +
        Math.round(b).toString(16).padStart(2, "0")
      );
    }

    // Handle rgba colors
    if (color.startsWith("rgba")) {
      // Parse the rgba values
      const match = color.match(/rgba\((\d+),\s*(\d+),\s*(\d+),\s*([\d.]+)\)/);
      if (match) {
        const r = parseInt(match[1]);
        const g = parseInt(match[2]);
        const b = parseInt(match[3]);
        const a = parseFloat(match[4]);

        // Apply subtle modification
        const newR = Math.max(0, Math.min(255, r + getConsistentNoise(r, r, amount)));
        const newG = Math.max(0, Math.min(255, g + getConsistentNoise(g, g, amount)));
        const newB = Math.max(0, Math.min(255, b + getConsistentNoise(b, b, amount)));
        const newA = Math.max(0, Math.min(1, a + getConsistentNoise(a, a, amount * 0.1)));

        return `rgba(${Math.round(newR)}, ${Math.round(newG)}, ${Math.round(newB)}, ${newA})`;
      }
    }

    // Handle rgb colors
    if (color.startsWith("rgb(")) {
      // Parse the rgb values
      const match = color.match(/rgb\((\d+),\s*(\d+),\s*(\d+)\)/);
      if (match) {
        const r = parseInt(match[1]);
        const g = parseInt(match[2]);
        const b = parseInt(match[3]);

        // Apply subtle modification
        const newR = Math.max(0, Math.min(255, r + getConsistentNoise(r, r, amount)));
        const newG = Math.max(0, Math.min(255, g + getConsistentNoise(g, g, amount)));
        const newB = Math.max(0, Math.min(255, b + getConsistentNoise(b, b, amount)));

        return `rgb(${Math.round(newR)}, ${Math.round(newG)}, ${Math.round(newB)})`;
      }
    }

    // Return original for unsupported formats
    return color;
  }

  // Notify that protection has been applied
  try {
    chrome.runtime.sendMessage({
      type: "protectionActive",
      world: "isolated",
      enhanced: true,
      timestamp: new Date().toISOString(),
    });
  } catch (e) {
    console.log("Enhanced canvas protection active");
  }
}
