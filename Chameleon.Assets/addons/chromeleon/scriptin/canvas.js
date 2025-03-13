// Enhanced Canvas Fingerprinting Protection
// https://privacycheck.sec.lrz.de/active/fp_c/fp_canvas.html
(function () {
  console.log("Enhanced Canvas Fingerprinting Protection");
  // Store original methods

  // Set properties to spoof
  // Get the script content
  const scriptContent = `
    const originalGetContext = HTMLCanvasElement.prototype.getContext;
  const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
  const originalToBlob = HTMLCanvasElement.prototype.toBlob;
  const originalFillText = CanvasRenderingContext2D.prototype.fillText;
  const originalFillRect = CanvasRenderingContext2D.prototype.fillRect;
  const originalGetImageData = CanvasRenderingContext2D.prototype.getImageData;
  const originalPutImageData = CanvasRenderingContext2D.prototype.putImageData;
  const originalDrawImage = CanvasRenderingContext2D.prototype.drawImage;
  const originalCreateLinearGradient = CanvasRenderingContext2D.prototype.createLinearGradient;
  const originalCreateRadialGradient = CanvasRenderingContext2D.prototype.createRadialGradient;
  const originalStrokeText = CanvasRenderingContext2D.prototype.strokeText;
  const originalMeasureText = CanvasRenderingContext2D.prototype.measureText;
  const originalIsPointInPath = CanvasRenderingContext2D.prototype.isPointInPath;

  // Helper function to decide if canvas is likely used for fingerprinting
  function isLikelyFingerprinting(context) {
    const canvas = context.canvas;
    // Small canvases are often used for fingerprinting
    return true; //canvas.width < 300 && canvas.height < 300;
  }

  // Helper function to add subtle noise
  function addNoise(value, factor = 0.04) {
    return value + (Math.random() * factor - factor / 2);
  }

  // Override fillText to add small variations to text rendering
  CanvasRenderingContext2D.prototype.fillText = function (text, x, y, maxWidth) {
    console.log("fillText intercepted:", text);

    if (isLikelyFingerprinting(this)) {
      // Modify coordinates slightly
      const xMod = addNoise(x);
      const yMod = addNoise(y);

      // Slightly modify the font if set
      const originalFont = this.font;
      if (originalFont && Math.random() < 0.3) {
        // 30% chance to modify font slightly
        const fontSize = parseFloat(originalFont);
        if (!isNaN(fontSize)) {
          this.font = originalFont.replace(
            fontSize.toString(),
            (fontSize + (Math.random() * 0.02 - 0.01)).toString()
          );
        }
      }

      const result =
        maxWidth !== undefined
          ? originalFillText.call(this, text, xMod, yMod, maxWidth)
          : originalFillText.call(this, text, xMod, yMod);

      // Restore original font
      if (originalFont) {
        this.font = originalFont;
      }

      return result;
    }

    return maxWidth !== undefined
      ? originalFillText.call(this, text, x, y, maxWidth)
      : originalFillText.call(this, text, x, y);
  };

  // Override strokeText similarly
  CanvasRenderingContext2D.prototype.strokeText = function (text, x, y, maxWidth) {
    console.log("strokeText intercepted");

    if (isLikelyFingerprinting(this)) {
      const xMod = addNoise(x);
      const yMod = addNoise(y);

      return maxWidth !== undefined
        ? originalStrokeText.call(this, text, xMod, yMod, maxWidth)
        : originalStrokeText.call(this, text, xMod, yMod);
    }

    return maxWidth !== undefined
      ? originalStrokeText.call(this, text, x, y, maxWidth)
      : originalStrokeText.call(this, text, x, y);
  };

  // Override measureText to add noise to text measurements
  CanvasRenderingContext2D.prototype.measureText = function (text) {
    console.log("measureText intercepted");
    const measurements = originalMeasureText.call(this, text);

    if (isLikelyFingerprinting(this)) {
      // Add tiny variations to width measurement
      const originalWidth = measurements.width;
      Object.defineProperty(measurements, "width", {
        get: function () {
          return originalWidth + (Math.random() * 0.02 - 0.01);
        },
      });
    }

    return measurements;
  };

  // Override fillRect to add subtle variations
  CanvasRenderingContext2D.prototype.fillRect = function (x, y, width, height) {
    console.log("fillRect intercepted");

    if (isLikelyFingerprinting(this)) {
      // Add subtle variations to rectangle dimensions
      const xMod = addNoise(x);
      const yMod = addNoise(y);
      const widthMod = addNoise(width);
      const heightMod = addNoise(height);
      return originalFillRect.call(this, xMod, yMod, widthMod, heightMod);
    }

    return originalFillRect.call(this, x, y, width, height);
  };

  // Override getImageData to add noise to pixel data
  CanvasRenderingContext2D.prototype.getImageData = function (x, y, width, height) {
    console.log("getImageData intercepted");
    const imageData = originalGetImageData.call(this, x, y, width, height);

    if (isLikelyFingerprinting(this)) {
      const pixels = imageData.data;
      // Add subtle random noise to random pixels
      for (let i = 0; i < pixels.length; i += 4) {
        if (Math.random() < 0.05) {
          // 5% of pixels
          for (let j = 0; j < 3; j++) {
            // Only modify RGB, not alpha
            pixels[i + j] = Math.max(0, Math.min(255, pixels[i + j] + (Math.random() > 0.5 ? 1 : -1)));
          }
        }
      }
    }

    return imageData;
  };

  // Override putImageData
  CanvasRenderingContext2D.prototype.putImageData = function (
    imageData,
    dx,
    dy,
    dirtyX,
    dirtyY,
    dirtyWidth,
    dirtyHeight
  ) {
    console.log("putImageData intercepted");

    if (isLikelyFingerprinting(this) && arguments.length <= 3) {
      // Only modify if it's the simple version of the call
      const dxMod = addNoise(dx, 0.02);
      const dyMod = addNoise(dy, 0.02);
      return originalPutImageData.call(this, imageData, dxMod, dyMod);
    }

    return originalPutImageData.apply(this, arguments);
  };

  // Override drawImage to add subtle variations
  CanvasRenderingContext2D.prototype.drawImage = function (image, ...args) {
    console.log("drawImage intercepted");

    if (isLikelyFingerprinting(this)) {
      // Add subtle variations to positioning parameters
      // Different signatures: (img, dx, dy), (img, dx, dy, dw, dh), (img, sx, sy, sw, sh, dx, dy, dw, dh)
      const modifiedArgs = args.map((arg) => {
        return typeof arg === "number" ? addNoise(arg, 0.03) : arg;
      });

      return originalDrawImage.call(this, image, ...modifiedArgs);
    }

    return originalDrawImage.apply(this, [image, ...args]);
  };

  // Override gradient creation methods
  CanvasRenderingContext2D.prototype.createLinearGradient = function (x0, y0, x1, y1) {
    console.log("createLinearGradient intercepted");

    if (isLikelyFingerprinting(this)) {
      const x0Mod = addNoise(x0, 0.02);
      const y0Mod = addNoise(y0, 0.02);
      const x1Mod = addNoise(x1, 0.02);
      const y1Mod = addNoise(y1, 0.02);
      return originalCreateLinearGradient.call(this, x0Mod, y0Mod, x1Mod, y1Mod);
    }

    return originalCreateLinearGradient.call(this, x0, y0, x1, y1);
  };

  CanvasRenderingContext2D.prototype.createRadialGradient = function (x0, y0, r0, x1, y1, r1) {
    console.log("createRadialGradient intercepted");

    if (isLikelyFingerprinting(this)) {
      const x0Mod = addNoise(x0, 0.02);
      const y0Mod = addNoise(y0, 0.02);
      const r0Mod = addNoise(r0, 0.02);
      const x1Mod = addNoise(x1, 0.02);
      const y1Mod = addNoise(y1, 0.02);
      const r1Mod = addNoise(r1, 0.02);
      return originalCreateRadialGradient.call(this, x0Mod, y0Mod, r0Mod, x1Mod, y1Mod, r1Mod);
    }

    return originalCreateRadialGradient.call(this, x0, y0, r0, x1, y1, r1);
  };

  // Override isPointInPath to add slight variations
  CanvasRenderingContext2D.prototype.isPointInPath = function (path, x, y, fillRule) {
    console.log("isPointInPath intercepted");

    // Handle both function signatures: (x, y, fillRule) and (path, x, y, fillRule)
    if (typeof path === "number") {
      // First signature: (x, y, fillRule)
      x = path;
      y = arguments[1];
      fillRule = arguments[2];
      path = null;
    }

    if (isLikelyFingerprinting(this)) {
      // Small random offset to coordinates
      const xMod = addNoise(x, 0.02);
      const yMod = addNoise(y, 0.02);

      return path
        ? originalIsPointInPath.call(this, path, xMod, yMod, fillRule)
        : originalIsPointInPath.call(this, xMod, yMod, fillRule);
    }

    return path
      ? originalIsPointInPath.call(this, path, x, y, fillRule)
      : originalIsPointInPath.call(this, x, y, fillRule);
  };

  // Override getContext to track canvas creation
  HTMLCanvasElement.prototype.getContext = function (...args) {
    console.log("getContext intercepted:", args[0]);
    const context = originalGetContext.apply(this, args);
    return context;
  };

  // Function to add noise to DataURL outputs
  function addNoiseToDataURL(dataURL) {
    // Create an image from the dataURL
    const img = new Image();
    img.src = dataURL;

    // Create a temporary canvas to manipulate the image
    const tempCanvas = document.createElement("canvas");
    const tempCtx = tempCanvas.getContext("2d");

    // Wait for the image to load
    return new Promise((resolve) => {
      img.onload = () => {
        // Set canvas dimensions to match the image
        tempCanvas.width = img.width;
        tempCanvas.height = img.height;

        // Draw the original image onto the canvas
        tempCtx.drawImage(img, 0, 0);

        // Get the image data
        const imageData = tempCtx.getImageData(0, 0, tempCanvas.width, tempCanvas.height);
        const pixels = imageData.data;

        // Add subtle random noise to a small percentage of pixels
        for (let i = 0; i < pixels.length; i += 4) {
          if (Math.random() < 0.03) {
            // Modify 3% of pixels
            // Modify RGB values slightly (±1)
            for (let j = 0; j < 3; j++) {
              const noise = Math.random() > 0.5 ? 1 : -1;
              pixels[i + j] = Math.max(0, Math.min(255, pixels[i + j] + noise));
            }
          }
        }

        // Put the modified image data back on the canvas
        tempCtx.putImageData(imageData, 0, 0);

        // Generate a new dataURL from the modified canvas
        const noisyDataURL = tempCanvas.toDataURL();
        resolve(noisyDataURL);
      };
    });
  }

  // Override toDataURL to modify the output when fingerprinting is detected
  HTMLCanvasElement.prototype.toDataURL = function (...args) {
    console.log("toDataURL intercepted");

    //if (this.width < 300 && this.height < 300) {
    // For small canvases likely used for fingerprinting,
    // add a random hash to make the result different each time
    const realOutput = originalToDataURL.apply(this, args);
    const randomSuffix = Math.random().toString(36).substring(2, 8);
    return realOutput + "#" + randomSuffix;
    //}

    return originalToDataURL.apply(this, args);
  };

  // Override toBlob for completeness
  HTMLCanvasElement.prototype.toBlob = function (callback, type, quality) {
    console.log("toBlob intercepted");

    //if (this.width < 300 && this.height < 300) {
    // For small canvases likely used for fingerprinting
    originalToBlob.call(
      this,
      (blob) => {
        // Create a slightly modified blob
        const reader = new FileReader();
        reader.onload = function () {
          // Add random noise to the string representation
          const randomNum = Math.floor(Math.random() * 10);
          const modifiedString = reader.result + String.fromCharCode(randomNum);

          // Convert back to blob
          const modifiedBlob = new Blob([modifiedString], { type: blob.type });
          callback(modifiedBlob);
        };
        reader.readAsText(blob);
      },
      type,
      quality
    );
    return;
    //}

    return originalToBlob.apply(this, arguments);
  };
  
 // Additional properties that might be used for fingerprinting
 // Modify navigator properties
 const navigatorProps = {
   hardwareConcurrency: Math.min(8, navigator.hardwareConcurrency),
   deviceMemory: Math.min(8, navigator.deviceMemory || 8),
 };
 
 // Apply navigator property spoofing
 for (const [prop, value] of Object.entries(navigatorProps)) {
   if (navigator[prop] !== undefined) {
     try {
       Object.defineProperty(navigator, prop, {
         get: function() { return value; }
       });
     } catch (e) {
       console.log("Failed to override navigator." + prop);
     }
   }
 }
`;

  // Function to inject script into the current document
  function injectScriptIntoDocument(scriptContent, document) {
    // Create blob from the script content
    const blob = new Blob([scriptContent], { type: "application/javascript" });

    // Create a URL for the blob
    const blobURL = URL.createObjectURL(blob);

    // Display the blob URL
    console.log("Blob URL:", blobURL);

    // Create and inject the script element
    const script = document.createElement("script");
    script.src = blobURL;
    script.onload = function () {
      // Clean up the URL when done
      URL.revokeObjectURL(blobURL);

      // Update status
      console.log("Script injected successfully in", document.location.href);

      // Display current navigator values
      console.log("navigator.hardwareConcurrency:", navigator.hardwareConcurrency);
      console.log("navigator.deviceMemory:", navigator.deviceMemory);
    };

    // Append the script to the document
    document.documentElement.appendChild(script);
  }

  // Inject into the main document
  injectScriptIntoDocument(scriptContent, document);

  // Function to recursively inject into all iframes
  function injectIntoIframes(parentDocument) {
    try {
      const iframes = parentDocument.querySelectorAll("iframe");

      iframes.forEach((iframe) => {
        try {
          // Check if we can access the iframe's contentDocument (same-origin policy)
          if (iframe.contentDocument) {
            // Inject script into this iframe
            injectScriptIntoDocument(scriptContent, iframe.contentDocument);

            // Recursively check for nested iframes
            injectIntoIframes(iframe.contentDocument);
          }
        } catch (err) {
          console.log("Cannot access iframe (likely cross-origin):", err);
        }
      });
    } catch (err) {
      console.log("Error while injecting into iframes:", err);
    }
  }

  // Start the iframe injection process
  injectIntoIframes(document);

  // Set up a MutationObserver to handle dynamically created iframes
  const observer = new MutationObserver((mutations) => {
    mutations.forEach((mutation) => {
      mutation.addedNodes.forEach((node) => {
        // Check if the added node is an iframe
        if (node.tagName === "IFRAME") {
          // Wait a bit for the iframe to load
          setTimeout(() => {
            try {
              if (node.contentDocument) {
                injectScriptIntoDocument(scriptContent, node.contentDocument);
                injectIntoIframes(node.contentDocument);
              }
            } catch (err) {
              console.log("Cannot access dynamically added iframe:", err);
            }
          }, 100);
        }

        // Check for iframes within added nodes
        if (node.querySelectorAll) {
          const childIframes = node.querySelectorAll("iframe");
          childIframes.forEach((iframe) => {
            setTimeout(() => {
              try {
                if (iframe.contentDocument) {
                  injectScriptIntoDocument(scriptContent, iframe.contentDocument);
                  injectIntoIframes(iframe.contentDocument);
                }
              } catch (err) {
                console.log("Cannot access iframe within added node:", err);
              }
            }, 100);
          });
        }
      });
    });
  });

  // Start observing the document with the configured parameters
  observer.observe(document, {
    childList: true,
    subtree: true,
  });

  // For Chrome extension context, add a message listener to handle iframe injection in web pages
  if (typeof chrome !== "undefined" && chrome.runtime && chrome.runtime.onMessage) {
    chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
      if (request.action === "injectScript") {
        // Reinject into all available contexts
        injectScriptIntoDocument(scriptContent, document);
        injectIntoIframes(document);
        sendResponse({ success: true });
      }
      return true; // Required for async response
    });
  }
})();

