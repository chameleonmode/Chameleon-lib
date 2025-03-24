export default async function (opts) {
  return function (params = { uuid: "bloop", noise: "medium", random: false }) {
    const { uuid, noise, random } = params;

    // Map different noise levels from smallest to largest
    const noises = {
      //nano: Number.EPSILON, // 2.22e-16 (smallest precision unit)
      micro: Number.EPSILON * 5, // 2.22e-15.5
      mini: Number.EPSILON * 10, // 2.22e-15
      low: Number.EPSILON * 100, // 2.22e-14
      medium: Number.EPSILON * 1000, // 2.22e-13
      bold: Number.EPSILON * 10000, // 2.22e-12
      high: Number.EPSILON * 100000, // 2.22e-11
      ultra: Number.EPSILON * 1000000, // 2.22e-10
      super: 0.000000001, // 1e-9
      max: 0.00000001, // 1e-8
    };

    const noiseify = () =>
      noises[random ? Object.keys(noises)[Math.floor(Math.random() * Object.keys(noises).length)] : noise];

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
    if (!window[uuid]) {
      window[uuid] = true;
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