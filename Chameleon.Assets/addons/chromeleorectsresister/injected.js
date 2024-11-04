{
  let config = {
    "noise": {
      "DOMRect": 0.00000001,
      "DOMRectReadOnly": 0.000001
    },
    "metrics": {
      "DOMRect": ['x', 'y', "width", "height"],
      "DOMRectReadOnly": ["top", "right", "bottom", "left"]
    },
    "method": {
      "DOMRect": function (e) {
        try {
          Object.defineProperty(DOMRect.prototype, e, {
            "get": new Proxy(Object.getOwnPropertyDescriptor(DOMRect.prototype, e).get, {
              apply(target, self, args) {
                const result = Reflect.apply(target, self, args);
                //
                const _result = result * (1 + (Math.random() < 0.5 ? -1 : +1) * config.noise.DOMRect);
                return _result;
              }
            })
          });
        } catch (e) {
          //console.error(e);
        }
      },
      "DOMRectReadOnly": function (e) {
        try {
          Object.defineProperty(DOMRectReadOnly.prototype, e, {
            "get": new Proxy(Object.getOwnPropertyDescriptor(DOMRectReadOnly.prototype, e).get, {
              apply(target, self, args) {
                const result = Reflect.apply(target, self, args);
                //
                const _result = result * (1 + (Math.random() < 0.5 ? -1 : +1) * config.noise.DOMRectReadOnly);
                return _result;
              }
            })
          });
        } catch (e) {
          //console.error(e);
        }
      }
    }
  };
  //
  config.method.DOMRect(config.metrics.DOMRect.sort(() => 0.5 - Math.random())[0]);
  config.method.DOMRectReadOnly(config.metrics.DOMRectReadOnly.sort(() => 0.5 - Math.random())[0]);
}

{
  const mkey = "clientrects-defender-sandboxed-frame";
  document.documentElement.setAttribute(mkey, '');
  //
  window.addEventListener("message", function (e) {
    if (e.data && e.data === mkey) {
      e.preventDefault();
      e.stopPropagation();
      //
      try {
        if (e.source.DOMRect) {
          const metrics = ['x', 'y', "width", "height"];
          for (let i = 0; i < metrics.length; i++) {
            Object.defineProperty(e.source.DOMRect.prototype, metrics[i], {
              "get": Object.getOwnPropertyDescriptor(DOMRect.prototype, metrics[i]).get
            });
          }
        }
      } catch (e) {
        //console.error(e);
      }
      //
      try {
        if (e.source.DOMRectReadOnly) {
          const metrics = ["top", "right", "bottom", "left"];
          for (let i = 0; i < metrics.length; i++) {
            Object.defineProperty(e.source.DOMRectReadOnly.prototype, metrics[i], {
              "get": Object.getOwnPropertyDescriptor(DOMRectReadOnly.prototype, metrics[i]).get
            });
          }
        }
      } catch (e) {
        //console.error(e);
      }
    }
  }, false);
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
