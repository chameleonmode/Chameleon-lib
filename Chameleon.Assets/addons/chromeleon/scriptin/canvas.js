// Enhanced Canvas Fingerprinting Protection
// https://privacycheck.sec.lrz.de/active/fp_c/fp_canvas.html
// https://www.browserleaks.com/canvas
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

// Version: 2.0
const getImageData = CanvasRenderingContext2D.prototype.getImageData;
const toBlob = HTMLCanvasElement.prototype.toBlob;
const toDataURL = HTMLCanvasElement.prototype.toDataURL;
//
const noisify = function (canvas, context) {
  if (context) {
    const shift = {
      r: Math.floor(Math.random() * 10 - 5 * 3.8),
      g: Math.floor(Math.random() * 10 - 5 * 3.8),
      b: Math.floor(Math.random() * 10 - 5 * 3.8),
    };
    //
    const width = canvas.width;
    const height = canvas.height;
    //
    if (width && height) {
      const imageData = getImageData.apply(context, [0, 0, width, height]);
      //
      for (let i = 0; i < height; i++) {
        for (let j = 0; j < width; j++) {
          const n = i * (width * 4) + j * 4;
          imageData.data[n + 0] = imageData.data[n + 0] + shift.r * 10;
          imageData.data[n + 1] = imageData.data[n + 1] + shift.g * 10;
          imageData.data[n + 2] = imageData.data[n + 2] + shift.b * 10;
        }
      }
      context.putImageData(imageData, 0, 0);
    }
  }
};

CanvasRenderingContext2D.prototype.getImageData = function (x, y, w, h) {
  noisify(window.canvas, window);
  return getImageData.apply(window, [x, y, w, h]);
};

HTMLCanvasElement.prototype.toBlob = function (callback, type, quality) {
  noisify(window, self.getContext("2d", { willReadFrequently: true }));
  return toBlob.apply(window, [callback, type, quality]);
};

HTMLCanvasElement.prototype.toDataURL = function (type, quality) {
  noisify(window, self.getContext("2d", { willReadFrequently: true }));
  return toDataURL.apply(window, [type, quality]);
};

(function () {
  // Store original methods
  const originalMethods = {
    getContext: HTMLCanvasElement.prototype.getContext,
    toDataURL: HTMLCanvasElement.prototype.toDataURL,
    getImageData: CanvasRenderingContext2D.prototype.getImageData,
    fillText: CanvasRenderingContext2D.prototype.fillText,
    fillRect: CanvasRenderingContext2D.prototype.fillRect,
  };

  // Helper function to add noise to image data
  function addNoiseToImageData(imageData, noiseLevel = 1) {
    // Create a copy of the data to avoid modifying the original
    const data = new Uint8ClampedArray(imageData.data);

    // Add slight random noise to pixel values
    // Only modify a small percentage of pixels to maintain visual similarity
    for (let i = 0; i < data.length; i += 4) {
      // Only modify if random value is less than 0.1 (10% of pixels)
      if (Math.random() < 0.1) {
        // Add small random offset to RGB values
        data[i] = Math.max(0, Math.min(255, data[i] + (Math.random() * 2 - 1) * noiseLevel));
        data[i + 1] = Math.max(0, Math.min(255, data[i + 1] + (Math.random() * 2 - 1) * noiseLevel));
        data[i + 2] = Math.max(0, Math.min(255, data[i + 2] + (Math.random() * 2 - 1) * noiseLevel));
        // Don't modify alpha channel (i+3) to keep transparency intact
      }
    }

    return new ImageData(data, imageData.width, imageData.height);
  }

  // Override toDataURL to add slight randomization
  HTMLCanvasElement.prototype.toDataURL = function (type, quality) {
    // Get the original image data
    const ctx = this.getContext("2d");
    const imageData = ctx.getImageData(0, 0, this.width, this.height);

    // Modify the image data
    const modifiedImageData = addNoiseToImageData(imageData);

    // Apply the modified data back to the canvas
    ctx.putImageData(modifiedImageData, 0, 0);

    // Call the original method
    return originalMethods.toDataURL.apply(this, arguments);
  };

  // Override getImageData to add slight randomization
  CanvasRenderingContext2D.prototype.getImageData = function (x, y, width, height) {
    // Call the original method
    const imageData = originalMethods.getImageData.call(this, x, y, width, height);

    return addNoiseToImageData(imageData);
  };

  // Add slight offset to text positioning
  CanvasRenderingContext2D.prototype.fillText = function (text, x, y, maxWidth) {
    // Add a small random offset
    const offsetX = x + (Math.random() * 0.2 - 0.1);
    const offsetY = y + (Math.random() * 0.2 - 0.1);

    return originalMethods.fillText.call(this, text, offsetX, offsetY, maxWidth);
  };

  // Modify rectangle drawing slightly
  CanvasRenderingContext2D.prototype.fillRect = function (x, y, width, height) {
    // Very slight modifications to dimensions
    const newX = x + (Math.random() * 0.2 - 0.1);
    const newY = y + (Math.random() * 0.2 - 0.1);
    const newWidth = width + (Math.random() * 0.4 - 0.2);
    const newHeight = height + (Math.random() * 0.4 - 0.2);

    return originalMethods.fillRect.call(this, newX, newY, newWidth, newHeight);
  };

  // Add property to indicate the canvas is being protected
  Object.defineProperty(HTMLCanvasElement.prototype, "_protected", {
    value: true,
    enumerable: false,
  });

  console.log("[Canvas Fingerprint Protection] Initialized");
})();
// Version: 2.0