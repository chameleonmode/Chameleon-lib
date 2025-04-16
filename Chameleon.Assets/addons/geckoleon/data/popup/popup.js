import { config, noises } from "../../src/app.js";
import { getTimezoneArray, getAllSupportedLocales } from "../../src/lib/util.js";

document.addEventListener("DOMContentLoaded", function () {
  // Load config first, then initialize UI
  chrome.runtime.sendMessage({ action: "getConfig" }, function (response) {
    if (response && response.config) {
      Object.assign(config, response.config);

      // Initialize UI elements based on current config
      initializeUI();

      // Set up event listeners
      setupEventListeners();

      // Populate dropdown options
      populateDropdowns();
    }
  });
});

function initializeUI() {
  // Extract RGB values from primary color for background opacity
  const primaryColor = getComputedStyle(document.documentElement)
    .getPropertyValue("--primary-color")
    .trim();
  let rgb = "0, 120, 212"; // Default RGB

  if (primaryColor.startsWith("#")) {
    const r = parseInt(primaryColor.slice(1, 3), 16);
    const g = parseInt(primaryColor.slice(3, 5), 16);
    const b = parseInt(primaryColor.slice(5, 7), 16);
    rgb = `${r}, ${g}, ${b}`;
  }

  document.documentElement.style.setProperty("--primary-color-rgb", rgb);

  // Main extension toggle
  document.getElementById("toggle-extension").checked = config.enabled;
  document.getElementById("status-text").textContent = config.enabled ? "Enabled" : "Disabled";

  // Sync button toggle functionality
  setupSyncButton();

  // Noise level
  const noiseValue = getNoiseLevelValue(config.noise);
  document.getElementById("noise-level-slider").value = noiseValue;
  document.getElementById("noise-level-text").textContent = getNoiseLevelName(noiseValue);

  // Canvas protection
  document.getElementById("canvas-protection").checked = config.canvas.enabled;
  document.getElementById("random-canvas-spoofing").checked = config.canvas.random;

  // WebGL protection
  document.getElementById("webgl-spoofing").checked = config.webgl.enabled;
  document.getElementById("random-webgl-spoofing").checked = config.webgl.random;

  // Client Rects protection
  document.getElementById("client-rects-spoofing").checked = config.rects.enabled;
  document.getElementById("random-rects-spoofing").checked = config.rects.random;

  // Fonts protection
  document.getElementById("fonts-spoofing").checked = config.fonts.enabled;
  document.getElementById("random-fonts-spoofing").checked = config.fonts.random;

  // Audio protection
  document.getElementById("audio-spoofing").checked = config.audio.enabled;
  document.getElementById("random-audio-spoofing").checked = config.audio.random;

  // Navigation/OS spoofing
  document.getElementById("navi-spoofing").checked = config.navi.enabled;
  document.getElementById("randomize-navi").checked = config.navi.random;
  document.getElementById("navi-select").value = config.navi.os;

  // Timezone spoofing
  document.getElementById("timezone-spoofing").checked = config.tz.enabled;
  document.getElementById("randomize-timezone").checked = config.tz.random;
  document.getElementById("timezone-select").value = config.tz.zone;
  document.getElementById("locale-select").value = config.tz.locale;
  document.getElementById("use-system-timezone").checked = config.tz.system;

  // Geolocation spoofing
  document.getElementById("geo-spoofing").checked = config.geo.enabled;
  document.getElementById("randomize-geo-toggle").checked = config.geo.random;
  document.getElementById("coordinates").value = `${config.geo.lat},${config.geo.lon}`;
  document.getElementById("geo-accuracy").value = config.geo.accuracy;
  document.getElementById("geo-accuracy-value").textContent = config.geo.accuracy.toFixed(4);

  // Advanced settings
  document.getElementById("dapi-select").value = config.dAPI;
  document.getElementById("log-level").value = config.log;

  // Populate bypass list
  populateBypassList();
}

function setupSyncButton() {
  const syncButton = document.getElementById("sync-button");
  if (!syncButton) return; // Safety check

  const setup = () => {
    if (config.sync) {
      syncButton.classList.add("active");
      syncButton.title = "On start (prefer app auto settings...)";
    } else {
      syncButton.classList.remove("active");
      syncButton.title = "On start (prefer these settings)";
    }
  };
  setup();

  // Add click listener
  syncButton.addEventListener("click", function () {
    config.sync = !config.sync;
    saveConfig();
    setup();
  });
}

