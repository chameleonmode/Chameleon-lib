{
  //https://privacycheck.sec.lrz.de/active/fp_c/fp_canvas.html
  const storageCache = {
    enabled: true,
    canvasProtection: true,
    canvasR: 1,
    canvasG: 1,
    canvasB: 1,
    canvasA: 1,
  };

  const getImageData = CanvasRenderingContext2D.prototype.getImageData;
  //
  const noisify = function (canvas, context) {
    if (context) {
      const shift = {
        // r: Math.floor(Math.random() * 10) - 5,
        // g: Math.floor(Math.random() * 10) - 5,
        // b: Math.floor(Math.random() * 10) - 5,
        // a: Math.floor(Math.random() * 10) - 5,
        r: storageCache.canvasR,
        g: storageCache.canvasG,
        b: storageCache.canvasB,
        a: storageCache.canvasA,
      };
      //
      const width = canvas.width;
      const height = canvas.height;
      //
      if (width && height) {
        const imageData = getImageData.apply(context, [0, 0, width, height]);
        //
        for (let i = 0; i < height; i++) {
          for (let j = 0; j < width; j++) {
            const n = i * (width * 4) + j * 4;
            imageData.data[n + 0] = imageData.data[n + 0] + shift.r;
            imageData.data[n + 1] = imageData.data[n + 1] + shift.g;
            imageData.data[n + 2] = imageData.data[n + 2] + shift.b;
            imageData.data[n + 3] = imageData.data[n + 3] + shift.a;
          }
        }
        context.putImageData(imageData, 0, 0);
      }
    }
  };

  {
    const loadPromise = new Promise((resolve) => {
      window.addEventListener(
        "cffjcbnflngjpnjenjogeaojacooflng-settings",
        (event) => {
          Object.assign(storageCache, event.detail);
          resolve();
        }
      );
    });

    const mkey = "cffjcbnflngjpnjenjogeaojacooflng-sandboxed-canvas";
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
            storageCache.canvasProtection === false ||
            storageCache.enabled === false
          ) {
            return;
          }
          //
          try {
            //
            HTMLCanvasElement.prototype.toBlob = new Proxy(
              HTMLCanvasElement.prototype.toBlob,
              {
                apply(target, self, args) {
                  noisify(self, self.getContext("2d", { willReadFrequently: true }));
                  //
                  return Reflect.apply(target, self, args);
                },
              }
            );
            //
            HTMLCanvasElement.prototype.toDataURL = new Proxy(
              HTMLCanvasElement.prototype.toDataURL,
              {
                apply(target, self, args) {
                  noisify(self, self.getContext("2d", { willReadFrequently: true }));
                  //
                  return Reflect.apply(target, self, args);
                },
              }
            );
            //
            CanvasRenderingContext2D.prototype.getImageData = new Proxy(
              CanvasRenderingContext2D.prototype.getImageData,
              {
                apply(target, self, args) {
                  noisify(self.canvas, self);
                  //
                  return Reflect.apply(target, self, args);
                },
              }
            );
            if (e.source) {
              if (e.source.CanvasRenderingContext2D) {
                e.source.CanvasRenderingContext2D.prototype.getImageData =
                  CanvasRenderingContext2D.prototype.getImageData;
              }
              //
              if (e.source.HTMLCanvasElement) {
                e.source.HTMLCanvasElement.prototype.toBlob =
                  HTMLCanvasElement.prototype.toBlob;
                e.source.HTMLCanvasElement.prototype.toDataURL =
                  HTMLCanvasElement.prototype.toDataURL;
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
