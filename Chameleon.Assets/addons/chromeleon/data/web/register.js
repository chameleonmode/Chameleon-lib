document.addEventListener("DOMContentLoaded", function () {
  const statusElement = document.getElementById("status");
  statusElement.textContent = "Processing registration...";

  // Get URL parameters
  const urlParams = new URLSearchParams(window.location.search);

  // Send registration to background script
  chrome.runtime.sendMessage(
    {
      action: "registerAppLaunch",
      sessionId: urlParams.get("sessionId"),
      instanceId: urlParams.get("instanceId"),
      data: Object.fromEntries(
        Array.from(urlParams.entries()).filter(
          ([key]) => !["sessionId", "instanceId"].includes(key)
        )
      ),
    },
    (response) => {
      if (response && response.success) {
        statusElement.textContent = "Registration successful!";
        
        // Close this page after successful registration (optional)
        // setTimeout(() => {
        //   window.close();
        // }, 2000);
      } else {
        statusElement.textContent = "Registration failed";
      }
    }
  );
});
