import { noises, SETTINGS_ARRAY } from "../../modules/settings.js";

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
  updateStatus();

  // Update status text
  function updateStatus() {
    statusText.textContent = toggleExtension.checked ? "Enabled" : "Disabled";
    // statusText.style.color = toggleExtension.checked ? "green" : "red";
    updateChecked(clientRectsSpoofing, randomRectsSpoofing);
    updateChecked(webglSpoofing, randomWebGLSpoofing);
    updateChecked(canvasProtection, randomCanvasSpoofing);
    updateChecked(fontsSpoofing, randomFontsSpoofing);
  }

  function updateChecked(element, toggle){
    const randomToggle = toggle.parentElement;
    if(!element.checked){
      randomToggle.classList.add('disabled');
      toggle.disabled = true;
      toggle.checked = false;
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
});
