export default async function (opts) {
  return function (params = { random: true, noise: "medium" }) {
    const { random, noise } = params;

    // Define noise levels for rectangle spoofing
    const noises = {
      micro: 0.95, // More acceptable range: values closer to 1.0
      mini: 0.96, // cause less noticeable distortion but still
      low: 0.97, // provide fingerprinting protection
      medium: 0.98,
      bold: 0.99,
      high: 1.01,
      ultra: 1.02,
      super: 1.03,
      max: 1.05,
    };

    // Methods to apply noise to rectangle objects
    const methods = {
      // Apply noise to DOMRect property
      DOMRect: function (property) {
        const originalGetter = Object.getOwnPropertyDescriptor(DOMRect.prototype, property).get;

        Object.defineProperty(DOMRect.prototype, property, {
          get: function () {
            console.log(`DOMRect property ${property} found`);
            // Get original value and apply noise multiplier
            const result = originalGetter.call(this);
            return result * noises[noise];
          },
        });
      },

      // Apply noise to DOMRectReadOnly property
      DOMRectReadOnly: function (property) {
        const originalGetter = Object.getOwnPropertyDescriptor(DOMRectReadOnly.prototype, property).get;

        Object.defineProperty(DOMRectReadOnly.prototype, property, {
          get: function () {
            console.log(`DOMRectReadOnly property ${property} found`);
            // Get original value and apply noise multiplier
            const result = originalGetter.call(this);
            return result * noises[noise];
          },
        });
      },
    };

    // Define property lists for each rectangle type
    const props = {
      rect: ["x", "y", "width", "height"],
      readOnly: ["top", "right", "bottom", "left"],
    };

    if (random) {
      // Apply noise to a random property
      const rect = props.rect[Math.floor(Math.random() * props.rect.length)];
      methods.DOMRect(rect);

      const readOnly = props.readOnly[Math.floor(Math.random() * props.readOnly.length)];
      methods.DOMRectReadOnly(readOnly);

      console.log(`Applied spoofing to DOMRect.${rect} and DOMRectReadOnly.${readOnly}`);
    } else {
      // Apply noise to all rectangle properties
      for (const prop of props.rect) {
        methods.DOMRect(prop);
      }
      for (const prop of props.readOnly) {
        methods.DOMRectReadOnly(prop);
      }

      console.log(`Applied spoofing to all DOMRect and DOMRectReadOnly properties`);
    }

    return true;
  };
}
