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
    HTMLCanvasElement.prototype.toDataURL = new Proxy(originalMethods.toDataURL, {
      apply(target, thisArg, argumentsList) {
        const ctx = thisArg.getContext('2d');
        
        // Save state
        ctx.save();
        
        // Create a deterministic "hash" from canvas dimensions
        const hash = (thisArg.width * 3 + thisArg.height * 7) % 255;
        
        // Define a constant position based on canvas size
        const x = Math.min(thisArg.width - 1, Math.max(0, Math.floor(thisArg.width / 3)));
        const y = Math.min(thisArg.height - 1, Math.max(0, Math.floor(thisArg.height / 3)));
        
        // Get original pixel
        const pixelData = ctx.getImageData(x, y, 1, 1);
        const originalPixel = new Uint8ClampedArray(pixelData.data);
        
        // Use different math operation: bitwise XOR
        const modifiedPixel = new Uint8ClampedArray(originalPixel);
        modifiedPixel[1] = (hash & 0x03); // Bitwise XOR with lower 2 bits of hash
        
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
    
    // Helper function to slightly modify a base64 character
    function modifyBase64Char(char) {
      const base64Chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
      // If not a base64 char, return it unchanged
      if (!base64Chars.includes(char)) return char;
      
      // Get the current index and choose a similar character
      const currentIndex = base64Chars.indexOf(char);
      const newIndex = (currentIndex + (Math.random() > 0.5 ? 1 : -1) + 64) % 64;
      return base64Chars[newIndex];
    }

    // // Override getImageData to add slight randomization
    // CanvasRenderingContext2D.prototype.getImageData = new Proxy(originalMethods.getImageData, {
    //   apply(target, thisArg, argumentsList) {
    //     // Call the original method
    //     const imageData = Reflect.apply(target, thisArg, argumentsList);
    //     return addNoiseToImageData(imageData);
    //   },
    // });

    // // Add slight offset to text positioning
    // CanvasRenderingContext2D.prototype.fillText = new Proxy(originalMethods.fillText, {
    //   apply(target, thisArg, argumentsList) {
    //     const [text, x, y, maxWidth] = argumentsList;

    //     // Add a small random offset
    //     const offsetX = x + positions.x;
    //     const offsetY = y + positions.y;

    //     return Reflect.apply(target, thisArg, [text, offsetX, offsetY, maxWidth]);
    //   },
    // });

    // // Modify rectangle drawing slightly
    // CanvasRenderingContext2D.prototype.fillRect = new Proxy(originalMethods.fillRect, {
    //   apply(target, thisArg, argumentsList) {
    //     const [x, y, width, height] = argumentsList;

    //     // Very slight modifications to dimensions
    //     const newX = x + rects.x;
    //     const newY = y + rects.y;
    //     const newWidth = width + rects.width;
    //     const newHeight = height + rects.height;

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
