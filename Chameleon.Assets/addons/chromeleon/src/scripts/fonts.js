export default function (opts) {
    return function fonts(params) {
      console.log(params);
      const { random, level } = params || {};
      
      // Define noise levels for different protection intensities
      const fontNoiseAmplitudes = {
        micro: 0.1,  // Very subtle changes
        mini: 0.4,   // Minor changes
        low: 0.8,    // Low noise level
        medium: 1.4, // Standard protection
        bold: 1.8,   // Stronger noise
        high: 2.4,   // High protection
        ultra: 2.5,  // Very high protection
        super: 3.4,  // Super high protection
        max: 3.8,    // Maximum protection
      };
      
      // PRNG configuration for consistent randomization
      const prngConfig = {
        seed: Math.floor(Math.random() * 1000000),
        // Linear congruential generator for pseudo-random numbers
        generateRandom: function () {
          this.seed = (this.seed * 9301 + 49297) % 233280;
          return this.seed / 233280;
        },
      };
      
      // Get amplitude based on level
      const noiseAmplitude = fontNoiseAmplitudes[level] || fontNoiseAmplitudes.medium;
      
      // Determine noise sign (-1 or 1)
      // This creates a bias toward certain directions based on the level
      let noiseSign;
      if (random) {
        // Using a more balanced approach for random mode
        noiseSign = prngConfig.generateRandom() < 0.5 ? -1 : 1;
      } else {
        // For fixed levels, use a consistent but slightly biased approach
        // Weighted toward negative values (making fonts appear smaller)
        // which is generally less disruptive to layouts
        const signProbabilities = [-1, -1, -1, -1, -1, -1, 1, -1, -1, -1]; // 90% negative
        const index = Math.floor(prngConfig.generateRandom() * signProbabilities.length);
        noiseSign = signProbabilities[index];
      }
      
      // Create noise function that will be applied to measurements
      const noisify = function(size) {
        if (!size) return size; // Don't modify zero or undefined values
        
        // Generate noise
        let noise;
        if (random) {
          // Each measurement gets its own random noise when random is true
          noise = noiseSign * prngConfig.generateRandom() * noiseAmplitude;
        } else {
          // Fixed noise for consistent results when random is false
          noise = noiseSign * noiseAmplitude;
        }
        
        // Apply noise and round to integer to avoid suspicious floating point values
        return Math.floor(size + noise);
      };
      
      // Store original methods
      const originalDescriptors = {
        offsetHeight: Object.getOwnPropertyDescriptor(HTMLElement.prototype, "offsetHeight"),
        offsetWidth: Object.getOwnPropertyDescriptor(HTMLElement.prototype, "offsetWidth")
      };
      
      // Override offsetHeight property
      Object.defineProperty(HTMLElement.prototype, "offsetHeight", {
        get: function() {
          try {
            // Get the height from getBoundingClientRect (more accurate)
            // then apply noise to slightly alter it
            return noisify(Math.floor(this.getBoundingClientRect().height));
          } catch (error) {
            console.error("Error in offsetHeight spoofing:", error);
            // Fallback to original if error occurs
            return originalDescriptors.offsetHeight.get.call(this);
          }
        }
      });
      
      // Override offsetWidth property
      Object.defineProperty(HTMLElement.prototype, "offsetWidth", {
        get: function() {
          try {
            // Get the width from getBoundingClientRect (more accurate)
            // then apply noise to slightly alter it
            return noisify(Math.floor(this.getBoundingClientRect().width));
          } catch (error) {
            console.error("Error in offsetWidth spoofing:", error);
            // Fallback to original if error occurs
            return originalDescriptors.offsetWidth.get.call(this);
          }
        }
      });
      
      console.log(`Applied font protection with noise amplitude: ${noiseAmplitude}, sign: ${noiseSign}, level: ${level}`);
      return true;
    };
  }