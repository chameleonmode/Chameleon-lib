(async () => {
    "use strict";

    let script = document.createElement("script");
    script.textContent = `
(function(){
    const inject = (spoofContext) => {
      if (spoofContext.CHAMELEON_SPOOF) return;

      spoofContext.CHAMELEON_SPOOF = "CHAMELEON_SPOOF";`

    // Geolocation spoofing
    if (settings.geoSpoofing) {
        script.addEventListener("sp-request-geo-data", () => {
            const geoSettings = {
                latitude: settings.latitude,
                longitude: settings.longitude,
                accuracy: settings.accuracy,
                enabled: settings.geoSpoofing,
                randomize: settings.randomizeGeo,
                bypass: settings.bypass,
            };

            const applyGeoSettings = () => {
                if (geoSettings.randomize) {
                    try {
                        const randomizeFactor =
                            typeof geoSettings.randomize === "number"
                                ? geoSettings.randomize
                                : 0.001;
                        const randomizeCoordinate = (coord) => {
                            const m = coord.toString().split(".")[1].length;
                            return Number(
                                (
                                    coord +
                                    (Math.random() > 0.5 ? 1 : -1) * randomizeFactor * Math.random()
                                ).toFixed(m)
                            );
                        };

                        geoSettings.latitude = randomizeCoordinate(geoSettings.latitude);
                        geoSettings.longitude = randomizeCoordinate(geoSettings.longitude);
                    } catch (e) {
                        console.warn("Cannot randomize GEO", e);
                    }
                }

                script.dataset.prefs = JSON.stringify(geoSettings);
                script.dispatchEvent(new Event("sp-response-geo-data"));
            };

            if (!geoSettings.enabled) {
                applyGeoSettings();
            } else if (geoSettings.latitude && geoSettings.longitude) {
                applyGeoSettings();
            } else {
                // Prompt for coordinates if not set
                const r = prompt(
                    `Enter your spoofed "latitude" and "longitude" (e.g. values for London, UK)

The number of digits to appear after the decimal point must be greater than 4
Use https://www.latlong.net/ to find these values`,
                    "51.507351, -0.127758"
                );

                if (r === null) {
                    applyGeoSettings();
                } else {
                    const [latitude, longitude] = r.split(/\s*,\s*/);

                    try {
                        if (!isFinite(latitude) || Math.abs(latitude) > 90) {
                            throw Error("Latitude must be a number between -90 and 90");
                        }
                        if (!isFinite(longitude) || Math.abs(longitude) > 180) {
                            throw Error("Longitude must a number between -180 and 180");
                        }
                        if (
                            latitude.split(".")[1].length < 4 ||
                            longitude.split(".")[1].length < 4
                        ) {
                            throw Error(
                                "The number of digits to appear after the decimal point must be greater than 4. Example: 51.507351, -0.127758"
                            );
                        }

                        geoSettings.latitude = Number(latitude);
                        geoSettings.longitude = Number(longitude);

                        // Update settings
                        settings.latitude = geoSettings.latitude;
                        settings.longitude = geoSettings.longitude;
                        browser.storage.sync.set(settings);

                        applyGeoSettings();
                    } catch (e) {
                        console.error(e);
                        applyGeoSettings();
                        alert("GEO Request Denied\n\n" + e.message);
                    }
                }
            }
        });
    }

    // Timezone spoofing
    if (settings.timezoneSpoofing) {
        script.textContent += `
if (new Date()[spoofContext.CHAMELEON_SPOOF]) {
  spoofContext.Date = Date;
  return;
}
let ORIGINAL_DATE = spoofContext.Date;

class SpoofDate extends ORIGINAL_DATE {
    #ad; // adjusted date

    #sync() {
      const offset = (${settings.tzOffset} + super.getTimezoneOffset());
      this.#ad = new ORIGINAL_DATE(this.getTime() + offset * 60 * 1000);
    }

    constructor(...args) {
      super(...args);

      this.#sync();
    }
    getTimezoneOffset() {
      return ${settings.tzOffset};
    }
    /* to string (only supports en locale) */
    toTimeString() {
      if (isNaN(this)) {
        return super.toTimeString();
      }

      const parts = super.toLocaleString.call(this, 'en', {
        timeZone: '${settings.timezone}',
        timeZoneName: 'longOffset'
      }).split('GMT');

      if (parts.length !== 2) {
        return super.toTimeString();
      }

      const a = 'GMT' + parts[1].replace(':', '');

      const b = super.toLocaleString.call(this, 'en', {
        timeZone: '${settings.timezone}',
        timeZoneName: 'long'
      }).split(/(AM |PM )/i).pop();

      return super.toTimeString.apply(this.#ad).split(' GMT')[0] + ' ' + a + ' (' + b + ')';
    }
    /* only supports en locale */
    toDateString() {
      return super.toDateString.apply(this.#ad);
    }
    /* only supports en locale */
    toString() {
      if (isNaN(this)) {
        return super.toString();
      }
      return this.toDateString() + ' ' + this.toTimeString();
    }
    toLocaleDateString(...args) {
      args[1] = args[1] || {};
      args[1].timeZone = args[1].timeZone || '${settings.timezone}';

      return super.toLocaleDateString(...args);
    }
    toLocaleTimeString(...args) {
      args[1] = args[1] || {};
      args[1].timeZone = args[1].timeZone || '${settings.timezone}';

      return super.toLocaleTimeString(...args);
    }
    toLocaleString(...args) {
      args[1] = args[1] || {};
      args[1].timeZone = args[1].timeZone || '${settings.timezone}';

      return super.toLocaleString(...args);
    }
    /* get */
    #get(name, ...args) {
      return super[name].call(this.#ad, ...args);
    }
    getDate(...args) {
      return this.#get('getDate', ...args);
    }
    getDay(...args) {
      return this.#get('getDay', ...args);
    }
    getHours(...args) {
      return this.#get('getHours', ...args);
    }
    getMinutes(...args) {
      return this.#get('getMinutes', ...args);
    }
    getMonth(...args) {
      return this.#get('getMonth', ...args);
    }
    getYear(...args) {
      return this.#get('getYear', ...args);
    }
    getFullYear(...args) {
      return this.#get('getFullYear', ...args);
    }
    /* set */
    #set(type, name, args) {
      if (type === 'ad') {
        const n = this.#ad.getTime();
        const r = this.#get(name, ...args);

        return super.setTime(this.getTime() + r - n);
      }
      else {
        const r = super[name](...args);
        this.#sync();

        return r;
      }
    }
    setHours(...args) {
      return this.#set('ad', 'setHours', args);
    }
    setMinutes(...args) {
      return this.#set('ad', 'setMinutes', args);
    }
    setMonth(...args) {
      return this.#set('ad', 'setMonth', args);
    }
    setDate(...args) {
      return this.#set('ad', 'setDate', args);
    }
    setYear(...args) {
      return this.#set('ad', 'setYear', args);
    }
    setFullYear(...args) {
      return this.#set('ad', 'setFullYear', args);
    }
    setTime(...args) {
      return this.#set('md', 'setTime', args);
    }
    setUTCDate(...args) {
      return this.#set('md', 'setUTCDate', args);
    }
    setUTCFullYear(...args) {
      return this.#set('md', 'setUTCFullYear', args);
    }
    setUTCHours(...args) {
      return this.#set('md', 'setUTCHours', args);
    }
    setUTCMinutes(...args) {
      return this.#set('md', 'setUTCMinutes', args);
    }
    setUTCMonth(...args) {
      return this.#set('md', 'setUTCMonth', args);
    }
  }

spoofContext.Date = SpoofDate;
spoofContext.Date = new Proxy(Date, {
  apply(target, self, args) {
    return new SpoofDate(...args);
  }
});

  const DateTimeFormat = spoofContext.Intl.DateTimeFormat;

  class SpoofDateTimeFormat extends Intl.DateTimeFormat {
    constructor(...args) {
      if (!args[1]) {
        args[1] = {};
      }
      if (!args[1].timeZone) {
        args[1].timeZone = '${settings.timezone}';
      }

      super(...args);
    }
  }

  spoofContext.Intl.DateTimeFormat = SpoofDateTimeFormat;
  spoofContext.Intl.DateTimeFormat = new Proxy(Intl.DateTimeFormat, {
    apply(target, self, args) {
      return new Intl.DateTimeFormat(...args);
    }
  });
`.replace(
            /ORIGINAL_DATE/g,
            String.fromCharCode(65 + Math.floor(Math.random() * 26)) +
            Math.random()
                .toString(36)
                .substring(Math.floor(Math.random() * 5) + 5)
        );
    }

    // Client rects spoofing
    if (settings.clientRectsSpoofing) {
        const clientRectsScript = `
  {
    const rand = { 
        noise: {
        DOMRect: 0.1,
        DOMRectReadOnly: 0.1,
        low: 0.3,
        medium: 0.5,
        high: 0.8,
      },
      metrics: {
        DOMRect: ["x", "y", "width", "height"],
        DOMRectReadOnly: ["top", "right", "bottom", "left"],
      },
    };
    const noieMultiplier = rand.noise['${settings.noiseLevel}'];

    const originalGetClientRects = spoofContext.Element.prototype.getClientRects;
    const originalGetBoundingClientRect = spoofContext.Element.prototype.getBoundingClientRect;

    spoofContext.Element.prototype.getClientRects = function() {
      const rects = originalGetClientRects.call(this);
      for (let i = 0; i < rects.length; i++) {
        rects[i].x += (Math.random() - noieMultiplier) * 0.01;
        rects[i].y += (Math.random() - noieMultiplier) * 0.01;
        rects[i].width += (Math.random() - noieMultiplier) * 0.01;
        rects[i].height += (Math.random() - noieMultiplier) * 0.01;
      }
      return rects;
    };

    spoofContext.Element.prototype.getBoundingClientRect = function() {
      const rect = originalGetBoundingClientRect.call(this);
      rect.x += (Math.random() - noieMultiplier) * 0.01;
      rect.y += (Math.random() - noieMultiplier) * 0.01;
      rect.width += (Math.random() - noieMultiplier) * 0.01;
      rect.height += (Math.random() - noieMultiplier) * 0.01;
      return rect;
    };
    const domRectProto = spoofContext.DOMRect.prototype;
    const domRectReadOnlyProto = spoofContext.DOMRectReadOnly.prototype;
    const clientRects = {
        DOMRect: function (e) {
          try {
            Object.defineProperty(domRectProto, e, {
              get: new Proxy(
                Object.getOwnPropertyDescriptor(domRectProto, e).get,
                {
                  get(target, p, receiver) {
                    return target;
                  },
                  apply(target, self, args) {
                    const result = Reflect.apply(target, self, args);
                    //
                    const _result =
                      result *
                      (1 +
                        (Math.random() < noieMultiplier
                          ? -1
                          : +1) *
                          rand.noise.DOMRect);
                    return _result;
                  },
                }
              ),
            });
            //
            Object.defineProperty(domRectProto, e, {
              get: Object.getOwnPropertyDescriptor(domRectProto, e).get,
            });
          } catch (e) {
            console.error(e);
          }
        },
        DOMRectReadOnly: function (e) {
          try {
            Object.defineProperty(domRectReadOnlyProto, e, {
              get: new Proxy(
                Object.getOwnPropertyDescriptor(
                  domRectReadOnlyProto,
                  e
                ).get,
                {
                  get(target, p, receiver) {
                    return target;
                  },
                  apply(target, self, args) {
                    const result = Reflect.apply(target, self, args);
                    //
                    const _result =
                      result *
                      (1 +
                        (Math.random() < noieMultiplier
                          ? -1
                          : +1) *
                          rand.noise.DOMRectReadOnly);
                    return _result;
                  },
                }
              ),
            });
            //
            Object.defineProperty(domRectReadOnlyProto, e, {
              get: Object.getOwnPropertyDescriptor(domRectReadOnlyProto, e)
                .get,
            });
          } catch (e) {
            console.error(e);
          }
        },
    }; 
    
    //Spoofing of DOMRect
    {
      const metrics = ["x", "y", "width", "height"];
      for (let i = 0; i < metrics.length; i++) {
        clientRects.DOMRect(metrics[i]);
      }
    }

    // Spoofing of DOMRectReadOnly
    {
      const metrics = ["top", "right", "bottom", "left"];
      for (let i = 0; i < metrics.length; i++) {
        clientRects.DOMRectReadOnly(metrics[i]);
      }
    }
  }
`;
        script.textContent += clientRectsScript;
    }

    script.textContent += ` };

    inject(window);
  })()
  `
        .replace(/CHAMELEON_SPOOF/g, randObjName)
        .replace(
            /ORIGINAL_INTL/g,
            String.fromCharCode(65 + Math.floor(Math.random() * 26)) +
            Math.random()
                .toString(36)
                .substring(Math.floor(Math.random() * 5) + 5)
        );
    // Inject the script into the page
    document.documentElement.append(script);
    script.remove();

    script = document.createElement('script');
    script.src = URL.createObjectURL(new Blob([script.textContent], { type: 'text/javascript' }));
    (document.head || document.documentElement).appendChild(script);
    try {
        URL.revokeObjectURL(script.src);
    } catch (e) { }
    script.remove();
})();
