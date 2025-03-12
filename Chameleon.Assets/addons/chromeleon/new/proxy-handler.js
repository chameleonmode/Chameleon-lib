// proxy-helper.js
// This file contains helper functions for canvas protection
// It's designed as a simpler utility file rather than trying to inject code

/**
 * Adds noise to image data
 * @param {Uint8ClampedArray} data - The image data array
 * @param {number} level - Noise level (1-10)
 */
function addCanvasNoise(data, level) {
    const noiseAmount = level * 0.1;
    
    for (let i = 0; i < data.length; i += 4) {
      data[i] = Math.max(0, Math.min(255, data[i] + (Math.random() * 2 - 1) * noiseAmount));
      data[i+1] = Math.max(0, Math.min(255, data[i+1] + (Math.random() * 2 - 1) * noiseAmount));
      data[i+2] = Math.max(0, Math.min(255, data[i+2] + (Math.random() * 2 - 1) * noiseAmount));
    }
    
    return data;
  }
  
  // Export for use in content scripts
  if (typeof module !== 'undefined') {
    module.exports = { addCanvasNoise };
  }