// https://privacycheck.sec.lrz.de/active/fp_gcr/fp_getclientrects.html#fpGetClientRects
// https://browserleaks.com/rects
export default async function(opts) {
  return function (params) {
    console.log(params);
    // params will be available here
    const random = params?.random || true;
    console.log("Using random:", random);

    // Define noise levels for rectangle spoofing
    const noiseLevels = {
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

    // Default settings
    const settings = {
      noiseLevel: "medium",
      DOMRectnoise: 0.98, // Default noise factor for DOMRect
      DOMRectReadOnlynoise: 0.98, // Default noise factor for DOMRectReadOnly
    };

    // Set noise factor based on settings
    settings.DOMRectnoise = noiseLevels[settings.noiseLevel];
    settings.DOMRectReadOnlynoise = noiseLevels[settings.noiseLevel];

    // Define property lists for each rectangle type
    const rectProperties = {
      DOMRect: ["x", "y", "width", "height"],
      DOMRectReadOnly: ["top", "right", "bottom", "left"],
    };

    // Store original property getters
    const originalGetters = {
      DOMRect: {},
      DOMRectReadOnly: {},
    };

    // Store original getters for each property
    for (const prop of rectProperties.DOMRect) {
      originalGetters.DOMRect[prop] = Object.getOwnPropertyDescriptor(DOMRect.prototype, prop).get;
    }

    for (const prop of rectProperties.DOMRectReadOnly) {
      originalGetters.DOMRectReadOnly[prop] = Object.getOwnPropertyDescriptor(
        DOMRectReadOnly.prototype,
        prop
      ).get;
    }

    // Methods to apply noise to rectangle properties
    const applyNoiseMethods = {
      // Apply noise to DOMRect properties
      DOMRect: function (property) {
        try {
          Object.defineProperty(DOMRect.prototype, property, {
            get: function () {
              // Get original value and apply noise multiplier
              const result = originalGetters.DOMRect[property].call(this);
              return result * settings.DOMRectnoise;
            },
          });
        } catch (error) {
          console.error(error);
        }
      },

      // Apply noise to DOMRectReadOnly properties
      DOMRectReadOnly: function (property) {
        try {
          Object.defineProperty(DOMRectReadOnly.prototype, property, {
            get: function () {
              // Get original value and apply noise multiplier
              const result = originalGetters.DOMRectReadOnly[property].call(this);
              return result * settings.DOMRectReadOnlynoise;
            },
          });
        } catch (error) {
          console.error(error);
        }
      },
    };


    // Function to apply spoofing
    function applySpoofing() {
      // Apply noise to a random property or based on noiseLevel
      const rectProp = rectProperties.DOMRect.sort(() =>
        random ? 0.5 - Math.random() : 0
      )[0];

      const rectReadOnlyProp = rectProperties.DOMRectReadOnly.sort(() =>
        random ? 0.5 - Math.random() : 0
      )[0];

      // Apply noise to selected properties
      applyNoiseMethods.DOMRect(rectProp);
      applyNoiseMethods.DOMRectReadOnly(rectReadOnlyProp);

      console.log(`Applied spoofing to DOMRect.${rectProp} and DOMRectReadOnly.${rectReadOnlyProp}`);
    }

    // Apply spoofing immediately
    applySpoofing();

    return true;
  };
}
