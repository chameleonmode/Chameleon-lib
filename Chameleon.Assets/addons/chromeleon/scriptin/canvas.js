// Version: 2.0
window.canvasNoiseLevels = {
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
window.noiseLevel = "max";
window.randomCanvasSpoofing = true;
window.canvasProtection = true;
window.enabled = true;
window.canvasR = Math.floor(Math.random() * 10) - 5 * 3.8; //window.canvasNoiseLevels[window.noiseLevel];
window.canvasG = Math.floor(Math.random() * 10) - 5 * 3.8; //window.canvasNoiseLevels[window.noiseLevel];
window.canvasB = Math.floor(Math.random() * 10) - 5 * 3.8; //window.canvasNoiseLevels[window.noiseLevel];
window.canvasR = 1;
window.canvasG = 1;
window.canvasB = 1;

//https://privacycheck.sec.lrz.de/active/fp_c/fp_canvas.html
const getImageData = CanvasRenderingContext2D.prototype.getImageData;
//
const noisify = function (canvas, context) {
  if (context) {
    const shift = {
      r: Math.floor(Math.random() * 10) - 5 * 3.8,
      g: Math.floor(Math.random() * 10) - 5 * 3.8,
      b: Math.floor(Math.random() * 10) - 5 * 3.8,
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

// HTMLCanvasElement.prototype.toBlob = new Proxy(HTMLCanvasElement.prototype.toBlob, {
//   apply(target, window, args) {
//     noisify(window, window.getContext("2d", { willReadFrequently: true }));
//     //
//     return Reflect.apply(target, window, args);
//   },
// });
// //
// HTMLCanvasElement.prototype.toDataURL = new Proxy(HTMLCanvasElement.prototype.toDataURL, {
//   apply(target, window, args) {
//     noisify(window, window.getContext("2d", { willReadFrequently: true }));
//     //
//     return Reflect.apply(target, window, args);
//   },
// });
// //
// CanvasRenderingContext2D.prototype.getImageData = new Proxy(
//   CanvasRenderingContext2D.prototype.getImageData,
//   {
//     apply(target, window, args) {
//       noisify(window.canvas, window);
//       //
//       return Reflect.apply(target, window, args);
//     },
//   }
// );

CanvasRenderingContext2D.prototype.getImageData = function (x, y, w, h) {
  noisify(window.canvas, window);
  //
  return getImageData.apply(window, [x, y, w, h]);
}

HTMLCanvasElement.prototype.toBlob = function (callback, type, quality) {
  //
  noisify(window, self.getContext("2d", { willReadFrequently: true }));
  //
  return HTMLCanvasElement.prototype.toBlob.apply(window, [callback, type, quality]);
}

HTMLCanvasElement.prototype.toDataURL = function (type, quality) {
  //
  noisify(window, self.getContext("2d", { willReadFrequently: true }));
  //
  return HTMLCanvasElement.prototype.toDataURL.apply(window, [type, quality]);
}

window.CanvasRenderingContext2D.prototype.getImageData = CanvasRenderingContext2D.prototype.getImageData;
window.HTMLCanvasElement.prototype.toBlob = HTMLCanvasElement.prototype.toBlob;
window.HTMLCanvasElement.prototype.toDataURL = HTMLCanvasElement.prototype.toDataURL;


/*! canvas.js */
!function() {
  var y = _el("#canvas-hash")
    , _ = _el("#canvas-ratio")
    , L = _el("#canvas-file")
    , b = _el("#canvas-table");
  function T(t, e) {
      var n = "";
      return "IHDR" == t ? (n = "PNG image header: ",
      n = (n += e.width + "x" + e.height + ", ") + e.depth + " bits/sample, ",
      0 == e.type ? n += "grayscale, " : 2 == e.type ? n += "truecolor, " : 3 == e.type ? n += "paletted, " : 4 == e.type ? n += "grayscale+alpha, " : 6 == e.type && (n += "truecolor+alpha, "),
      "0" == e.interlaced ? n += "noninterlaced, " : "1" == e.interlaced && (n += "interlaced, "),
      n = n.slice(0, -2)) : "gAMA" == t ? n = "file gamma = : " + e.gamma : "sRGB" == t ? n = "sRGB color space, rendering intent: " + e.desc : "IDAT" == t ? n = "PNG image data" : "IEND" == t && (n = "end-of-image marker"),
      n
  }
  (function e() {
      _.classList.add("load-td");
      var n, a, v, r = !0, s = ico(0) + "False", i = ico(0) + "False", t = ico(0) + "False", o = "BrowserLeaks,com <canvas> 1.0";
      if ((c = _el("#canvas-iframe").contentDocument.createElement("canvas")).getContext && (n = c.getContext("2d"))) {
          if (s = ico(1) + "True",
          "function" == typeof c.getContext("2d").fillText) {
              i = ico(1) + "True";
              try {
                  c.setAttribute("width", 220),
                  c.setAttribute("height", 30),
                  n.textBaseline = "top",
                  n.font = "14px 'Arial'",
                  n.textBaseline = "alphabetic",
                  n.fillStyle = "#f60",
                  n.fillRect(125, 1, 62, 20),
                  n.fillStyle = "#069",
                  n.fillText(o, 2, 15),
                  n.fillStyle = "rgba(102, 204, 0, 0.7)",
                  n.fillText(o, 4, 17)
              } catch (t) {
                  void 0 === (n = (c = document.createElement("canvas")).getContext("2d")) || "function" != typeof c.getContext("2d").fillText ? (s = ico(0) + "False",
                  i = ico(0) + "False",
                  r = !1) : (c.setAttribute("width", 220),
                  c.setAttribute("height", 30),
                  n.textBaseline = "top",
                  n.font = "14px 'Arial'",
                  n.textBaseline = "alphabetic",
                  n.fillStyle = "#f60",
                  n.fillRect(125, 1, 62, 20),
                  n.fillStyle = "#069",
                  n.fillText(o, 2, 15),
                  n.fillStyle = "rgba(102, 204, 0, 0.7)",
                  n.fillText(o, 4, 17))
              }
          } else
              r = !1;
          if (r && "function" == typeof c.toDataURL) {
              try {
                  if ("boolean" == typeof (a = c.toDataURL("image/png")) || void 0 === a)
                      throw 1
              } catch (t) {
                  a = ""
              }
              0 === a.indexOf("data:image/png") ? t = ico(1) + "True" : r = !1
          } else
              r = !1
      } else
          r = !1;
      if (_el("#canvas-support-2d").innerHTML = s,
      _el("#canvas-support-text").innerHTML = i,
      _el("#canvas-support-todataurl").innerHTML = t,
      r) {
          var o = n
            , l = a
            , c = function() {
              var e = atob(l.split(",")[1])
                , n = new Uint8Array(e.length);
              for (let t = 0; t < e.length; t++)
                  n[t] = e.charCodeAt(t);
              return n
          }()
            , s = md5(c)
            , d = (y.textContent = s,
          y.className = "wball mono upper",
          v = s,
          fetch("/api/canvas/" + v).then(function(t) {
              return t.json()
          }).then(function(t) {
              if (void 0 !== t[v] && "not_listed" !== t[v]) {
                  _.textContent = function(t, e) {
                      for (var n, a = 2; n = 100 - (100 * t / e).toFixed(a),
                      a++,
                      "100" == n && 0 < t; )
                          ;
                      return (n = 5 < n.toString().length ? n.toFixedNoRounding(2) : n) + "% (" + t + " of " + e + " user agents have the same signature)"
                  }(t[v].count, t[v].total),
                  _el("#canvas-verdict-ua").textContent = t[v].ua[0][0],
                  _el("#canvas-verdict-os").textContent = t[v].os[0][0],
                  _el("#canvas-verdict").classList.remove("n");
                  var e, n = t[v], a = 0, r = [];
                  for (e in n)
                      r[e] = n[e].length,
                      10 < r[e] && (r[e] = n[e].length = 10),
                      a < r[e] && (a = r[e]);
                  (a = r.ua + r.ua_ver > (a = r.os + r.os_ver) ? r.ua + r.ua_ver : a) < r.device + r.platform && (a = r.device + r.platform),
                  a += 1;
                  for (var s = '<tr><td class="th r" colspan="2"><h3>Operating Systems</h3></td><td class="th r" colspan="2"><h3>Browsers</h3></td><td class="th" colspan="2"><h3>Devices</h3></td></tr>', i = 0, o = 0, l = 0, c = 0, d = 0, f = 0, u = 0; u < a; u++)
                      s += "<tr>",
                      void 0 !== n.os_ver[u] ? (i++,
                      s += "<td>" + n.os_ver[u][0] + '</td><td class="r">' + n.os_ver[u][1] + "/" + n.count + "</td>") : void 0 !== n.engine_ver[u - i] ? 0 == o ? (i++,
                      o = 1,
                      s += '<td class="t th r" colspan="2"><h3>Engines</h3></td>') : s += "<td>" + n.engine_ver[u - i][0] + '</td><td class="r">' + n.engine_ver[u - i][1] + "/" + n.count + "</td>" : s += '<td></td><td class="r"></td>',
                      void 0 !== n.ua[u] ? (l++,
                      s += "<td>" + n.ua[u][0] + '</td><td class="r">' + n.ua[u][1] + "/" + n.count + "</td>") : void 0 !== n.ua_ver[u - l] ? 0 == c ? (l++,
                      c = 1,
                      s += '<td class="t th r" colspan="2"><h3>Browsers by Version</h3></td>') : s += "<td>" + n.ua_ver[u - l][0] + '</td><td class="r">' + n.ua_ver[u - l][1] + "/" + n.count + "</td>" : s += '<td></td><td class="r"></td>',
                      void 0 !== n.device[u] ? (d++,
                      s += "<td>" + n.device[u][0] + "</td><td>" + n.device[u][1] + "/" + n.count + "</td>") : void 0 !== n.platform[u - d] ? 0 == f ? (d++,
                      f = 1,
                      s += '<td class="t th" colspan="2"><h3>Platforms</h3></td>') : s += "<td>" + n.platform[u - d][0] + "</td><td>" + n.platform[u - d][1] + "/" + n.count + "</td>" : s += "<td></td><td></td>",
                      s += "</tr>";
                  b.insertAdjacentHTML("beforeend", s),
                  b.style.marginBottom = "2px",
                  [b, _el("#canvas-stats")].forEach(function(t) {
                      t.classList.remove("n")
                  }),
                  sectionClick(!1)
              } else
                  _.textContent = "100% (The signature is unique to our database)"
          }).catch(function(t) {
              _.innerHTML = ico(0) + 'Data retrieval error <a id="canvas-retry" href="#">[retry]</a>',
              _el("#canvas-retry").addEventListener("click", function(t) {
                  t.preventDefault(),
                  _.classList.add("load-td"),
                  e()
              })
          }).finally(function() {
              _.classList.remove("load-td")
          }),
          _el("#canvas-img").innerHTML = '<img src="' + l + '" alt="&nbsp;Error displaying &lt;img&gt; tag">',
          0);
          try {
              for (var f = o.getImageData(0, 0, 220, 30), u = new Uint32Array(f.data.buffer), h = u.length, g = {}, p = 0; p < h; p++) {
                  var m = "" + (16777215 & u[p]);
                  g[m] || (d++,
                  g[m] = 0),
                  g[m]++
              }
          } catch (t) {}
          d < 1 && (d = "n/a"),
          _el("#canvas-file-colors").textContent = d,
          _el("#canvas-file-size").textContent = c.length + " bytes";
          var x = new PngToy([{
              doCRC: "true"
          }]);
          x.fetch(c).then(function() {
              if (2303741511 !== x.view.getUint32(0) || 218765834 !== x.view.getUint32(4))
                  throw new Error("Not a PNG file.");
              for (var t, e, n = "IHDR,PLTE,sPLT,tRNS,tEXt,gAMA,cHRM,sRGB,hIST,pHYs,bKGD,tIME,sBIT,oFFs,sTER,sCAL,pCAL", a = "", r = 0, s = x.chunks.length; r < s; r++) {
                  for (t = x.chunks[r].crc.toString(16); t.length < 8; )
                      t = "0" + t;
                  a = (a = (a += '<tr><td class="n-640 nt"></td>') + '<td class="br t">' + x.chunks[r].name + "</td>") + '<td class="br t">' + x.chunks[r].length + '</td><td class="br t mono upper">' + t + "</code></td>",
                  e = "";
                  try {
                      "" == (e = -1 != n.indexOf(x.chunks[r].name) ? T(x.chunks[r].name, x.getChunk(x.chunks[r].name)) : T(x.chunks[r].name)) && -1 != n.indexOf(x.chunks[r].name) && (e = JSON.stringify(x.getChunk(x.chunks[r].name)))
                  } catch (t) {}
                  a = a + ('<td class="t"><div>' + (e = "" == e ? "parser error" : e)) + "</div></td></tr>"
              }
              L.classList.remove("n");
              var i = _el("#canvas-png");
              i.innerHTML = a,
              i.classList.remove("n"),
              sectionClick(!1)
          }).catch(function(t) {
              t = '<tr><td class="n-640 nt"></td><td colspan="4">' + ico(0) + t + "</td>",
              L.insertAdjacentHTML("beforeend", t),
              L.classList.remove("n"),
              _.textContent = "n/a",
              _.classList.remove("load-td")
          })
      } else
          y.textContent = "n/a",
          _.textContent = "n/a",
          _.classList.remove("load-td")
  }
  )(),
  Number.prototype.toFixedNoRounding = function(t) {
      var e = new RegExp("^-?\\d+(?:\\.\\d{0," + t + "})?","g")
        , n = (e = this.toString().match(e)[0]).indexOf(".");
      return -1 === n ? e + "." + Array(t + 1).join("0") : 0 < (t = t - (e.length - n) + 1) ? e + Array(1 + t).join("0") : e
  }
}();