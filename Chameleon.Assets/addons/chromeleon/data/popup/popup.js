import { resetSettings, SETTINGS_ARRAY } from "../../modules/settings.js";
import { offsets } from "../../modules/offsets.js";

document.addEventListener("DOMContentLoaded", async function () {
  let settings = await chrome.storage.sync.get(SETTINGS_ARRAY);
  const toggleExtension = document.getElementById("toggle-extension");
  const statusText = document.getElementById("status-text");
  const noiseLevelSlider = document.getElementById("noise-level-slider");
  const webglSpoofing = document.getElementById("webgl-spoofing");
  const randomWebGLSpoofing = document.getElementById("random-webgl-spoofing");
  const canvasProtection = document.getElementById("canvas-protection");
  const randomCanvasSpoofing = document.getElementById(
    "random-canvas-spoofing"
  );
  const clientRectsSpoofing = document.getElementById("client-rects-spoofing");
  const randomRectsSpoofing = document.getElementById("random-rects-spoofing");
  const fontsSpoofing = document.getElementById("fonts-spoofing");
  const randomFontsSpoofing = document.getElementById("random-fonts-spoofing");
  const audioSpoofing = document.getElementById("audio-spoofing");
  const randomAudioSpoofing = document.getElementById("random-audio-spoofing");
  const blockedCount = document.getElementById("blocked-count");
  const timezoneSpoofing = document.getElementById("timezone-spoofing");
  const randomizeTimezone = document.getElementById("randomize-timezone");
  const timezoneSelect = document.getElementById("timezone-select");
  const timezoneOptions = document.getElementById("timezone-options");
  const refreshButton = document.getElementById("refresh-button");

  // Geolocation elements
  const geoSpoofing = document.getElementById("geo-spoofing");
  const randomizeGeo = document.getElementById("randomize-geo");
  const randomizeGeoValue = document.getElementById("randomize-geo-value");
  const geoAccuracy = document.getElementById("geo-accuracy");
  const geoAccuracyValue = document.getElementById("geo-accuracy-value");
  const coordinates = document.getElementById("coordinates");

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
  toggleExtension.checked = settings.enabled;
  noiseLevelSlider.value = getNoiseLevelNumber(settings.noiseLevel);
  blockedCount.textContent = settings.blockedCount || 0;
  webglSpoofing.checked = settings.webglSpoofing;
  canvasProtection.checked = settings.canvasProtection;
  clientRectsSpoofing.checked = settings.clientRectsSpoofing;
  fontsSpoofing.checked = settings.fontsSpoofing;
  audioSpoofing.checked = settings.audioSpoofing;
  randomAudioSpoofing.checked = settings.randomAudioSpoofing;
  randomWebGLSpoofing.checked = settings.randomWebGLSpoofing;
  randomCanvasSpoofing.checked = settings.randomCanvasSpoofing;
  randomFontsSpoofing.checked = settings.randomFontsSpoofing;
  randomRectsSpoofing.checked = settings.randomRectsSpoofing;
  timezoneSpoofing.checked = settings.timezoneSpoofing;
  randomizeTimezone.checked = settings.randomizeTZ;

  // Load geolocation settings
  geoSpoofing.checked = settings.geoSpoofing;
  // Convert saved randomizeGeo value to slider value
  const randomizeGeoValues = [
    "false",
    "0.1",
    "0.01",
    "0.001",
    "0.0001",
    "0.00001",
  ];
  const savedRandomizeGeo =
    settings.randomizeGeo === false
      ? "false"
      : settings.randomizeGeo?.toString() || "false";
  randomizeGeo.value = randomizeGeoValues.indexOf(savedRandomizeGeo);

  // Convert saved accuracy value to slider value
  geoAccuracy.value = settings.accuracy || 64.0999;
  
  // Set coordinates value if both latitude and longitude exist
  if (settings.latitude !== null && settings.longitude !== null) {
    coordinates.value = `${settings.latitude},${settings.longitude}`;
  }

  // Set timezone value
  timezoneSelect.value = settings.timezone;

  // Update status text
  function updateStatus() {
    statusText.textContent = toggleExtension.checked ? "Enabled" : "Disabled";
    updateSliderDisplays();
    function updateChecked(element, toggle) {
      const randomToggle = toggle.parentElement;
      if (!element.checked) {
        randomToggle.classList.add("disabled");
        toggle.disabled = true;
        if (toggle.type === "checkbox") {
          toggle.checked = false;
        }
      } else {
        randomToggle.classList.remove("disabled");
        toggle.disabled = false;
      }
    }
    updateChecked(clientRectsSpoofing, randomRectsSpoofing);
    updateChecked(webglSpoofing, randomWebGLSpoofing);
    updateChecked(canvasProtection, randomCanvasSpoofing);
    updateChecked(fontsSpoofing, randomFontsSpoofing);
    updateChecked(audioSpoofing, randomAudioSpoofing);
    updateChecked(timezoneSpoofing, randomizeTimezone);
    updateChecked(timezoneSpoofing, timezoneSelect);
    updateChecked(geoSpoofing, randomizeGeo);
    updateChecked(geoSpoofing, geoAccuracy);
    updateChecked(geoSpoofing, coordinates);
  }

  function getNoiseLevelLabel(value) {
    switch (value) {
      case "1":
        return "micro";
      case "2":
        return "mini";
      case "3":
        return "low";
      case "4":
        return "medium";
      case "5":
        return "bold";
      case "6":
        return "high";
      case "7":
        return "heavy";
      case "8":
        return "ultra";
      case "9":
        return "super";
      case "10":
        return "max";
      default:
        return "medium";
    }
  }

  function getNoiseLevelNumber(label) {
    switch (label.toString()) {
      case "micro":
        return "1";
      case "mini":
        return "2";
      case "low":
        return "3";
      case "medium":
        return "4";
      case "bold":
        return "5";
      case "high":
        return "6";
      case "heavy":
        return "7";
      case "ultra":
        return "8";
      case "super":
        return "9";
      case "max":
        return "10";
      default:
        return "4";
    }
  }

  // Helper function to convert randomize-geo slider value to actual value
  function getRandomizeGeoValue(value) {
    const values = ["false", "0.1", "0.01", "0.001", "0.0001", "0.00001"];
    return values[parseInt(value)];
  }

  // Helper function to get display text for randomize-geo value
  function getRandomizeGeoDisplayValue(value) {
    return value === "false" ? "Disabled" : value;
  }

  // Helper function to convert geo-accuracy slider value to actual value
  function getGeoAccuracyValue(value) {
    return value.toFixed(4);
  }

  // Helper function to parse coordinates string
  function parseCoordinates(coordStr) {
    if (!coordStr) return { lat: null, lng: null };
    const [lat, lng] = coordStr.split(',').map(str => {
      const num = parseFloat(str.trim());
      return isNaN(num) ? null : num;
    });
    return { lat, lng };
  }

  // Update slider display values
  function updateSliderDisplays() {
    randomizeGeoValue.textContent = getRandomizeGeoDisplayValue(
      getRandomizeGeoValue(randomizeGeo.value)
    );
    geoAccuracyValue.textContent = getGeoAccuracyValue(
      parseFloat(geoAccuracy.value)
    );
  }

  // Save settings and update content scripts
  function saveSettings() {
    settings.enabled = toggleExtension.checked;
    settings.webglSpoofing = webglSpoofing.checked;
    settings.canvasProtection = canvasProtection.checked;
    settings.clientRectsSpoofing = clientRectsSpoofing.checked;
    settings.fontsSpoofing = fontsSpoofing.checked;
    settings.audioSpoofing = audioSpoofing.checked;
    settings.randomAudioSpoofing = randomAudioSpoofing.checked;
    settings.randomWebGLSpoofing = randomWebGLSpoofing.checked;
    settings.randomCanvasSpoofing = randomCanvasSpoofing.checked;
    settings.randomFontsSpoofing = randomFontsSpoofing.checked;
    settings.randomRectsSpoofing = randomRectsSpoofing.checked;
    settings.timezoneSpoofing = timezoneSpoofing.checked;
    settings.myIP = !settings.timezoneSpoofing;
    settings.randomizeTZ = randomizeTimezone.checked;

    // Save geolocation settings
    settings.geoSpoofing = geoSpoofing.checked;
    const randomizeGeoVal = getRandomizeGeoValue(randomizeGeo.value);
    settings.randomizeGeo =
      randomizeGeoVal === "false" ? false : parseFloat(randomizeGeoVal);
    settings.accuracy = parseFloat(geoAccuracy.value);
    
    // Parse and save coordinates
    const { lat, lng } = parseCoordinates(coordinates.value);
    settings.latitude = lat;
    settings.longitude = lng;

    // Extract timezone value from input (remove GMT offset if present)
    settings.timezone = timezoneSelect.value;

    var noise = getNoiseLevelLabel(noiseLevelSlider.value);
    if (settings.noiseLevel !== noise) {
      settings.noiseLevel = noise;
      noiseLevelSlider.value = getNoiseLevelNumber(settings.noiseLevel);
      resetSettings(settings);
    }

    chrome.storage.sync.set(settings, function () {
      updateStatus();
    });
  }

  updateStatus();

  // Event listeners
  toggleExtension.addEventListener("change", saveSettings);
  noiseLevelSlider.addEventListener("input", saveSettings);
  webglSpoofing.addEventListener("change", saveSettings);
  canvasProtection.addEventListener("change", saveSettings);
  clientRectsSpoofing.addEventListener("change", saveSettings);
  fontsSpoofing.addEventListener("change", saveSettings);
  audioSpoofing.addEventListener("change", saveSettings);
  randomAudioSpoofing.addEventListener("change", saveSettings);
  randomWebGLSpoofing.addEventListener("change", saveSettings);
  randomCanvasSpoofing.addEventListener("change", saveSettings);
  randomFontsSpoofing.addEventListener("change", saveSettings);
  randomRectsSpoofing.addEventListener("change", saveSettings);
  timezoneSpoofing.addEventListener("change", saveSettings);
  randomizeTimezone.addEventListener("change", saveSettings);
  timezoneSelect.addEventListener("change", saveSettings);
  timezoneSelect.addEventListener("input", saveSettings);

  // Geolocation event listeners
  geoSpoofing.addEventListener("change", saveSettings);
  randomizeGeo.addEventListener("input", saveSettings);
  geoAccuracy.addEventListener("input", saveSettings);
  coordinates.addEventListener("change", saveSettings);
  coordinates.addEventListener("input", saveSettings);

  // Refresh button event listener
  refreshButton.addEventListener("click", saveSettings);
});
