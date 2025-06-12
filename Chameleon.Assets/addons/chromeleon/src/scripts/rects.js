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

    // Map different noise levels from smallest to largest
    const noises = {
      //micro: Number.EPSILON, // 2.22e-16 (smallest precision unit)
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

    const define = (prototype, property) => {
      Object.defineProperty(prototype, property, {
        get: new Proxy(Object.getOwnPropertyDescriptor(prototype, property).get, {
          apply(target, self, args) {
            const result = Reflect.apply(target, self, args);
            return result + noiseify();
          },
        }),
      });
    };

    // Define property lists for each rectangle type
    if (!window[uuid]["rects"]) {
      window[uuid]["rects"] = true;
      // Apply noise to all selected properties
      ["x", "y", "width", "height"].forEach((property) => {
        define(DOMRect.prototype, property);
      });
      ["top", "right", "bottom", "left"].forEach((property) => {
        define(DOMRectReadOnly.prototype, property);
      });
    }

    return true;
  };
}
