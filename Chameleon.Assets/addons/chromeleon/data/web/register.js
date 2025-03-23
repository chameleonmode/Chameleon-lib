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
        Array.from(urlParams.entries()).filter(([key]) => !["sessionId", "instanceId"].includes(key))
      ),
    },
    (response) => {
      if (response && response.success) {
        statusElement.textContent = "Registration successful!";
        // navigate to the URL from the response.url
        window
          .open(response.url, "_self")
          .then(() => {
            statusElement.textContent = "Navigated to the URL.";
          })
          .catch((error) => {
            statusElement.textContent = "Failed to navigate to the URL.";
            console.error("Navigation error:", error);
          });
        // Optionally, you can also log the response data
        console.log("Registration response:", response);
        console.log("Session ID:", urlParams.get("sessionId"));
        console.log("Instance ID:", urlParams.get("instanceId"));
        console.log("Data:", Object.fromEntries(urlParams.entries()));

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