// The rest of the functions remain the same...
function populateDropdowns() {
  // Populate timezone options
  const timezoneSelect = document.getElementById("timezone-select");
  timezoneSelect.innerHTML = "";

  // Convert the timezone object to an array for sorting
  const timezoneArray = getTimezoneArray();

  // Sort by offset (from negative to positive)
  timezoneArray.sort((a, b) => a.offset - b.offset);

  // Add each timezone to the dropdown
  timezoneArray.forEach((timezone) => {
    const option = document.createElement("option");
    option.value = timezone.zone;

    option.textContent = `${timezone.zone} (${timezone.offset})`;
    if (timezone.zone === config.tz.zone) {
      option.selected = true;
    }

    timezoneSelect.appendChild(option);
  });

  // Populate locale options
  const localeSelect = document.getElementById("locale-select");
  localeSelect.innerHTML = "";
  const { flat } = getAllSupportedLocales();
  flat.forEach((locale) => {
    const option = document.createElement("option");
    option.value = locale;
    option.textContent = locale;
    if (locale === config.tz.locale) {
      option.selected = true;
    }
    localeSelect.appendChild(option);
  });
}

function populateBypassList() {
  const bypassList = document.getElementById("bypass-list");
  bypassList.innerHTML = "";

  if (config.bypass.length === 0) {
    const emptyMsg = document.createElement("div");
    emptyMsg.className = "bypass-item";
    emptyMsg.textContent = "No websites in bypass list";
    bypassList.appendChild(emptyMsg);
    return;
  }

  config.bypass.forEach((site) => {
    const item = document.createElement("div");
    item.className = "bypass-item";

    const siteSpan = document.createElement("span");
    siteSpan.textContent = site;

    const deleteBtn = document.createElement("button");
    deleteBtn.className = "bypass-delete";
    deleteBtn.textContent = "✕";
    deleteBtn.addEventListener("click", () => {
      removeBypassSite(site);
    });

    item.appendChild(siteSpan);
    item.appendChild(deleteBtn);
    bypassList.appendChild(item);
  });
}

