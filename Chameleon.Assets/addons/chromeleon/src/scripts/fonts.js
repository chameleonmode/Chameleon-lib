export default async function (opts) {
  return function (params = { uuid: "bloop", noise: "mid", random: false }) {
    const { uuid, noise, random } = params;

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

    const noiseify = () => {
      return noises[
        random ? Object.keys(noises)[Math.floor(Math.random() * Object.keys(noises).length)] : noise
      ];
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
