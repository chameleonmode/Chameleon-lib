//https://gist.github.com/abrahamjuliot/7baf3be8c451d23f7a8693d7e28a35e2
{
  const noiseLevels = {
    micro: 0.0001,
    mini: 0.0002,
    low: 0.0003,
    medium: 0.0004,
    bold: 0.0005,
    high: 0.006,
    ultra: 0.007,
    super: 0.08,
    max: 0.09,
  }
  const storageCache = {
    enabled: true,
    webglSpoofing: true,
    randomWebGLSpoofing: false,
    WebGLnoise: 1,
    WebGLnoiseAmplitude: 1,
  };

  const config = {
    seed: Math.floor(Math.random() * 1000000),
    randvalue: function () {
      let thisseed = (this.seed * 9301 + 49297) % 233280;
      return thisseed / 233280;
    },
    buffer: function (target) {
      let proto = target.prototype ? target.prototype : target.__proto__;
      //
      proto.bufferData = new Proxy(proto.bufferData, {
        get(target, p, receiver) {
          return target;
        },
        apply(target, self, args) {
          try {
            const [target, srcData, usage] = args;
            if (srcData instanceof ArrayBuffer || ArrayBuffer.isView(srcData)) {
              const length = srcData.byteLength;
              const dataView = new DataView(srcData.buffer || srcData);
              for (let i = 0; i < length; i += 4) {
                const value = dataView.getFloat32(i, true);
                const noise = storageCache.WebGLnoiseAmplitude * (storageCache.WebGLnoise * 2 - 1) * value;
                dataView.setFloat32(i, value + noise, true);
              }
            }
          } catch (error) {
            log.error("Error in bufferData spoofing", error);
          }
          return Reflect.apply(target, self, args);
        },
      });
    },
  };

  {
    const loadPromise = new Promise((resolve) => {
      window.addEventListener(
        "cffjcbnflngjpnjenjogeaojacooflng-settings",
        (event) => {
          Object.assign(storageCache, event.detail);
          if(storageCache.randomWebGLSpoofing){
            // Update WebGL noise levels
            storageCache.WebGLnoise = config.randvalue();
            storageCache.WebGLnoiseAmplitude = noiseLevels[storageCache.noiseLevel];
          }
          resolve();
        }
      );
    });

    const mkey = "cffjcbnflngjpnjenjogeaojacooflng-sandboxed-gl";
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
            storageCache.webglSpoofing === false ||
            storageCache.enabled === false
          ) {
            return;
          }
          //
          try {
            [WebGLRenderingContext, WebGL2RenderingContext].forEach(
              (context) => {
                config.buffer(context);
              }
            );
            if (e.source) {
              if (e.source.WebGLRenderingContext) {
                e.source.WebGLRenderingContext.prototype.bufferData =
                  WebGLRenderingContext.prototype.bufferData;
              }
              //
              if (e.source.WebGL2RenderingContext) {
                e.source.WebGL2RenderingContext.prototype.bufferData =
                  WebGL2RenderingContext.prototype.bufferData;
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