function setupEventListeners() {
  // Main extension toggle
  document.getElementById("toggle-extension").addEventListener("change", function (e) {
    config.enabled = e.target.checked;
    document.getElementById("status-text").textContent = config.enabled ? "Enabled" : "Disabled";
    saveConfig();
  });

  // Tab functionality
  const tabButtons = document.querySelectorAll(".tab-button");
  const tabContents = document.querySelectorAll(".tab-content");

  tabButtons.forEach((button) => {
    button.addEventListener("click", () => {
      const tabName = button.getAttribute("data-tab");

      // Remove active class from all buttons and contents
      tabButtons.forEach((btn) => btn.classList.remove("active"));
      tabContents.forEach((content) => content.classList.remove("active"));

      // Add active class to current button and content
      button.classList.add("active");
      document.getElementById(`${tabName}-tab`).classList.add("active");
    });
  });

  // Noise level slider
  document.getElementById("noise-level-slider").addEventListener("input", function (e) {
    const noiseValue = parseInt(e.target.value);
    const noiseLevel = getNoiseLevelName(noiseValue);
    document.getElementById("noise-level-text").textContent = noiseLevel;
    config.noise = noiseLevel.toLowerCase();
    saveConfig();
  });

  // Feature toggles - setup for all features
  setupFeatureToggle("canvas-protection", "random-canvas-spoofing", "canvas");
  setupFeatureToggle("webgl-spoofing", "random-webgl-spoofing", "webgl");
  setupFeatureToggle("client-rects-spoofing", "random-rects-spoofing", "rects");
  setupFeatureToggle("fonts-spoofing", "random-fonts-spoofing", "fonts");
  setupFeatureToggle("audio-spoofing", "random-audio-spoofing", "audio");

  // OS spoofing
  document.getElementById("navi-spoofing").addEventListener("change", function (e) {
    config.navi.enabled = e.target.checked;
    saveConfig();
  });

  document.getElementById("randomize-navi").addEventListener("change", function (e) {
    config.navi.random = e.target.checked;
    saveConfig();
  });

  document.getElementById("navi-select").addEventListener("change", function (e) {
    config.navi.os = e.target.value;
    saveConfig();
  });

  // Timezone spoofing
  document.getElementById("timezone-spoofing").addEventListener("change", function (e) {
    config.tz.enabled = e.target.checked;
    saveConfig();
  });

  document.getElementById("randomize-timezone").addEventListener("change", function (e) {
    config.tz.random = e.target.checked;
    saveConfig();
  });

  // This function will be called when the timezone changes
  document.getElementById("timezone-select").addEventListener("change", function (e) {
    const selectedTimezone = e.target.value;
    config.tz.zone = selectedTimezone;

    // Get the timezone offset for display purposes
    // const timezoneData = timezoneOffsets[selectedTimezone];
    // if (timezoneData) {
    //   const offset = timezoneData.offset;
    //   const absOffset = Math.abs(offset);
    //   const hours = Math.floor(absOffset / 60);
    //   const minutes = absOffset % 60;
    //   const sign = offset < 0 ? "-" : "+";
    //   console.log(
    //     `Timezone changed to: ${selectedTimezone} (UTC${sign}${hours.toString().padStart(2, "0")}:${minutes
    //       .toString()
    //       .padStart(2, "0")})`
    //   );
    // }

    saveConfig();
  });

  document.getElementById("locale-select").addEventListener("change", function (e) {
    config.tz.locale = e.target.value;
    saveConfig();
  });

  document.getElementById("use-system-timezone").addEventListener("change", function (e) {
    config.tz.system = e.target.checked;
    saveConfig();
  });

  // Geolocation spoofing
  document.getElementById("geo-spoofing").addEventListener("change", function (e) {
    config.geo.enabled = e.target.checked;
    saveConfig();
  });

  document.getElementById("randomize-geo-toggle").addEventListener("change", function (e) {
    config.geo.random = e.target.checked;
    saveConfig();
  });

  // document.getElementById('coordinates').addEventListener('blur', function(e) {
  //   const coords = e.target.value.split(',');
  //   if (coords.length === 2) {
  //     const lat = parseFloat(coords[0].trim());
  //     const lon = parseFloat(coords[1].trim());

  //     if (!isNaN(lat) && !isNaN(lon)) {
  //       config.geo.lat = lat;
  //       config.geo.lon = lon;
  //       saveConfig();
  //     }
  //   }
  // });

  // Get the coordinates input element
  const coordsInput = document.getElementById("coordinates");
  // Process and save coordinates
  function processCoordinates(value) {
    const coords = value.split(",");
    if (coords.length === 2) {
      const lat = parseFloat(coords[0].trim());
      const lon = parseFloat(coords[1].trim());

      if (!isNaN(lat) && !isNaN(lon) && (config.geo.lat !== lat || config.geo.lon !== lon)) {
        config.geo.lat = lat;
        config.geo.lon = lon;
        saveConfig();
        return true;
      }
    }
    return false;
  }

  // Handle input events
  coordsInput.addEventListener("blur", function (e) {
    processCoordinates(e.target.value);
  });

  // Also handle keyup events for Enter key
  coordsInput.addEventListener("keyup", function (e) {
    if (e.key === "Enter") {
      processCoordinates(e.target.value);
    }
  });

  // Geolocation accuracy
  document.getElementById("geo-accuracy").addEventListener("input", function (e) {
    const value = parseFloat(e.target.value);
    config.geo.accuracy = value;
    document.getElementById("geo-accuracy-value").textContent = value.toFixed(4);
    saveConfig();
  });

  // Advanced settings
  document.getElementById("dapi-select").addEventListener("change", function (e) {
    config.dAPI = e.target.value;
    saveConfig();
  });

  document.getElementById("log-level").addEventListener("change", function (e) {
    config.log = e.target.value;
    saveConfig();
  });

  // Bypass list management
  document.getElementById("add-bypass-btn").addEventListener("click", addBypassSite);
  document.getElementById("new-bypass").addEventListener("keypress", function (e) {
    if (e.key === "Enter") {
      addBypassSite();
    }
  });

  // Refresh button
  document.getElementById("save-button").addEventListener("click", function () {
    saveConfig();
  });
}

function setupFeatureToggle(enableId, randomId, configKey) {
  document.getElementById(enableId).addEventListener("change", function (e) {
    config[configKey].enabled = e.target.checked;
    saveConfig();
  });

  document.getElementById(randomId).addEventListener("change", function (e) {
    config[configKey].random = e.target.checked;
    saveConfig();
  });
}

function addBypassSite() {
  const input = document.getElementById("new-bypass");
  const site = input.value.trim();

  if (site && !config.bypass.includes(site)) {
    config.bypass.push(site);
    saveConfig();
    populateBypassList();
    input.value = "";
  }
}

function removeBypassSite(site) {
  config.bypass = config.bypass.filter((s) => s !== site);
  saveConfig();
  populateBypassList();
}

// Noise level mapping functions
function getNoiseLevelName(value) {
  return noises[value - 1] || "mid"; // Default to Medium (4)
}

function getNoiseLevelValue(name) {
  const noiseLevels = {
    //nano: 0,
    nano: 1,
    mini: 2,
    low: 3,
    mid: 4,
    bold: 5,
    high: 6,
    ultra: 7,
    super: 8,
    max: 9,
  };
  return noiseLevels[name.toLowerCase()] || 4; // Default to Medium (4)
}

function saveConfig() {
  // Send updated config to background script
  chrome.runtime.sendMessage({ action: "updateConfig", config }, function (response) {
    if (response && response.success && config.log !== "none") {
      console.log("Config saved successfully");
    } else {
      console.error("Failed to save config");
    }
  });
}
