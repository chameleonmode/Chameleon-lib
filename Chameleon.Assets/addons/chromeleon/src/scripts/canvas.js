export default async function (opts) {
  return function (params = { uuid: "bloop", noise: "mid", hash: Math.random(), random: false }) {
    const { uuid, noise, random, hash } = params;
    window[uuid] = window[uuid] || {};

    // Get noise value for random or fixed noise setting
    const noiseify = () => {
      const noiseKey = random
        ? Object.keys(noises)[Math.floor(Math.random() * Object.keys(noises).length)]
        : noise;
      // Use hash to create micro-variations for uniqueness
      const baseLevel = noises[noiseKey] || 4;
      // console.log(`${baseLevel}\n`);
      return hash + baseLevel;
    };

    // Store original methods
    const originalMethods = {
      toDataURL: HTMLCanvasElement.prototype.toDataURL,
      getContext: HTMLCanvasElement.prototype.getContext,
    };

    // Noise levels - using small consistent values
    const noises = {
      nano: 0.1, // Very subtle
      mini: 1, // Barely perceptible
      low: 1.75, // Slight change
      mid: 2.5, // Small but effective
      bold: 3.75, // Noticeable but minimal
      high: 4.5, // More significant
      ultra: 5.75, // Clearly visible change
      super: 6.5, // Substantial adjustment
      max: 7, // Even more
    };

    // console.log(getNoiseValue());

    // Generate more modification points for better uniqueness
    const generatePoints = (w, h) => [
      { x: Math.floor(w / 2), y: Math.floor(h / 2) },
      { x: Math.floor(w / 4), y: Math.floor(h / 4) },
      { x: Math.floor((w * 3) / 4), y: Math.floor(h / 4) },
      { x: Math.floor(w / 4), y: Math.floor((h * 3) / 4) },
      { x: Math.floor((w * 3) / 4), y: Math.floor((h * 3) / 4) },
      // Additional points for more uniqueness
      { x: Math.floor(w / 8), y: Math.floor(h / 8) },
      { x: Math.floor((w * 7) / 8), y: Math.floor(h / 8) },
      { x: Math.floor(w / 8), y: Math.floor((h * 7) / 8) },
      { x: Math.floor((w * 7) / 8), y: Math.floor((h * 7) / 8) },
    ];

    // Ensure value stays within 0-255 range with high precision
    const clamp = (value) => Math.min(255, Math.max(0, Math.round(value * 10000) / 10000));

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
            const noiseValue = noiseify();

            // Apply modifications to each selected point with unique variations
            allPoints.forEach((point, index) => {
              // Ensure point is within canvas bounds
              const x = Math.min(width - 1, Math.max(0, point.x));
              const y = Math.min(height - 1, Math.max(0, point.y));
              
              // Get original pixel data
              const pixelData = ctx.getImageData(x, y, 1, 1);

              // Apply unique noise to each channel with micro-variations per point
              const pointVariation = (index * 0.00001) % 0.0001;
              const r = clamp(pixelData.data[0] + noiseValue + pointVariation);
              const g = clamp(pixelData.data[1] + noiseValue + (pointVariation * 1.1));
              const b = clamp(pixelData.data[2] + noiseValue + (pointVariation * 1.2));

              pixelData.data[0] = r;
              pixelData.data[1] = g;
              pixelData.data[2] = b;

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
  };
}
