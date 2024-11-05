import { setDomRectsNoises, SETTINGS_ARRAY } from "../../modules/settings.js";

document.addEventListener("DOMContentLoaded", async function () {
  let settings = await browser.storage.sync.get(SETTINGS_ARRAY);
  const toggleExtension = document.getElementById("toggle-extension");
  const clientRectsSpoofing = document.getElementById("client-rects-spoofing");
  const randomRectsSpoofing = document.getElementById("random-rects-spoofing");
  const noiseLevel = document.getElementById("noise-level");
  const statusText = document.getElementById("status-text");
  const blockedCount = document.getElementById("blocked-count");

  // Load saved settings
    toggleExtension.checked = settings.enabled;
    clientRectsSpoofing.checked = settings.clientRectsSpoofing;
    randomRectsSpoofing.checked = settings.randomRectsSpoofing;
    noiseLevel.value = settings.noiseLevel || "medium";
    blockedCount.textContent = settings.blockedCount || 0;
    updateStatus();

  // Update status text
  function updateStatus() {
    statusText.textContent = toggleExtension.checked ? "Enabled" : "Disabled";
    // statusText.style.color = toggleExtension.checked ? "green" : "red";
    const randomToggle = randomRectsSpoofing.parentElement;
    if (!clientRectsSpoofing.checked) {
      randomToggle.classList.add('disabled');
      randomRectsSpoofing.disabled = true;
      randomRectsSpoofing.checked = false;
    } else {
      randomToggle.classList.remove('disabled');
      randomRectsSpoofing.disabled = false;
    }
  }

  // Save settings and update content scripts
  function saveSettings() {
    settings.enabled = toggleExtension.checked;
    settings.clientRectsSpoofing = clientRectsSpoofing.checked;
    settings.randomRectsSpoofing = randomRectsSpoofing.checked;
    if (settings.noiseLevel !== noiseLevel.value) {
      settings.noiseLevel = noiseLevel.value;
      setDomRectsNoises(settings);
    }

    chrome.storage.sync.set(settings, function () {
      updateStatus();
    });
  }

  // Event listeners
  toggleExtension.addEventListener("change", saveSettings);
  clientRectsSpoofing.addEventListener("change", saveSettings);
  randomRectsSpoofing.addEventListener("change", saveSettings);
  noiseLevel.addEventListener("change", saveSettings);
});