(function () {
  var a = ".doscript{display:block}.noscript{display:none!important}.script{opacity:1}",
    b = document.head || document.getElementsByTagName("head")[0],
    c = document.createElement("style");
  c.type = "text/css";
  c.styleSheet ? (c.styleSheet.cssText = a) : c.appendChild(document.createTextNode(a));
  b.appendChild(c);
})();

/*! canvas.js */
!(function () {
  var y = _el("#canvas-hash"),
    _ = _el("#canvas-ratio"),
    L = _el("#canvas-file"),
    b = _el("#canvas-table");
  function T(t, e) {
    var n = "";
    return (
      "IHDR" == t
        ? ((n = "PNG image header: "),
          (n = (n += e.width + "x" + e.height + ", ") + e.depth + " bits/sample, "),
          0 == e.type
            ? (n += "grayscale, ")
            : 2 == e.type
            ? (n += "truecolor, ")
            : 3 == e.type
            ? (n += "paletted, ")
            : 4 == e.type
            ? (n += "grayscale+alpha, ")
            : 6 == e.type && (n += "truecolor+alpha, "),
          "0" == e.interlaced ? (n += "noninterlaced, ") : "1" == e.interlaced && (n += "interlaced, "),
          (n = n.slice(0, -2)))
        : "gAMA" == t
        ? (n = "file gamma = : " + e.gamma)
        : "sRGB" == t
        ? (n = "sRGB color space, rendering intent: " + e.desc)
        : "IDAT" == t
        ? (n = "PNG image data")
        : "IEND" == t && (n = "end-of-image marker"),
      n
    );
  }
  (function e() {
    _.classList.add("load-td");
    var n,
      a,
      v,
      r = !0,
      s = ico(0) + "False",
      i = ico(0) + "False",
      t = ico(0) + "False",
      o = "BrowserLeaks,com <canvas> 1.0";
    if (
      (c = _el("#canvas-iframe").contentDocument.createElement("canvas")).getContext &&
      (n = c.getContext("2d"))
    ) {
      if (((s = ico(1) + "True"), "function" == typeof c.getContext("2d").fillText)) {
        i = ico(1) + "True";
        try {
          c.setAttribute("width", 220),
            c.setAttribute("height", 30),
            (n.textBaseline = "top"),
            (n.font = "14px 'Arial'"),
            (n.textBaseline = "alphabetic"),
            (n.fillStyle = "#f60"),
            n.fillRect(125, 1, 62, 20),
            (n.fillStyle = "#069"),
            n.fillText(o, 2, 15),
            (n.fillStyle = "rgba(102, 204, 0, 0.7)"),
            n.fillText(o, 4, 17);
        } catch (t) {
          void 0 === (n = (c = document.createElement("canvas")).getContext("2d")) ||
          "function" != typeof c.getContext("2d").fillText
            ? ((s = ico(0) + "False"), (i = ico(0) + "False"), (r = !1))
            : (c.setAttribute("width", 220),
              c.setAttribute("height", 30),
              (n.textBaseline = "top"),
              (n.font = "14px 'Arial'"),
              (n.textBaseline = "alphabetic"),
              (n.fillStyle = "#f60"),
              n.fillRect(125, 1, 62, 20),
              (n.fillStyle = "#069"),
              n.fillText(o, 2, 15),
              (n.fillStyle = "rgba(102, 204, 0, 0.7)"),
              n.fillText(o, 4, 17));
        }
      } else r = !1;
      if (r && "function" == typeof c.toDataURL) {
        try {
          if ("boolean" == typeof (a = c.toDataURL("image/png")) || void 0 === a) throw 1;
        } catch (t) {
          a = "";
        }
        0 === a.indexOf("data:image/png") ? (t = ico(1) + "True") : (r = !1);
      } else r = !1;
    } else r = !1;
    if (
      ((_el("#canvas-support-2d").innerHTML = s),
      (_el("#canvas-support-text").innerHTML = i),
      (_el("#canvas-support-todataurl").innerHTML = t),
      r)
    ) {
      var o = n,
        l = a,
        c = (function () {
          var e = atob(l.split(",")[1]),
            n = new Uint8Array(e.length);
          for (let t = 0; t < e.length; t++) n[t] = e.charCodeAt(t);
          return n;
        })(),
        s = md5(c),
        d =
          ((y.textContent = s),
          (y.className = "wball mono upper"),
          (v = s),
          fetch("/api/canvas/" + v)
            .then(function (t) {
              return t.json();
            })
            .then(function (t) {
              if (void 0 !== t[v] && "not_listed" !== t[v]) {
                (_.textContent = (function (t, e) {
                  for (var n, a = 2; (n = 100 - ((100 * t) / e).toFixed(a)), a++, "100" == n && 0 < t; );
                  return (
                    (n = 5 < n.toString().length ? n.toFixedNoRounding(2) : n) +
                    "% (" +
                    t +
                    " of " +
                    e +
                    " user agents have the same signature)"
                  );
                })(t[v].count, t[v].total)),
                  (_el("#canvas-verdict-ua").textContent = t[v].ua[0][0]),
                  (_el("#canvas-verdict-os").textContent = t[v].os[0][0]),
                  _el("#canvas-verdict").classList.remove("n");
                var e,
                  n = t[v],
                  a = 0,
                  r = [];
                for (e in n)
                  (r[e] = n[e].length), 10 < r[e] && (r[e] = n[e].length = 10), a < r[e] && (a = r[e]);
                (a = r.ua + r.ua_ver > (a = r.os + r.os_ver) ? r.ua + r.ua_ver : a) <
                  r.device + r.platform && (a = r.device + r.platform),
                  (a += 1);
                for (
                  var s =
                      '<tr><td class="th r" colspan="2"><h3>Operating Systems</h3></td><td class="th r" colspan="2"><h3>Browsers</h3></td><td class="th" colspan="2"><h3>Devices</h3></td></tr>',
                    i = 0,
                    o = 0,
                    l = 0,
                    c = 0,
                    d = 0,
                    f = 0,
                    u = 0;
                  u < a;
                  u++
                )
                  (s += "<tr>"),
                    void 0 !== n.os_ver[u]
                      ? (i++,
                        (s +=
                          "<td>" +
                          n.os_ver[u][0] +
                          '</td><td class="r">' +
                          n.os_ver[u][1] +
                          "/" +
                          n.count +
                          "</td>"))
                      : void 0 !== n.engine_ver[u - i]
                      ? 0 == o
                        ? (i++, (o = 1), (s += '<td class="t th r" colspan="2"><h3>Engines</h3></td>'))
                        : (s +=
                            "<td>" +
                            n.engine_ver[u - i][0] +
                            '</td><td class="r">' +
                            n.engine_ver[u - i][1] +
                            "/" +
                            n.count +
                            "</td>")
                      : (s += '<td></td><td class="r"></td>'),
                    void 0 !== n.ua[u]
                      ? (l++,
                        (s +=
                          "<td>" +
                          n.ua[u][0] +
                          '</td><td class="r">' +
                          n.ua[u][1] +
                          "/" +
                          n.count +
                          "</td>"))
                      : void 0 !== n.ua_ver[u - l]
                      ? 0 == c
                        ? (l++,
                          (c = 1),
                          (s += '<td class="t th r" colspan="2"><h3>Browsers by Version</h3></td>'))
                        : (s +=
                            "<td>" +
                            n.ua_ver[u - l][0] +
                            '</td><td class="r">' +
                            n.ua_ver[u - l][1] +
                            "/" +
                            n.count +
                            "</td>")
                      : (s += '<td></td><td class="r"></td>'),
                    void 0 !== n.device[u]
                      ? (d++,
                        (s +=
                          "<td>" + n.device[u][0] + "</td><td>" + n.device[u][1] + "/" + n.count + "</td>"))
                      : void 0 !== n.platform[u - d]
                      ? 0 == f
                        ? (d++, (f = 1), (s += '<td class="t th" colspan="2"><h3>Platforms</h3></td>'))
                        : (s +=
                            "<td>" +
                            n.platform[u - d][0] +
                            "</td><td>" +
                            n.platform[u - d][1] +
                            "/" +
                            n.count +
                            "</td>")
                      : (s += "<td></td><td></td>"),
                    (s += "</tr>");
                b.insertAdjacentHTML("beforeend", s),
                  (b.style.marginBottom = "2px"),
                  [b, _el("#canvas-stats")].forEach(function (t) {
                    t.classList.remove("n");
                  }),
                  sectionClick(!1);
              } else _.textContent = "100% (The signature is unique to our database)";
            })
            .catch(function (t) {
              (_.innerHTML = ico(0) + 'Data retrieval error <a id="canvas-retry" href="#">[retry]</a>'),
                _el("#canvas-retry").addEventListener("click", function (t) {
                  t.preventDefault(), _.classList.add("load-td"), e();
                });
            })
            .finally(function () {
              _.classList.remove("load-td");
            }),
          (_el("#canvas-img").innerHTML =
            '<img src="' + l + '" alt="&nbsp;Error displaying &lt;img&gt; tag">'),
          0);
      try {
        for (
          var f = o.getImageData(0, 0, 220, 30),
            u = new Uint32Array(f.data.buffer),
            h = u.length,
            g = {},
            p = 0;
          p < h;
          p++
        ) {
          var m = "" + (16777215 & u[p]);
          g[m] || (d++, (g[m] = 0)), g[m]++;
        }
      } catch (t) {}
      d < 1 && (d = "n/a"),
        (_el("#canvas-file-colors").textContent = d),
        (_el("#canvas-file-size").textContent = c.length + " bytes");
      var x = new PngToy([
        {
          doCRC: "true",
        },
      ]);
      x.fetch(c)
        .then(function () {
          if (2303741511 !== x.view.getUint32(0) || 218765834 !== x.view.getUint32(4))
            throw new Error("Not a PNG file.");
          for (
            var t,
              e,
              n = "IHDR,PLTE,sPLT,tRNS,tEXt,gAMA,cHRM,sRGB,hIST,pHYs,bKGD,tIME,sBIT,oFFs,sTER,sCAL,pCAL",
              a = "",
              r = 0,
              s = x.chunks.length;
            r < s;
            r++
          ) {
            for (t = x.chunks[r].crc.toString(16); t.length < 8; ) t = "0" + t;
            (a =
              (a =
                (a += '<tr><td class="n-640 nt"></td>') +
                '<td class="br t">' +
                x.chunks[r].name +
                "</td>") +
              '<td class="br t">' +
              x.chunks[r].length +
              '</td><td class="br t mono upper">' +
              t +
              "</code></td>"),
              (e = "");
            try {
              "" ==
                (e =
                  -1 != n.indexOf(x.chunks[r].name)
                    ? T(x.chunks[r].name, x.getChunk(x.chunks[r].name))
                    : T(x.chunks[r].name)) &&
                -1 != n.indexOf(x.chunks[r].name) &&
                (e = JSON.stringify(x.getChunk(x.chunks[r].name)));
            } catch (t) {}
            a = a + ('<td class="t"><div>' + (e = "" == e ? "parser error" : e)) + "</div></td></tr>";
          }
          L.classList.remove("n");
          var i = _el("#canvas-png");
          (i.innerHTML = a), i.classList.remove("n"), sectionClick(!1);
        })
        .catch(function (t) {
          (t = '<tr><td class="n-640 nt"></td><td colspan="4">' + ico(0) + t + "</td>"),
            L.insertAdjacentHTML("beforeend", t),
            L.classList.remove("n"),
            (_.textContent = "n/a"),
            _.classList.remove("load-td");
        });
    } else (y.textContent = "n/a"), (_.textContent = "n/a"), _.classList.remove("load-td");
  })(),
    (Number.prototype.toFixedNoRounding = function (t) {
      var e = new RegExp("^-?\\d+(?:\\.\\d{0," + t + "})?", "g"),
        n = (e = this.toString().match(e)[0]).indexOf(".");
      return -1 === n
        ? e + "." + Array(t + 1).join("0")
        : 0 < (t = t - (e.length - n) + 1)
        ? e + Array(1 + t).join("0")
        : e;
    });
})();
