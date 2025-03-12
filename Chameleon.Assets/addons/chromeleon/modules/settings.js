
export const SETTINGS_ARRAY = [
  "enabled",
  "webglSpoofing",
  "canvasProtection",
  "clientRectsSpoofing",
  "fontsSpoofing",
  "geoSpoofing",
  "timezoneSpoofing",
  "audioSpoofing",
  "dAPI",
  "webRtcEnabled",
  "randomizeTZ",
  "randomizeGeo",
  "noiseLevel",
  "timezone",
  "locale",
  "debug",
  "lat",
  "lon",
  "accuracy",
  "myIP",
  "bypass",
  "history",
  "DOMRectnoise",
  "DOMRectReadOnlynoise",
  "WebGLnoise",
  "WebGLnoiseAmplitude",
  "canvasR",
  "canvasG",
  "canvasB",
  "canvasA",
  "Fontsnoise",
  "Fontssign",
  "randomWebGLSpoofing",
  "randomCanvasSpoofing",
  "randomFontsSpoofing",
  "randomRectsSpoofing",
  "randomAudioSpoofing",
];
export let settings = {
  enabled: true,
  audioSpoofing: true,
  webglSpoofing: true,
  canvasProtection: true,
  clientRectsSpoofing: true,
  fontsSpoofing: true,
  geoSpoofing: true,
  timezoneSpoofing: true,
  webRtcEnabled: true,
  dAPI: true,
  myIP: false,
  randomizeTZ: false,
  randomizeGeo: false,
  noiseLevel: "medium",
  DOMRectnoise: 1,
  DOMRectReadOnlynoise: 1,
  WebGLnoise: 1,
  WebGLnoiseAmplitude: 1,
  canvasR: 1,
  canvasG: 1,
  canvasB: 1,
  canvasA: 1,
  Fontsnoise: 1,
  Fontssign: 1,
  timezone: "America/Los_Angeles",
  lat: 34.052235,
  lon: -118.243683,
  locale: "en-US",
  debug: 4,
  accuracy: 64.0999,
  bypass: [],
  history: [],
  randomWebGLSpoofing: false,
  randomCanvasSpoofing: false,
  randomFontsSpoofing: false,
  randomRectsSpoofing: false,
  randomAudioSpoofing: false,
};
export const noises = {
  noiseLevel: {
    micro: 0.1,
    mini: 0.2,
    low: 0.3,
    medium: 0.4,
    bold: 0.5,
    high: 0.6,
    heavy: 0.7,
    ultra: 0.8,
    super: 0.9,
    max: 1.5,
  },
  canvasNoiseLevels: {
    micro: 0.1,
    mini: 0.4,
    low: 0.8,
    medium: 1.4,
    bold: 1.8,
    high: 2.4,
    ultra: 2.5,
    super: 3.4,
    max: 3.8,
  },
  webglNoiseLevels: {
    micro: 0.0001,
    mini: 0.0002,
    low: 0.0003,
    medium: 0.0004,
    bold: 0.0005,
    high: 0.006,
    ultra: 0.007,
    super: 0.08,
    max: 0.09,
  },
  DOMRect: 0.00000001,
  DOMRectReadOnly: 0.000001,
  random: {
    seed: Math.floor(Math.random() * 1000000),
    noise: {
      DOMRect: 0.00000001,
      DOMRectReadOnly: 0.000001,
    },
    metrics: {
      DOMRect: ["x", "y", "width", "height"],
      DOMRectReadOnly: ["top", "right", "bottom", "left"],
    },
    randvalue: function () {
      let thisseed = (this.seed * 9301 + 49297) % 233280;
      return thisseed / 233280;
    },
    item: function (e) {
      let rand = e.length * this.randvalue();
      return e[Math.floor(rand)];
    },
    number: function (power) {
      let tmp = [];
      for (let i = 0; i < power.length; i++) {
        tmp.push(Math.pow(2, power[i]));
      }
      return this.item(tmp);
    },
    int: function (power) {
      let tmp = [];
      for (let i = 0; i < power.length; i++) {
        let n = Math.pow(2, power[i]);
        tmp.push(new Int32Array([n, n]));
      }
      return this.item(tmp);
    },
    float: function (power) {
      let tmp = [];
      for (let i = 0; i < power.length; i++) {
        let n = Math.pow(2, power[i]);
        tmp.push(new Float32Array([1, n]));
      }
      return this.item(tmp);
    },
  },
};

