import { noises, SETTINGS_ARRAY } from "../../modules/settings.js";

document.addEventListener("DOMContentLoaded", async function () {
  let settings = await browser.storage.sync.get(SETTINGS_ARRAY);
  const toggleExtension = document.getElementById("toggle-extension");
  const clientRectsSpoofing = document.getElementById("client-rects-spoofing");
  const noiseLevel = document.getElementById("noise-level");
  const statusText = document.getElementById("status-text");
  const blockedCount = document.getElementById("blocked-count");

  // Load saved settings
    toggleExtension.checked = settings.enabled !== false;
    clientRectsSpoofing.checked = settings.clientRectsSpoofing !== false;
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
    settings.clientRectsSpoofing = clientRectsSpoofing.checked;
    if (settings.noiseLevel !== noiseLevel.value) {
      settings.noiseLevel = noiseLevel.value;
      // Update rectys noise levels
      settings.DOMRectnoise =
        1 +
        (Math.random() < 0.5 ? -1 : +1) *
          (noises.DOMRect * noises.noiseLevel[settings.noiseLevel]);
      settings.DOMRectReadOnlynoise =
        1 +
        (Math.random() < 0.5 ? -1 : +1) *
          (noises.DOMRectReadOnly * noises.noiseLevel[settings.noiseLevel]);
    }

    chrome.storage.sync.set(settings, function () {
      updateStatus();
    });
  }

  // Event listeners
  toggleExtension.addEventListener("change", saveSettings);
  clientRectsSpoofing.addEventListener("change", saveSettings);
  noiseLevel.addEventListener("change", saveSettings);
});
