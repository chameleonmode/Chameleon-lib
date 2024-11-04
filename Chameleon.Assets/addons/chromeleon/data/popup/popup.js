import { noises, SETTINGS_ARRAY } from "../../modules/settings.js";

document.addEventListener("DOMContentLoaded", async function () {
  let settings = await chrome.storage.sync.get(SETTINGS_ARRAY);
  const toggleExtension = document.getElementById("toggle-extension");
  const webglSpoofing = document.getElementById("webgl-spoofing");
  const canvasProtection = document.getElementById("canvas-protection");
  const clientRectsSpoofing = document.getElementById("client-rects-spoofing");
  const fontsSpoofing = document.getElementById("fonts-spoofing");
  const geoSpoofing = document.getElementById("geo-spoofing");
  const timezoneSpoofing = document.getElementById("timezone-spoofing");
  const noiseLevel = document.getElementById("noise-level");
  const statusText = document.getElementById("status-text");
  const blockedCount = document.getElementById("blocked-count");

  // Load saved settings
  toggleExtension.checked = settings.enabled !== false;
  webglSpoofing.checked = settings.webglSpoofing;
  canvasProtection.checked = settings.canvasProtection;
  clientRectsSpoofing.checked = settings.clientRectsSpoofing;
  fontsSpoofing.checked = settings.fontsSpoofing;
  geoSpoofing.checked = settings.geoSpoofing;
  timezoneSpoofing.checked = settings.timezoneSpoofing;
  noiseLevel.value = settings.noiseLevel || "medium";
  blockedCount.textContent = settings.blockedCount || 0;
  updateStatus();

  // Update status text
  function updateStatus() {
    statusText.textContent = toggleExtension.checked ? "Enabled" : "Disabled";
    statusText.style.color = toggleExtension.checked ? "green" : "red";
  }

  // Save settings and update content scripts
  function saveSettings() {
    settings.enabled = toggleExtension.checked;
    settings.webglSpoofing = webglSpoofing.checked;
    settings.canvasProtection = canvasProtection.checked;
    settings.clientRectsSpoofing = clientRectsSpoofing.checked;
    settings.fontsSpoofing = fontsSpoofing.checked;
    settings.geoSpoofing = geoSpoofing.checked;
    settings.timezoneSpoofing = timezoneSpoofing.checked;
    if (settings.noiseLevel !== noiseLevel.value) {
      settings.noiseLevel = noiseLevel.value;
      // Update rectys noise levels  
      settings.DOMRectnoise =
        1 + (Math.random() < 0.5 ? -1 : +1) * (noises.DOMRect * noises.noiseLevel[settings.noiseLevel]);
      settings.DOMRectReadOnlynoise =
        1 + (Math.random() < 0.5 ? -1 : +1) * (noises.DOMRectReadOnly * noises.noiseLevel[settings.noiseLevel])
      // Update WebGL noise levels
      settings.WebGLnoise = noises.random.randvalue();
      settings.WebGLnoiseAmplitude = settings.noiseLevel === "high"
        ? 0.01
        : settings.noiseLevel === "medium"
        ? 0.001
        : 0.0001;
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
  geoSpoofing.addEventListener("change", saveSettings);
  timezoneSpoofing.addEventListener("change", saveSettings);
  noiseLevel.addEventListener("change", saveSettings);
});
