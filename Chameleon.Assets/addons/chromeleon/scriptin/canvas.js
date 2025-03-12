// Version: 2.0
window.randomCanvasSpoofing = true;
window.canvasR = 1;
window.canvasG = 1;
window.canvasB = 1;
window.canvasProtection = true;
window.enabled = true;

//https://privacycheck.sec.lrz.de/active/fp_c/fp_canvas.html
const getImageData = CanvasRenderingContext2D.prototype.getImageData;
//
const noisify = function (canvas, context) {
  if (context) {
    const shift = {
      r: window.randomCanvasSpoofing ? Math.floor(Math.random() * 100) - 5 : window.canvasR,
      g: window.randomCanvasSpoofing ? Math.floor(Math.random() * 100) - 5 : window.canvasG,
      b: window.randomCanvasSpoofing ? Math.floor(Math.random() * 100) - 5 : window.canvasB,
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
          imageData.data[n + 0] = imageData.data[n + 0] + shift.r * 10;
          imageData.data[n + 1] = imageData.data[n + 1] + shift.g * 10;
          imageData.data[n + 2] = imageData.data[n + 2] + shift.b * 10;
        }
      }
      context.putImageData(imageData, 0, 0);
    }
  }
};

HTMLCanvasElement.prototype.toBlob = new Proxy(HTMLCanvasElement.prototype.toBlob, {
  apply(target, self, args) {
    noisify(self, self.getContext("2d", { willReadFrequently: true }));
    //
    return Reflect.apply(target, self, args);
  },
});
//
HTMLCanvasElement.prototype.toDataURL = new Proxy(HTMLCanvasElement.prototype.toDataURL, {
  apply(target, self, args) {
    noisify(self, self.getContext("2d", { willReadFrequently: true }));
    //
    return Reflect.apply(target, self, args);
  },
});
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
