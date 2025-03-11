document.addEventListener('DOMContentLoaded', function() {
    // Get URL parameters
    const urlParams = new URLSearchParams(window.location.search);
    const source = urlParams.get('source');
    const sessionId = urlParams.get('sessionId');
    const appInstanceId = urlParams.get('appInstanceId');
    
    const statusElement = document.getElementById('status');
    statusElement.textContent = 'Processing registration...';
    
    // Validate parameters
    if (source !== 'app' || !sessionId) {
      statusElement.textContent = 'Invalid registration parameters';
      return;
    }
    
    // Send registration to background script
    chrome.runtime.sendMessage({
      action: 'registerAppLaunch',
      sessionId: sessionId,
      appInstanceId: appInstanceId,
      timestamp: Date.now(),
      additionalData: Object.fromEntries(urlParams.entries())
    }, (response) => {
      if (response && response.success) {
        statusElement.textContent = 'Registration successful!';
        
        // Store in session storage for this tab
        sessionStorage.setItem('sessionId', sessionId);
        sessionStorage.setItem('appInstanceId', appInstanceId);
        
        // Close this page after successful registration (optional)
        setTimeout(() => {
          window.close();
        }, 2000);
      } else {
        statusElement.textContent = 'Registration failed';
      }
    });
  });