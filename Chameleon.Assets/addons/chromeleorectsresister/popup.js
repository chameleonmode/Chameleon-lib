// popup.js

// Set resistance mode on change
document.querySelectorAll("input[name=mode]").forEach((input) => {
    input.addEventListener("change", () => {
        const selectedMode = document.querySelector("input[name=mode]:checked").value;
        chrome.storage.local.set({ resistanceMode: selectedMode });
    });
});

// Load the selected mode on popup open
chrome.storage.local.get(["resistanceMode"], (result) => {
    const mode = result.resistanceMode || "perCall";
    document.querySelector(`input[value=${mode}]`).checked = true;
});

// Refresh multi-session offsets
document.getElementById("refreshOffsets").addEventListener("click", () => {
    chrome.runtime.sendMessage({ action: "refreshOffsets" }, (response) => {
        alert(response.status);
    });
});