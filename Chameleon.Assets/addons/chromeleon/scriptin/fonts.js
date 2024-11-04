{
  const storageCache = {
    enabled: true,
    fontsSpoofing: true,
    Fontsnoise: 1,
    Fontssign: 1,
  };

  const noisify = function (size) {
    // const valid = size && storageCache.Fontssign === 1;
    const result = size ? size + storageCache.Fontsnoise : size;
    return result;
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

    const mkey = "cffjcbnflngjpnjenjogeaojacooflng-sandboxed-fonts";
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
            storageCache.fontsSpoofing === false ||
            storageCache.enabled === false
          ) {
            return;
          }
          //
          try {
            //
            Object.defineProperty(HTMLElement.prototype, "offsetHeight", {
              get: new Proxy(
                Object.getOwnPropertyDescriptor(
                  HTMLElement.prototype,
                  "offsetHeight"
                ).get,
                {
                  apply(target, self, args) {
                    return noisify(
                      Math.floor(self.getBoundingClientRect().height)
                    );
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
                    return noisify(
                      Math.floor(self.getBoundingClientRect().width)
                    );
                  },
                }
              ),
            });
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
          } catch (e) {
            console.error(e);
          }
        }
      },
      false
    );
  }
}
