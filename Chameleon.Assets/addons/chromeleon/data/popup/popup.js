import { noises, SETTINGS_ARRAY } from "../../modules/settings.js";
import { offsets } from "../../modules/offsets.js";

document.addEventListener("DOMContentLoaded", async function () {
  let settings = await chrome.storage.sync.get(SETTINGS_ARRAY);
  const toggleExtension = document.getElementById("toggle-extension");
  const webglSpoofing = document.getElementById("webgl-spoofing");
  const canvasProtection = document.getElementById("canvas-protection");
  const clientRectsSpoofing = document.getElementById("client-rects-spoofing");
  const fontsSpoofing = document.getElementById("fonts-spoofing");
  const randomWebGLSpoofing = document.getElementById("random-webgl-spoofing");
  const randomCanvasSpoofing = document.getElementById("random-canvas-spoofing");
  const randomFontsSpoofing = document.getElementById("random-fonts-spoofing");
  const randomRectsSpoofing = document.getElementById("random-rects-spoofing");
  const noiseLevel = document.getElementById("noise-level");
  const statusText = document.getElementById("status-text");
  const blockedCount = document.getElementById("blocked-count");
  const timezoneSpoofing = document.getElementById("timezone-spoofing");
  const randomizeTimezone = document.getElementById("randomize-timezone");
  const timezoneSelect = document.getElementById("timezone-select");
  const timezoneOptions = document.getElementById("timezone-options");
  
  // Geolocation elements
  const geoSpoofing = document.getElementById("geo-spoofing");
  const randomizeGeo = document.getElementById("randomize-geo");
  const geoAccuracy = document.getElementById("geo-accuracy");
  const latitude = document.getElementById("latitude");
  const longitude = document.getElementById("longitude");

  // Populate timezone datalist options
  Object.keys(offsets).forEach((zone) => {
    const offset = offsets[zone].offset;
    const offsetHours = offset / 60;
    const option = document.createElement("option");
    option.value = zone;
    option.text = `${zone} (GMT${offsetHours > 0 ? "+" : ""}${offsetHours})`;
    timezoneOptions.appendChild(option);
  });

  // Load saved settings
  toggleExtension.checked = settings.enabled !== false;
  webglSpoofing.checked = settings.webglSpoofing;
  canvasProtection.checked = settings.canvasProtection;
  clientRectsSpoofing.checked = settings.clientRectsSpoofing;
  fontsSpoofing.checked = settings.fontsSpoofing;
  randomWebGLSpoofing.checked = settings.randomWebGLSpoofing;
  randomCanvasSpoofing.checked = settings.randomCanvasSpoofing;
  randomFontsSpoofing.checked = settings.randomFontsSpoofing;
  randomRectsSpoofing.checked = settings.randomRectsSpoofing;
  noiseLevel.value = settings.noiseLevel || "medium";
  blockedCount.textContent = settings.blockedCount || 0;
  timezoneSpoofing.checked = settings.timezoneSpoofing;
  randomizeTimezone.checked = settings.randomizeTZ;
  timezoneSelect.value = settings.timezone;
  
  // Load geolocation settings
  geoSpoofing.checked = settings.geoSpoofing;
  randomizeGeo.value = settings.randomizeGeo === false ? "false" : settings.randomizeGeo?.toString() || "false";
  geoAccuracy.value = settings.accuracy?.toString() || "64.0999";
  latitude.value = settings.latitude || "";
  longitude.value = settings.longitude || "";
  
  updateStatus();

  // Update status text
  function updateStatus() {
    statusText.textContent = toggleExtension.checked ? "Enabled" : "Disabled";
    updateChecked(clientRectsSpoofing, randomRectsSpoofing);
    updateChecked(webglSpoofing, randomWebGLSpoofing);
    updateChecked(canvasProtection, randomCanvasSpoofing);
    updateChecked(fontsSpoofing, randomFontsSpoofing);
    updateChecked(timezoneSpoofing, randomizeTimezone);
    updateChecked(timezoneSpoofing, timezoneSelect);
    updateChecked(geoSpoofing, randomizeGeo);
    updateChecked(geoSpoofing, geoAccuracy);
    updateChecked(geoSpoofing, latitude);
    updateChecked(geoSpoofing, longitude);
  }

  function updateChecked(element, toggle) {
    const randomToggle = toggle.parentElement;
    if (!element.checked) {
      randomToggle.classList.add('disabled');
      toggle.disabled = true;
      if (toggle.type === 'checkbox') {
        toggle.checked = false;
      }
    } else {
      randomToggle.classList.remove('disabled');
      toggle.disabled = false;
    }
  }

  // Save settings and update content scripts
  function saveSettings() {
    settings.enabled = toggleExtension.checked;
    settings.webglSpoofing = webglSpoofing.checked;
    settings.canvasProtection = canvasProtection.checked;
    settings.clientRectsSpoofing = clientRectsSpoofing.checked;
    settings.fontsSpoofing = fontsSpoofing.checked;
    settings.randomWebGLSpoofing = randomWebGLSpoofing.checked;
    settings.randomCanvasSpoofing = randomCanvasSpoofing.checked;
    settings.randomFontsSpoofing = randomFontsSpoofing.checked;
    settings.randomRectsSpoofing = randomRectsSpoofing.checked;
    settings.timezoneSpoofing = timezoneSpoofing.checked;
    settings.myIP = !settings.timezoneSpoofing;
    settings.randomizeTZ = randomizeTimezone.checked;
    
    // Save geolocation settings
    settings.geoSpoofing = geoSpoofing.checked;
    settings.randomizeGeo = randomizeGeo.value === "false" ? false : parseFloat(randomizeGeo.value);
    settings.accuracy = parseFloat(geoAccuracy.value);
    settings.latitude = latitude.value ? parseFloat(latitude.value) : null;
    settings.longitude = longitude.value ? parseFloat(longitude.value) : null;
    
    // Extract timezone value from input (remove GMT offset if present)
    settings.timezone = timezoneSelect.value;

    if (settings.noiseLevel !== noiseLevel.value) {
      settings.noiseLevel = noiseLevel.value;
      // Update rects noise levels
      settings.DOMRectnoise =
        1 +
        (Math.random() < 0.5 ? -1 : +1) *
          (noises.DOMRect * noises.noiseLevel[settings.noiseLevel]);
      settings.DOMRectReadOnlynoise =
        1 +
        (Math.random() < 0.5 ? -1 : +1) *
          (noises.DOMRectReadOnly * noises.noiseLevel[settings.noiseLevel]);

      // Update WebGL noise levels
      settings.WebGLnoise = noises.random.randvalue();
      settings.WebGLnoiseAmplitude = noises.noiseLevel[settings.noiseLevel];
          
      // Update canvas noise levels
      const noiseAmplitude =
        settings.noiseLevel === "high"
          ? 2
          : settings.noiseLevel === "medium"
          ? 1
          : 0.5;
      settings.canvasR = Math.floor(Math.random() * 10) - 5 * noiseAmplitude;
      settings.canvasG = Math.floor(Math.random() * 10) - 5 * noiseAmplitude;
      settings.canvasB = Math.floor(Math.random() * 10) - 5 * noiseAmplitude;
      settings.canvasA = Math.floor(Math.random() * 10) - 5 * noiseAmplitude;

      const SIGN = Math.random() < Math.random() ? -1 : 1;
      settings.Fontsnoise = Math.floor(Math.random() + SIGN * Math.random()) * noiseAmplitude;
    
      const tmp = [-1, -1, -1, -1, -1, -1, +1, -1, -1, -1];
      const index = Math.floor(Math.random() * tmp.length);
      settings.Fontssign = tmp[index];
    }

    chrome.storage.sync.set(settings, function () {
      updateStatus();
    });
  }

  // Event listeners
  toggleExtension.addEventListener("change", saveSettings);
  webglSpoofing.addEventListener("change", saveSettings);
  canvasProtection.addEventListener("change", saveSettings);
  clientRectsSpoofing.addEventListener("change", saveSettings);
  fontsSpoofing.addEventListener("change", saveSettings);
  randomWebGLSpoofing.addEventListener("change", saveSettings);
  randomCanvasSpoofing.addEventListener("change", saveSettings);
  randomFontsSpoofing.addEventListener("change", saveSettings);
  randomRectsSpoofing.addEventListener("change", saveSettings);
  noiseLevel.addEventListener("change", saveSettings);
  timezoneSpoofing.addEventListener("change", saveSettings);
  randomizeTimezone.addEventListener("change", saveSettings);
  timezoneSelect.addEventListener("change", saveSettings);
  timezoneSelect.addEventListener("input", saveSettings);

  // Geolocation event listeners
  geoSpoofing.addEventListener("change", saveSettings);
  randomizeGeo.addEventListener("change", saveSettings);
  geoAccuracy.addEventListener("change", saveSettings);
  latitude.addEventListener("change", saveSettings);
  longitude.addEventListener("change", saveSettings);
  latitude.addEventListener("input", saveSettings);
  longitude.addEventListener("input", saveSettings);
});
