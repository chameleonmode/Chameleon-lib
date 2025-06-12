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
      nano: -4,
      mini: -3,
      low: -2,
      mid: -1,
      bold: 1,
      high: 2,
      ultra: 3,
      super: 4,
      max: 5,
    };

    const define = (prototype, property, prop) => {
      Object.defineProperty(prototype, property, {
        get: new Proxy(Object.getOwnPropertyDescriptor(prototype, property).get, {
          apply(target, self, args) {
            return Math.floor(self.getBoundingClientRect()[prop] + noiseify());
          },
        }),
      });
    };

    define(HTMLElement.prototype, "offsetHeight", "height");
    define(HTMLElement.prototype, "offsetWidth", "width");
    return true;
  };
}
