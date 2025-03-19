export default async function (opts) {
  return function (params) {
    const { noise, w, h } = params || {};

    // Store original methods
    const originalMethods = {
      getContext: HTMLCanvasElement.prototype.getContext,
      toDataURL: HTMLCanvasElement.prototype.toDataURL,
      getImageData: CanvasRenderingContext2D.prototype.getImageData,
      fillText: CanvasRenderingContext2D.prototype.fillText,
      fillRect: CanvasRenderingContext2D.prototype.fillRect,
    };

    HTMLCanvasElement.prototype.toDataURL = new Proxy(originalMethods.toDataURL, {
      apply(target, thisArg, argumentsList) {
        const contexts = ["2d", "webgl", "webgl2", "bitmaprenderer"];
        // Check if the contextId is in the list of contexts
        const getContext = (contextId) => {
          try {
            // Check if the contextId is valid
            if (contextId >= contexts.length) return null;
            return thisArg.getContext(contexts[contextId]);
          } catch (e) {
            console.warn("Error getting context", e);
            return getContext(contextId + 1);
          }
        };

        const ctx = getContext(0);
        if (!ctx) {
          console.warn("No valid context found");
          return Reflect.apply(target, thisArg, argumentsList);
        }
        // Save the original canvas state
        ctx.save();

        // Define a constant position based on canvas size
        const x = Math.min(thisArg.width - 1, Math.max(0, Math.floor(thisArg.width / 3)));
        const y = Math.min(thisArg.height - 1, Math.max(0, Math.floor(thisArg.height / 3)));

        // Get original pixel
        const pixelData = ctx.getImageData(x, y, 1, 1);
        const originalPixel = new Uint8ClampedArray(pixelData.data);
        const modifiedPixel = new Uint8ClampedArray(originalPixel);

        // Simple hash based on canvas dimensions
        const dimensionHash = (thisArg.width * 13 + thisArg.height * 17) % 256;

        // Apply a different mathematical operation for each level
        switch (noise) {
          case "micro":
            // Replace lowest bit only
            modifiedPixel[1] = (originalPixel[1] & ~0x01) | (dimensionHash & 0x01);
            break;

          case "mini":
            // Small addition to green
            modifiedPixel[1] = Math.min(255, originalPixel[1] + (dimensionHash % 3));
            break;

          case "low":
            // Slight adjustment to red and green
            modifiedPixel[1] = Math.min(255, originalPixel[0] + (dimensionHash % 2));
            break;

          case "medium":
            // Replace lower 2 bits with hash bits
            modifiedPixel[1] = (originalPixel[1] & ~0x03) | (dimensionHash & 0x03);
            break;

          case "bold":
            // Modify both red and blue channels
            modifiedPixel[0] = (originalPixel[0] & ~0x03) | ((dimensionHash >> 2) & 0x03);
            break;

          case "high":
            // Use addition for red, subtraction for blue
            modifiedPixel[1] = (originalPixel[2] & ~0x03) | ((dimensionHash >> 4) & 0x03);
            break;

          case "ultra":
            // Rotation effect: cycle RGB values
            modifiedPixel[0] = originalPixel[1];
            modifiedPixel[1] = originalPixel[2];
            break;

          case "super":
            // Multiple bit operations
            modifiedPixel[0] = (originalPixel[0] & 0xf0) | ((dimensionHash & 0xf0) >> 4);
            modifiedPixel[1] = (originalPixel[1] & 0x0f) | ((dimensionHash & 0x0f) << 4);
            break;

          case "max":
            modifiedPixel[1] = (originalPixel[2] & ~0x03) | ((dimensionHash >> 4) & 0x03);
            break;

          default:
            // Default operation for unknown levels
            modifiedPixel[3] = (originalPixel[1] & ~0x03) | (dimensionHash & 0x03);
        }

        // Apply the modified pixel
        ctx.putImageData(new ImageData(modifiedPixel, 1, 1), x, y);

        // Generate the dataURL
        const dataURL = Reflect.apply(target, thisArg, argumentsList);

        // Restore the original pixel
        ctx.putImageData(new ImageData(originalPixel, 1, 1), x, y);
        ctx.restore();

        return dataURL;
      },
    });

    // // Override getImageData to add slight randomization
    // CanvasRenderingContext2D.prototype.getImageData = new Proxy(originalMethods.getImageData, {
    //   apply(target, thisArg, argumentsList) {
    //     // Call the original method
    //     const imageData = Reflect.apply(target, thisArg, argumentsList);
    //     // Create a copy of the data to avoid modifying the original
    //     const data = new Uint8ClampedArray(imageData.data);

    //     // Add consistent noise to pixel values
    //     for (let i = 0; i < data.length; i += 4) {
    //       data[i] = Math.max(0, Math.min(255, data[i] + pixels.r));
    //       data[i + 1] = Math.max(0, Math.min(255, data[i + 1] + pixels.g));
    //       data[i + 2] = Math.max(0, Math.min(255, data[i + 2] + pixels.b));
    //       // data[i + 3] = Math.max(0, Math.min(255, data[i + 3] + pixels.a));
    //     }

    //     return new ImageData(data, imageData.width, imageData.height);
    //   },
    // });

    // Add slight offset to text positioning
    // CanvasRenderingContext2D.prototype.fillText = new Proxy(originalMethods.fillText, {
    //   apply(target, thisArg, argumentsList) {
    //     const [text, x, y, maxWidth] = argumentsList;

    //     // Add a small random offset
    //     const offsetX = x + Math.random() * 0.001;
    //     const offsetY = y + Math.random() * 0.001;

    //     return Reflect.apply(target, thisArg, [text, offsetX, offsetY, maxWidth]);
    //   },
    // });

    // Modify rectangle drawing slightly
    // CanvasRenderingContext2D.prototype.fillRect = new Proxy(originalMethods.fillRect, {
    //   apply(target, thisArg, argumentsList) {
    //     const [x, y, width, height] = argumentsList;

    //     // Deterministic modifications based on input values
    //     // Using a simple formula that will always produce the same result for the same inputs
    //     const seed = x * 10000 + y * 1000 + width * 100 + height;
    //     const modifier = Math.abs(Math.sin(seed)) * 0.2;

    //     const newX = x + modifier;
    //     const newY = y + modifier;
    //     const newWidth = width + modifier;
    //     const newHeight = height + modifier;

    //     return Reflect.apply(target, thisArg, [newX, newY, newWidth, newHeight]);
    //   },
    // });

    // Override getContext to always set willReadFrequently for 2d contexts
    HTMLCanvasElement.prototype.getContext = function (contextId, options) {
      // return originalMethods.getContext.call(this, contextId, options);

      // If it's a 2d context, ensure willReadFrequently is set
      if (contextId === "2d") {
        options = options || {};
        options.willReadFrequently = true;
      }

      // Call the original method with our modified attributes
      return originalMethods.getContext.call(this, contextId, options);
    };

    return true;
  };
}
