{
  //https://privacycheck.sec.lrz.de/active/fp_gcr/fp_getclientrects.html#fpGetClientRects
  const noiseLevels = {
    micro: 0.1,
    mini: 0.2,
    low: 0.3,
    medium: 0.4,
    bold: 0.5,
    high: 0.6,
    ultra: 0.7,
    super: 0.8,
    max: 0.9,
  }

  const settings = {
    enabled: true,
    clientRectsSpoofing: true,
    randomRectsSpoofing: false,
    DOMRectnoise: 1,
    DOMRectReadOnlynoise: 1,
    noiseLevel: "medium",
  };

  const metrics = {
    DOMRect: ["x", "y", "width", "height"],
    DOMRectReadOnly: ["top", "right", "bottom", "left"],
  }

  const methods = {
    DOMRect: function (e) {
      try {
        Object.defineProperty(DOMRect.prototype, e, {
          get: new Proxy(
            Object.getOwnPropertyDescriptor(DOMRect.prototype, e).get,
            {
              apply(target, self, args) {
                const result = Reflect.apply(target, self, args);
                return result * settings.DOMRectnoise;
              },
            }
          ),
        });
      } catch (e) {
        console.error(e);
      }
    },
    DOMRectReadOnly: function (e) {
      try {
        Object.defineProperty(DOMRectReadOnly.prototype, e, {
          get: new Proxy(
            Object.getOwnPropertyDescriptor(DOMRectReadOnly.prototype, e).get,
            {
              apply(target, self, args) {
                const result = Reflect.apply(target, self, args);
                return result * settings.DOMRectReadOnlynoise;
              },
            }
          ),
        });
      } catch (e) {
        console.error(e);
      }
    },
  };

  {
    const loadPromise = new Promise((resolve) => {
      window.addEventListener(
        "cffjcbnflngjpnjenjogeaojacooflng-settings",
        (event) => {
          Object.assign(settings, event.detail);
          resolve();
        }
      );
    });

    const mkey = "cffjcbnflngjpnjenjogeaojacooflng-sandboxed-rects";
    document.documentElement.setAttribute(mkey, "");
    //
    window.addEventListener(
      "message",
      async function (e) {
        if (e.data && e.data.key === mkey) {
          e.preventDefault();
          e.stopPropagation();
          await loadPromise;
          if (
            settings.clientRectsSpoofing === false ||
            settings.enabled === false
          ) {
            return;
          }
          
          methods.DOMRect(
            metrics.DOMRect.sort(() =>
              settings.randomRectsSpoofing
                ? 0.5 - Math.random()
                : noiseLevels[settings.noiseLevel]
            )[0]
          );
          methods.DOMRectReadOnly(
            metrics.DOMRectReadOnly.sort(() =>
              settings.randomRectsSpoofing
                ? 0.5 - Math.random()
                : noiseLevels[settings.noiseLevel]
            )[0]
          );

          //
          try {
            if (e.source.DOMRect) {
              const metrics = ["x", "y", "width", "height"];
              for (let i = 0; i < metrics.length; i++) {
                Object.defineProperty(e.source.DOMRect.prototype, metrics[i], {
                  get: Object.getOwnPropertyDescriptor(
                    DOMRect.prototype,
                    metrics[i]
                  ).get,
                });
              }
            }
          } catch (e) {
            console.error(e);
          }
          //
          try {
            if (e.source.DOMRectReadOnly) {
              const metrics = ["top", "right", "bottom", "left"];
              for (let i = 0; i < metrics.length; i++) {
                Object.defineProperty(e.source.DOMRectReadOnly.prototype, metrics[i], {
                    get: Object.getOwnPropertyDescriptor(
                      DOMRectReadOnly.prototype,
                      metrics[i]
                    ).get,
                  }
                );
              }
            }
          } catch (e) {
            console.error(e);
          }
        }
      },
      false
    );
  }
}
// {
//   const metrics = clientRects.metrics.DOMRect;
//   for (let i = 0; i < metrics.length; i++) {
//     clientRects.method.DOMRect(metrics[i]);
//   }
// }
// // Spoofing of DOMRectReadOnly
// {
//   const metrics = clientRects.metrics.DOMRectReadOnly;
//   for (let i = 0; i < metrics.length; i++) {
//     clientRects.method.DOMRectReadOnly(metrics[i]);
//   }
// }
// Element.prototype.getClientRects = function () {
//   return {
//     item: function (index) {
//       return clientRects.method.DOMRect.prototype[index] || null;
//     },
//     length: clientRects.method.DOMRect.prototype.length,
//     [Symbol.iterator]: function* () {
//       for (let rect of clientRects.method.DOMRect.prototype) yield rect;
//     },
//   };
// };

// /*        Override getBoundingClientRect*/
// Element.prototype.getBoundingClientRect = function () {
//   const rects = this.getClientRects();
//   if (rects.length === 0) {
//     return new DOMRect(0, 0, 0, 0);
//   }

//   let minX = Infinity,
//     minY = Infinity,
//     maxX = -Infinity,
//     maxY = -Infinity;
//   for (const rect of rects) {
//     if (rect.width !== 0 && rect.height !== 0) {
//       minX = Math.min(minX, rect.x);
//       minY = Math.min(minY, rect.y);
//       maxX = Math.max(maxX, rect.x + rect.width);
//       maxY = Math.max(maxY, rect.y + rect.height);
//     }
//   }

//   if (
//     minX === Infinity ||
//     minY === Infinity ||
//     maxX === -Infinity ||
//     maxY === -Infinity
//   ) {
//     return rects.item(0); // Return the first if all are zero-sized
//   }

//   return new DOMRect(minX, minY, maxX - minX, maxY - minY);
// };
