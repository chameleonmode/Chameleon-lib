const clientRects = {
    seed: Math.floor(Math.random() * 1000000),
    noise: {
        DOMRect: 0.00000001,
        DOMRectReadOnly: 0.000001,
        low: 0.3,
        medium: 0.5,
        high: 0.8,
    },
    metrics: {
        DOMRect: ["x", "y", "width", "height"],
        DOMRectReadOnly: ["top", "right", "bottom", "left"],
    },
    method: {
        addNoise: function (type, result) {
            const noiseLevel = clientRects.noise[type];
            return result * (1 + ((Math.random() < noiseLevel ? -1 : +1) * noiseLevel));
        },
        hashValue: function (value) {
            const perturb = Math.sin(value + clientRects.seed) * 10000;
            return value + (perturb - Math.floor(perturb)) * 0.0001;
        },
        applyResistance: function (values, type) {
            return values.map(v => this.hashValue(this.addNoise(type, v)));
        },
        adjustForBrowser: function (value) {
            // Example adjustment that could be made for specific browser behavior.
            if (navigator.userAgent.includes("Firefox")) {
                return value * 0.99; // Slight adjustment for Firefox
            }
            // Add other browser-specific adjustments here
            return value;
        },
        handleExtremeValues: function (value) {
            if (value === Infinity || value === -Infinity || isNaN(value)) {
                return 0; // Handle extreme or unusable values
            }
            return value;
        },
        createDOMRectList: function (elements) {
            return elements.map(el => {
                const rect = el.getBoundingClientRect();
                return new DOMRect(
                    this.applyResistance([rect.x], 'DOMRect')[0],
                    this.applyResistance([rect.y], 'DOMRect')[0],
                    this.applyResistance([rect.width], 'DOMRect')[0],
                    this.applyResistance([rect.height], 'DOMRect')[0]
                );
            });
        },
        DOMRect: function (e) {
            try {
                Object.defineProperty(DOMRect.prototype, e, {
                    get: new Proxy(
                        Object.getOwnPropertyDescriptor(DOMRect.prototype, e).get,
                        {
                            apply: (target, self, args) => {
                                let result = Reflect.apply(target, self, args);
                                return clientRects.method.handleExtremeValues(
                                    clientRects.method.adjustForBrowser(
                                        clientRects.method.applyResistance([result], 'DOMRect')[0]
                                    )
                                );
                            }
                        }
                    ),
                });
                //Object.defineProperty(DOMRect.prototype, e, {
                //    "get": Object.getOwnPropertyDescriptor(DOMRect.prototype, e).get
                //});
            } catch (err) {
                console.error(err);
            }
        },
        DOMRectReadOnly: function (e) {
            try {
                Object.defineProperty(DOMRectReadOnly.prototype, e, {
                    get: new Proxy(
                        Object.getOwnPropertyDescriptor(DOMRectReadOnly.prototype, e).get,
                        {
                            apply: (target, self, args) => {
                                let result = Reflect.apply(target, self, args);
                                return clientRects.method.applyResistance([result], 'DOMRectReadOnly')[0];
                            }
                        }
                    ),
                });
                //Object.defineProperty(DOMRectReadOnly.prototype, e, {
                //    "get": Object.getOwnPropertyDescriptor(DOMRectReadOnly.prototype, e).get
                //});
            } catch (err) {
                console.error(err);
            }
        }
    }
};
{
    const metrics = clientRects.metrics.DOMRect;
    for (let i = 0; i < metrics.length; i++) {
        clientRects.method.DOMRect(metrics[i]);
    }
}
// Spoofing of DOMRectReadOnly
{
    const metrics = clientRects.metrics.DOMRectReadOnly;
    for (let i = 0; i < metrics.length; i++) {
        clientRects.method.DOMRectReadOnly(metrics[i]);
    }

}

Element.prototype.getClientRects = function () {
    return {
        item: function (index) { return clientRects.method.DOMRect.prototype[index] || null; },
        length: clientRects.method.DOMRect.prototype.length,
        [Symbol.iterator]: function* () {
            for (let rect of clientRects.method.DOMRect.prototype) yield rect;
        }
    };
};

/*        Override getBoundingClientRect*/
Element.prototype.getBoundingClientRect = function () {
    const rects = this.getClientRects();
    if (rects.length === 0) {
        return new DOMRect(0, 0, 0, 0);
    }

    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
    for (const rect of rects) {
        if (rect.width !== 0 && rect.height !== 0) {
            minX = Math.min(minX, rect.x);
            minY = Math.min(minY, rect.y);
            maxX = Math.max(maxX, rect.x + rect.width);
            maxY = Math.max(maxY, rect.y + rect.height);
        }
    }

    if (minX === Infinity || minY === Infinity || maxX === -Infinity || maxY === -Infinity) {
        return rects.item(0); // Return the first if all are zero-sized
    }

    return new DOMRect(minX, minY, maxX - minX, maxY - minY);
};
 
