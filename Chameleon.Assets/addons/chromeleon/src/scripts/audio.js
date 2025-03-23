export default async function (opts) {
  console.log("Audio Spoofer - Starting", JSON.stringify(opts));
  // Default settings
  return function (params) {
    const { random, level } = params || {};

    // Define noise levels for AudioBuffer.getChannelData with slight variations
    const channelDataNoiseLevels = {
      micro: [0.00000005, 0.00000015], // Range instead of fixed value
      mini: [0.00000015, 0.00000025],
      low: [0.00000025, 0.00000035],
      medium: [0.00000035, 0.00000045],
      bold: [0.00000045, 0.00000055],
      high: [0.00000055, 0.00000065],
      ultra: [0.00000065, 0.00000075],
      super: [0.00000075, 0.00000085],
      max: [0.00000085, 0.00000095],
    };

    // Define noise levels for Analyzer.getFloatFrequencyData with ranges
    const analyzerNoiseLevels = {
      micro: [0.05, 0.15],
      mini: [0.15, 0.25],
      low: [0.25, 0.35],
      medium: [0.35, 0.45],
      bold: [0.45, 0.55],
      high: [0.55, 0.65],
      ultra: [0.65, 0.75],
      super: [0.75, 0.85],
      max: [0.85, 0.95],
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

    // Get noise levels based on selected protection level with fluid randomization
    const getChannelDataNoiseLevel = () => {
      const range = channelDataNoiseLevels[level] || channelDataNoiseLevels.medium;
      // Return a random value within the specified range
      return range[0] + prngConfig.generateRandom() * (range[1] - range[0]);
    };

    const getAnalyzerNoiseLevel = () => {
      const range = analyzerNoiseLevels[level] || analyzerNoiseLevels.medium;
      // Return a random value within the specified range
      return range[0] + prngConfig.generateRandom() * (range[1] - range[0]);
    };

    // Store original methods
    const originalMethods = {
      getChannelData: AudioBuffer.prototype.getChannelData,
      createAnalyserAudioContext: AudioContext.prototype.createAnalyser,
      createAnalyserOfflineAudioContext: OfflineAudioContext.prototype.createAnalyser,
    };

    // Cache for buffer to prevent re-applying noise to the same buffer
    let lastProcessedBuffer = null;

    // Override AudioBuffer.prototype.getChannelData to add subtle noise
      AudioBuffer.prototype.getChannelData = new Proxy(originalMethods.getChannelData, {
        apply(target, self, args) {
          // Call the original method
          const buffer = Reflect.apply(target, self, args);

          // Only modify the buffer if we haven't processed it before
          if (lastProcessedBuffer !== buffer) {
            lastProcessedBuffer = buffer;

            // Apply noise every 100 samples to minimize audio quality impact
            for (let i = 0; i < buffer.length; i += 100) {
              if (random) {
                // Use random index within range when random mode is enabled
                const index = Math.floor(prngConfig.generateRandom() * 100) + Math.floor(i);
                if (index < buffer.length) {
                  // Apply fluid noise value - different for each sample
                  buffer[index] += (prngConfig.generateRandom() * 2 - 1) * getChannelDataNoiseLevel();
                }
              } else {
                // Use fixed index when random mode is disabled (more consistent)
                // Still use fluid noise level
                buffer[i] += (prngConfig.generateRandom() * 2 - 1) * getChannelDataNoiseLevel();
              }
            }
          }

          return buffer;
        },
      });

    // Helper function to protect analyzer nodes
    const protectAnalyser = function (analyserNode) {
      try {
        // Store the original method
        const originalGetFloatFrequencyData = analyserNode.getFloatFrequencyData;

        // Override the getFloatFrequencyData method
        analyserNode.getFloatFrequencyData = new Proxy(originalGetFloatFrequencyData, {
          apply(target, self, args) {
            // Call the original method to fill the frequency data array
            const result = Reflect.apply(target, self, args);

            // The frequency data is passed as the first argument (a Float32Array)
            const frequencyData = args[0];

            // Apply noise to the frequency data
            if (frequencyData && frequencyData.length) {
              for (let i = 0; i < frequencyData.length; i += 100) {
                if (random) {
                  // Random index when random mode is enabled
                  const index = Math.floor(prngConfig.generateRandom() * 100) + Math.floor(i);
                  if (index < frequencyData.length) {
                    // Apply fluid noise value - different for each frequency bin
                    frequencyData[index] += (prngConfig.generateRandom() * 2 - 1) * getAnalyzerNoiseLevel();
                  }
                } else {
                  // Fixed index when random mode is disabled
                  if (i < frequencyData.length) {
                    // Still use fluid noise level
                    frequencyData[i] += (prngConfig.generateRandom() * 2 - 1) * getAnalyzerNoiseLevel();
                  }
                }
              }
            }

            return result;
          },
        });

        return analyserNode;
      } catch (error) {
        return analyserNode;
      }
    };

    // Override AudioContext.prototype.createAnalyser
      AudioContext.prototype.createAnalyser = new Proxy(originalMethods.createAnalyserAudioContext, {
        apply(target, self, args) {
          // Call the original method to create the analyser
          const analyserNode = Reflect.apply(target, self, args);

          // Apply protection to the newly created analyser node
          return protectAnalyser(analyserNode);
        },
      });

    // Override OfflineAudioContext.prototype.createAnalyser
      OfflineAudioContext.prototype.createAnalyser = new Proxy(
        originalMethods.createAnalyserOfflineAudioContext,
        {
          apply(target, self, args) {
            // Call the original method to create the analyser
            const analyserNode = Reflect.apply(target, self, args);

            // Apply protection to the newly created analyser node
            return protectAnalyser(analyserNode);
          },
        }
      );

    console.log(`Applied fluid audio protection with level: ${level}, using dynamic noise ranges`);
    return true;
  };
}
