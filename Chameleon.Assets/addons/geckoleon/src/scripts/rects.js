export default function (opts) {
  const { noise: noiseLevel, random } = opts;
  console.log("Geckoleon: Direct method override starting", noiseLevel, random);

  // Map different noise levels
  const noises = {
    nano: Number.EPSILON * 5, // 2.22e-15.5
    mini: Number.EPSILON * 10, // 2.22e-15
    low: Number.EPSILON * 100, // 2.22e-14
    mid: Number.EPSILON * 1000, // 2.22e-13
    bold: Number.EPSILON * 10000, // 2.22e-12
    high: Number.EPSILON * 100000, // 2.22e-11
    ultra: Number.EPSILON * 1000000, // 2.22e-10
    super: 0.000000001, // 1e-9
    max: 0.00000001, // 1e-8
  };

  function Noise() {
    if (random) {
      const keys = Object.keys(noises);
      return noises[keys[Math.floor(Math.random() * keys.length)]];
    }
    return noises[noiseLevel] || noises.max;
  }

  // Store original methods
  const originals = {
    getBoundingClientRect: Element.prototype.getBoundingClientRect,
    getClientRects: Element.prototype.getClientRects,
  };

  // CRITICAL: Override the getClientRects method directly
  Element.prototype.getClientRects = function () {
    // Call the original method
    const originalRects = originals.getClientRects.apply(this, arguments);

    // Apply noise to each rect
    const noise = Noise();

    // Create a completely new DOMRectList-like object
    const clientRects = {};

    // Set the length property
    Object.defineProperty(clientRects, "length", {
      value: originalRects.length,
    });

    // Add the item method
    clientRects.item = function (index) {
      if (index >= originalRects.length) return null;

      const rect = originalRects[index];
      return createNoisyRect(rect, noise);
    };

    // Add index access
    for (let i = 0; i < originalRects.length; i++) {
      clientRects[i] = createNoisyRect(originalRects[i], noise);
    }

    // Make it iterable
    clientRects[Symbol.iterator] = function* () {
      for (let i = 0; i < originalRects.length; i++) {
        yield clientRects[i];
      }
    };

    if (Math.random() < 0.001) {
      console.log("Geckoleon: Applied noise to getClientRects", noise);
    }

    return clientRects;
  };

  // Override getBoundingClientRect
  Element.prototype.getBoundingClientRect = function () {
    const originalRect = originals.getBoundingClientRect.apply(this, arguments);
    return createNoisyRect(originalRect,  Noise());
  };

  // Helper function to create a noisy rect
  function createNoisyRect(originalRect, noise) {
    // Create a new object with the same properties
    const noisyRect = {};

    // Apply noise to each property
    ["x", "y", "width", "height", "top", "right", "bottom", "left"].forEach((prop) => {
      if (prop in originalRect) {
        // Use Object.defineProperty to ensure the property appears in Object.keys
        Object.defineProperty(noisyRect, prop, {
          value: originalRect[prop] * (1 + noise),
          enumerable: true,
        });
      }
    });

    // Copy any other properties
    Object.getOwnPropertyNames(originalRect).forEach((prop) => {
      if (!noisyRect.hasOwnProperty(prop) && typeof originalRect[prop] !== "function") {
        noisyRect[prop] = originalRect[prop];
      }
    });

    // Copy methods
    ["toJSON"].forEach((method) => {
      if (typeof originalRect[method] === "function") {
        noisyRect[method] = originalRect[method].bind(originalRect);
      }
    });

    return noisyRect;
  }

  console.log("Geckoleon: Direct method override complete");
}
