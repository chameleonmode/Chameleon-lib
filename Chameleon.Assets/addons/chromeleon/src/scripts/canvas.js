export default async function (opts) {
  return function (params) {
    const { rects, pixels, positions } = params || {};

    // Store original methods
    const originalMethods = {
      getContext: HTMLCanvasElement.prototype.getContext,
      toDataURL: HTMLCanvasElement.prototype.toDataURL,
      getImageData: CanvasRenderingContext2D.prototype.getImageData,
      fillText: CanvasRenderingContext2D.prototype.fillText,
      fillRect: CanvasRenderingContext2D.prototype.fillRect,
    };

    // Helper function to add noise to image data
    function addNoiseToImageData(imageData) {
      // Create a copy of the data to avoid modifying the original
      const data = new Uint8ClampedArray(imageData.data);

      // Add consistent noise to pixel values
      for (let i = 0; i < data.length; i += 4) {
        data[i] = Math.max(0, Math.min(255, data[i] + pixels.r));
        data[i + 1] = Math.max(0, Math.min(255, data[i + 1] + pixels.g));
        data[i + 2] = Math.max(0, Math.min(255, data[i + 2] + pixels.b));
        // data[i + 3] = Math.max(0, Math.min(255, data[i + 3] + pixels.a));
      }

      return new ImageData(data, imageData.width, imageData.height);
    }

    //// Override toDataURL to add slight randomization
    //HTMLCanvasElement.prototype.toDataURL = new Proxy(originalMethods.toDataURL, {
    //  apply(target, thisArg, argumentsList) {
    //    try {
    //      // Get the original image data
    //      const ctx = thisArg.getContext("2d");
    //      const imageData = ctx.getImageData(0, 0, thisArg.width, thisArg.height);

    //      // Modify the image data
    //      const modifiedImageData = addNoiseToImageData(imageData);

    //      // Apply the modified data back to the canvas
    //      ctx.putImageData(modifiedImageData, 0, 0);
    //    } catch (error) {
    //      console.error("Error modifying image data:", error);
    //    }

    //    // Call the original method
    //    return Reflect.apply(target, thisArg, argumentsList);
    //  },
    //});

    //// Override getImageData to add slight randomization
    CanvasRenderingContext2D.prototype.getImageData = new Proxy(originalMethods.getImageData, {
      apply(target, thisArg, argumentsList) {
        // Call the original method
        const imageData = Reflect.apply(target, thisArg, argumentsList);
        return addNoiseToImageData(imageData);
      },
    });

    // Add slight offset to text positioning
    CanvasRenderingContext2D.prototype.fillText = new Proxy(originalMethods.fillText, {
      apply(target, thisArg, argumentsList) {
        const [text, x, y, maxWidth] = argumentsList;

        // Add a small random offset
        const offsetX = x + positions.x;
        const offsetY = y + positions.y;

        return Reflect.apply(target, thisArg, [text, offsetX, offsetY, maxWidth]);
      },
    });

    // Modify rectangle drawing slightly
    CanvasRenderingContext2D.prototype.fillRect = new Proxy(originalMethods.fillRect, {
      apply(target, thisArg, argumentsList) {
        const [x, y, width, height] = argumentsList;

        // Very slight modifications to dimensions
        const newX = x + rects.x;
        const newY = y + rects.y;
        const newWidth = width + rects.width;
        const newHeight = height + rects.height;

        return Reflect.apply(target, thisArg, [newX, newY, newWidth, newHeight]);
      },
    });

    // // Override getContext to always set willReadFrequently for 2d contexts
    // HTMLCanvasElement.prototype.getContext = function (contextId, options) {
    //   // return originalMethods.getContext.call(this, contextId, options);

    //   // If it's a 2d context, ensure willReadFrequently is set
    //   if (contextId === "2d") {
    //     options = options || {};
    //     options.willReadFrequently = true;
    //   }

    //   // Call the original method with our modified attributes
    //   return originalMethods.getContext.call(this, contextId, options);
    // };

    console.log("[Canvas Fingerprint Protection] Initialized" + JSON.stringify(params));

    return true;
  };
}
