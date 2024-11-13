{
  const fontsNoiseLevels = {
    micro: 0.1,
    mini: 0.4,
    low: 0.8,
    medium: 1.4,
    bold: 1.8,
    high: 2.4,
    ultra: 2.5,
    super: 3.4,
    max: 3.8,
  };

  const noisify = function (size) {
    const result = size ? size + settings.Fontsnoise : size;
    return result;
  };

  if (settings.randomFontsSpoofing) {
    const SIGN = Math.random() < Math.random() ? -1 : 1;
    settings.Fontsnoise =
      Math.floor(Math.random() + SIGN * Math.random()) *
      fontsNoiseLevels[settings.noiseLevel];

    const tmp = [-1, -1, -1, -1, -1, -1, +1, -1, -1, -1];
    const index = Math.floor(Math.random() * tmp.length);
    settings.Fontssign = tmp[index];
  }

  if (settings.fontsSpoofing === true && settings.enabled === true) {
    //
    Object.defineProperty(HTMLElement.prototype, "offsetHeight", {
      get: new Proxy(
        Object.getOwnPropertyDescriptor(
          HTMLElement.prototype,
          "offsetHeight"
        ).get,
        {
          apply(target, self, args) {
            return noisify(Math.floor(self.getBoundingClientRect().height));
          },
        }
      ),
    });
    //
    Object.defineProperty(HTMLElement.prototype, "offsetWidth", {
      get: new Proxy(
        Object.getOwnPropertyDescriptor(
          HTMLElement.prototype,
          "offsetWidth"
        ).get,
        {
          apply(target, self, args) {
            return noisify(Math.floor(self.getBoundingClientRect().width));
          },
        }
      ),
    });
  }

  {
    const mkey = "cffjcbnflngjpnjenjogeaojacooflng-sandboxed-fonts";
    document.documentElement.setAttribute(mkey, "");
    //
    window.addEventListener(
      "message",
      async function (e) {
        if (e.data && e.data.key === mkey) {
          e.preventDefault();
          e.stopPropagation();
          if (settings.fontsSpoofing === false || settings.enabled === false) {
            return;
          }
          //
          if (e.source) {
            if (e.source.HTMLElement) {
              Object.defineProperty(
                e.source.HTMLElement.prototype,
                "offsetWidth",
                {
                  get: Object.getOwnPropertyDescriptor(
                    HTMLElement.prototype,
                    "offsetWidth"
                  ).get,
                }
              );
              //
              Object.defineProperty(
                e.source.HTMLElement.prototype,
                "offsetHeight",
                {
                  get: Object.getOwnPropertyDescriptor(
                    HTMLElement.prototype,
                    "offsetHeight"
                  ).get,
                }
              );
            }
          }
        }
      },
      false
    );
  }
}
