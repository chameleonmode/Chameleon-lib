// popup.js - Complete implementation
document.addEventListener('DOMContentLoaded', function() {
    // Load current settings
    chrome.storage.local.get(null, function(settings) {
      document.getElementById('proxyAPI').checked = settings.enableProxyAPI;
      document.getElementById('cssInjection').checked = settings.enableCSSInjection;
      document.getElementById('shadowDOM').checked = settings.enableShadowDOM;
      document.getElementById('noiseLevel').value = settings.noiseLevel || 5;
      document.getElementById('noiseLevelValue').textContent = settings.noiseLevel || 5;
      
      // Advanced settings
      document.getElementById('isolatedWorld').checked = settings.isolatedWorldInjection !== false;
      
      // Get extension version
      const manifest = chrome.runtime.getManifest();
      document.getElementById('version').textContent = manifest.version;
    });
    
    // Advanced settings toggle
    document.getElementById('advancedToggle').addEventListener('click', function() {
      const advancedSection = document.getElementById('advancedSettings');
      const isHidden = advancedSection.classList.contains('hidden');
      
      if (isHidden) {
        advancedSection.classList.remove('hidden');
        this.textContent = 'Advanced Settings ▲';
      } else {
        advancedSection.classList.add('hidden');
        this.textContent = 'Advanced Settings ▼';
      }
    });
    
    // Add event listeners for settings changes
    document.getElementById('proxyAPI').addEventListener('change', function(e) {
      chrome.storage.local.set({enableProxyAPI: e.target.checked});
      updateStatusText();
      broadcastSettingsUpdate({enableProxyAPI: e.target.checked, reapplyProtection: true});
    });
    
    document.getElementById('cssInjection').addEventListener('change', function(e) {
      chrome.storage.local.set({enableCSSInjection: e.target.checked});
      updateStatusText();
      broadcastSettingsUpdate({enableCSSInjection: e.target.checked, reapplyProtection: true});
    });
    
    document.getElementById('shadowDOM').addEventListener('change', function(e) {
      chrome.storage.local.set({enableShadowDOM: e.target.checked});
      updateStatusText();
      broadcastSettingsUpdate({enableShadowDOM: e.target.checked, reapplyProtection: true});
    });
    
    document.getElementById('noiseLevel').addEventListener('input', function(e) {
      const value = e.target.value;
      document.getElementById('noiseLevelValue').textContent = value;
      chrome.storage.local.set({noiseLevel: parseInt(value, 10)});
      broadcastSettingsUpdate({noiseLevel: parseInt(value, 10)});
    });
    
    document.getElementById('isolatedWorld').addEventListener('change', function(e) {
      chrome.storage.local.set({isolatedWorldInjection: e.target.checked});
      broadcastSettingsUpdate({isolatedWorldInjection: e.target.checked});
      
      // Provide feedback about the change
      const statusText = e.target.checked ? 
        "Isolated world injection enabled" : 
        "Isolated world injection disabled";
      
      showTemporaryMessage(statusText);
    });
    
    document.getElementById('reapplyButton').addEventListener('click', function() {
      broadcastSettingsUpdate({reapplyProtection: true});
      
      // Visual feedback
      this.textContent = "Reapplying...";
      this.disabled = true;
      
      showTemporaryMessage("Reapplying protection to all tabs");
      
      setTimeout(() => {
        this.textContent = "Reapply Protection";
        this.disabled = false;
      }, 1000);
    });
    
    // Function to broadcast settings updates to all tabs
    function broadcastSettingsUpdate(settings) {
      chrome.runtime.sendMessage({
        type: "updateSettings",
        settings: settings
      });
    }
    
    // Function to show a temporary message
    function showTemporaryMessage(message, duration = 3000) {
      // Create message element if it doesn't exist
      let messageElement = document.getElementById('temporaryMessage');
      
      if (!messageElement) {
        messageElement = document.createElement('div');
        messageElement.id = 'temporaryMessage';
        messageElement.style.position = 'fixed';
        messageElement.style.bottom = '10px';
        messageElement.style.left = '10px';
        messageElement.style.right = '10px';
        messageElement.style.padding = '8px';
        messageElement.style.backgroundColor = '#4285F4';
        messageElement.style.color = 'white';
        messageElement.style.borderRadius = '4px';
        messageElement.style.textAlign = 'center';
        messageElement.style.transition = 'opacity 0.3s';
        messageElement.style.zIndex = '1000';
        document.body.appendChild(messageElement);
      }
      
      // Show message
      messageElement.textContent = message;
      messageElement.style.opacity = '1';
      
      // Hide after duration
      setTimeout(() => {
        messageElement.style.opacity = '0';
      }, duration);
    }
    
    // Display blocked count and update in real-time
    function updateBlockedCount() {
      chrome.storage.local.get('blockedCount', function(data) {
        document.getElementById('blockedCount').textContent = data.blockedCount || 0;
      });
    }
    
    // Check if any protection is enabled
    function updateStatusText() {
      chrome.storage.local.get(null, function(settings) {
        const isAnyProtectionEnabled = 
          settings.enableProxyAPI || 
          settings.enableCSSInjection || 
          settings.enableShadowDOM;
        
        const statusElement = document.getElementById('statusText');
        if (isAnyProtectionEnabled) {
          statusElement.textContent = "Active";
          statusElement.style.color = "#4CAF50"; // Green
        } else {
          statusElement.textContent = "Inactive";
          statusElement.style.color = "#F44336"; // Red
        }
      });
    }
    
    // Initialize status text and blocked count
    updateStatusText();
    updateBlockedCount();
    
    // Set up counter updates
    setInterval(updateBlockedCount, 1000);
    
    // Add reset functionality
    const resetSection = document.createElement('div');
    resetSection.className = 'advanced-section';
    resetSection.innerHTML = `
      <div class="toggle-row" style="justify-content: center; margin-top: 10px;">
        <button id="resetButton" style="background-color: #F44336; color: white; border: none; padding: 5px 10px; border-radius: 4px;">Reset Counter</button>
      </div>
    `;
    document.querySelector('.advanced-section').after(resetSection);
    
    // Add reset button functionality
    document.getElementById('resetButton').addEventListener('click', function() {
      chrome.storage.local.set({blockedCount: 0}, function() {
        updateBlockedCount();
        showTemporaryMessage("Counter has been reset");
        
        // Also reset badge
        chrome.action.setBadgeText({text: ""});
      });
    });
    
    // Add testing section for developers
    if (location.hash === '#dev') {
      const testSection = document.createElement('div');
      testSection.className = 'advanced-section';
      testSection.innerHTML = `
        <div class="advanced-toggle">Developer Testing ▼</div>
        <div class="hidden">
          <div class="toggle-row">
            <button id="testCanvasButton">Test Canvas Protection</button>
          </div>
          <div id="testResult" style="margin-top: 10px; font-size: 12px;"></div>
        </div>
      `;
      document.body.appendChild(testSection);
      
      // Make the developer toggle work
      testSection.querySelector('.advanced-toggle').addEventListener('click', function() {
        const content = this.nextElementSibling;
        const isHidden = content.classList.contains('hidden');
        
        if (isHidden) {
          content.classList.remove('hidden');
          this.textContent = 'Developer Testing ▲';
        } else {
          content.classList.add('hidden');
          this.textContent = 'Developer Testing ▼';
        }
      });
      
      // Add test canvas button functionality
      document.getElementById('testCanvasButton').addEventListener('click', function() {
        chrome.tabs.query({active: true, currentWindow: true}, function(tabs) {
          chrome.scripting.executeScript({
            target: {tabId: tabs[0].id},
            function: testCanvasFingerprinting
          }).then(results => {
            const result = results[0].result;
            document.getElementById('testResult').innerHTML = 
              `<strong>Test Result:</strong> ${result.protected ? 'Protected ✓' : 'Unprotected ✗'}<br>` +
              `<strong>Difference:</strong> ${result.difference.toFixed(2)}%`;
          });
        });
      });
      
      // Function to test canvas fingerprinting protection
      function testCanvasFingerprinting() {
        // Create two canvas elements
        const canvas1 = document.createElement('canvas');
        const canvas2 = document.createElement('canvas');
        canvas1.width = canvas2.width = 200;
        canvas1.height = canvas2.height = 200;
        
        // Draw identical content
        const ctx1 = canvas1.getContext('2d');
        const ctx2 = canvas2.getContext('2d');
        
        // Draw a complex scene
        const drawScene = (ctx) => {
          ctx.fillStyle = 'rgb(255, 0, 0)';
          ctx.fillRect(20, 20, 50, 50);
          ctx.fillStyle = 'rgb(0, 255, 0)';
          ctx.fillRect(80, 20, 50, 50);
          ctx.fillStyle = 'rgb(0, 0, 255)';
          ctx.fillRect(20, 80, 50, 50);
          ctx.font = '18px Arial';
          ctx.fillStyle = 'rgb(0, 0, 0)';
          ctx.fillText('Canvas Test', 40, 150);
        };
        
        drawScene(ctx1);
        drawScene(ctx2);
        
        // Get data URLs and compare
        const dataURL1 = canvas1.toDataURL();
        const dataURL2 = canvas2.toDataURL();
        
        // In a perfect world, both should be identical
        // If protection is active, they should differ
        const isIdentical = dataURL1 === dataURL2;
        
        // Calculate percentage difference
        let diffCount = 0;
        for (let i = 0; i < dataURL1.length; i++) {
          if (dataURL1[i] !== dataURL2[i]) diffCount++;
        }
        
        const difference = (diffCount / dataURL1.length) * 100;
        
        return {
          protected: !isIdentical,
          difference: difference
        };
      }
    }
  });