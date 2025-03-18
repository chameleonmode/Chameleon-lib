export default function() {
  return function protection() {
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
    
    return true;
  };
}
