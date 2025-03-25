export default async function (opts) {
  return function (params = { uuid: "bloop", noise: "mid", random: false }) {
    const { uuid, noise, random } = params;
    window[uuid] = window[uuid] || {};

    // Store original methods
    const originalMethods = {
      toDataURL: HTMLCanvasElement.prototype.toDataURL,
      getContext: HTMLCanvasElement.prototype.getContext,
    };

    // Noise levels - using small consistent values
    const noises = {
      nano: 0.1, // Very subtle
      mini: 0.5, // Barely perceptible
      low: 0.75, // Slight change
      mid: 1.5, // Small but effective
      bold: 1.75, // Noticeable but minimal
      high: 2.5, // More significant
      ultra: 2.75, // Clearly visible change
      super: 3.5, // Substantial adjustment
      max: 4, // Maximum recommended for subtlety
    };

    // Get noise level for random or fixed noise setting
    const getNoiseValue = () => {
      const noiseKey = random
        ? Object.keys(noises)[Math.floor(Math.random() * Object.keys(noises).length)]
        : noise;
      return noises[noiseKey] || 1.5; // Default to 'mid' if invalid
    };

    // Define the modification points based on canvas dimensions
    const generatePoints = (w, h) => [
      { x: Math.floor(w / 2), y: Math.floor(h / 2) },
      { x: Math.floor(w / 4), y: Math.floor(h / 4) },
      { x: Math.floor((w * 3) / 4), y: Math.floor(h / 4) },
      { x: Math.floor(w / 4), y: Math.floor((h * 3) / 4) },
      { x: Math.floor((w * 3) / 4), y: Math.floor((h * 3) / 4) },
    ];

    // Ensure value stays within 0-255 range
    const clamp = (value) => Math.min(255, Math.max(0, value));

    if (!window[uuid]["canvi"]) {
      window[uuid]["canvi"] = true;

      HTMLCanvasElement.prototype.toDataURL = new Proxy(originalMethods.toDataURL, {
        apply(target, thisArg, argumentsList) {
          try {
            // Get the 2D context
            const ctx = thisArg.getContext("2d");

            // Canvas dimensions
            const width = thisArg.width || 1;
            const height = thisArg.height || 1;

            // Generate all potential points
            const allPoints = generatePoints(width, height);

            // Get noise value once for consistency
            const noiseValue = getNoiseValue();

            // Apply modifications to each selected point
            allPoints.forEach((point) => {
              // Ensure point is within canvas bounds
              const x = Math.min(width - 1, Math.max(0, point.x));
              const y = Math.min(height - 1, Math.max(0, point.y));
              // Get original pixel data
              const pixelData = ctx.getImageData(x, y, 1, 1);

              // Apply consistent noise to each channel
              pixelData.data[0] = clamp(pixelData.data[0] + noiseValue);
              pixelData.data[1] = clamp(pixelData.data[1] + noiseValue);
              pixelData.data[2] = clamp(pixelData.data[2] + noiseValue);

              // Apply the modified pixel
              ctx.putImageData(pixelData, x, y);
            });
          } catch (e) {
            // Fallback to original behavior if any errors occur
          }

          // Generate the dataURL
          return Reflect.apply(target, thisArg, argumentsList);
        },
      });

      HTMLCanvasElement.prototype.getContext = function (contextId, options) {
        // If it's a 2d context, ensure willReadFrequently is set
        if (contextId === "2d") {
          options = options || {};
          options.willReadFrequently = true;
        }
        return originalMethods.getContext.call(this, contextId, options);
      };
    }

    // Return clean-up function
    return true;
    // return function() {
    //   HTMLCanvasElement.prototype.toDataURL = originalMethods.toDataURL;
    //   HTMLCanvasElement.prototype.getContext = originalMethods.getContext;
    //   delete window[uuid]["canvi"];
    // };
  };
}