export async function updateSettings(built) {
  if (built) {
    var current = await chrome.storage.sync.get(SETTINGS_ARRAY);
    if (current) Object.assign(settings, current);

    settings.webRtcEnabled = built.webRtcEnabled;
    settings.dAPI = built.dAPI;
    settings.webglSpoofing = built.webglSpoofing;
    settings.canvasProtection = built.canvasProtection;
    settings.clientRectsSpoofing = built.clientRectsSpoofing;
    settings.fontsSpoofing = built.fontsSpoofing;
    settings.debug = built.debug;
    settings.timezoneSpoofing = built.timezoneSpoofing;
    settings.audioSpoofing = built.audioSpoofing;
    settings.myIP = built.myIP;
    settings.timezone = built.timezone;
    settings.geoSpoofing = built.geoSpoofing;
    settings.lat = built.lat;
    settings.lon = built.lon;
    if (
      settings.DOMRectnoise === 1 ||
      settings.DOMRectReadOnlynoise === 1 ||
      settings.WebGLnoise === 1 ||
      settings.WebGLnoiseAmplitude === 1 ||
      settings.canvasR === 1 ||
      settings.canvasG === 1 ||
      settings.canvasB === 1 ||
      settings.canvasA === 1 ||
      settings.Fontsnoise === 1 ||
      settings.Fontssign === 1
    ) {
      resetSettings(settings);  
    }
  }
  
  await chrome.storage.sync.set(settings);
  settings = await chrome.storage.sync.get(SETTINGS_ARRAY);
}

export async function resetSettings(thesettings) {
  // Update rects noise levels
  thesettings.DOMRectnoise =
    1 +
    (Math.random() < 0.5 ? -1 : +1) *
      (noises.DOMRect * noises.noiseLevel[thesettings.noiseLevel]);
  thesettings.DOMRectReadOnlynoise =
    1 +
    (Math.random() < 0.5 ? -1 : +1) *
      (noises.DOMRectReadOnly * noises.noiseLevel[thesettings.noiseLevel]);

  // Update WebGL noise levels
  thesettings.WebGLnoise = noises.random.randvalue();
  thesettings.WebGLnoiseAmplitude =
    noises.webglNoiseLevels[thesettings.noiseLevel];

  // Update canvas noise levels
  thesettings.canvasR =
    Math.floor(Math.random() * 10) -
    5 * noises.canvasNoiseLevels[thesettings.noiseLevel];
  thesettings.canvasG =
    Math.floor(Math.random() * 10) -
    5 * noises.canvasNoiseLevels[thesettings.noiseLevel];
  thesettings.canvasB =
    Math.floor(Math.random() * 10) -
    5 * noises.canvasNoiseLevels[thesettings.noiseLevel];
  thesettings.canvasA =
    Math.floor(Math.random() * 10) -
    5 * noises.canvasNoiseLevels[thesettings.noiseLevel];

  // Update fonts noise levels
  const SIGN = Math.random() < Math.random() ? -1 : 1;
  thesettings.Fontsnoise =
    Math.floor(Math.random() + SIGN * Math.random()) *
    noises.canvasNoiseLevels[thesettings.noiseLevel];

  const tmp = [-1, -1, -1, -1, -1, -1, +1, -1, -1, -1];
  const index = Math.floor(Math.random() * tmp.length);
  thesettings.Fontssign = tmp[index];
}