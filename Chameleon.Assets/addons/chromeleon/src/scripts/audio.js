export default async function (opts) {
  return function (params = { uuid: "bloop", noise: "mid", hash: Math.random(), random: false }) {
    const { uuid, noise, random, hash } = params;
    window[uuid] = window[uuid] || {};

    // Get noise value for random or fixed noise setting
    const noiseify = () => {
      const noiseKey = random
        ? Object.keys(noises)[Math.floor(Math.random() * Object.keys(noises).length)]
        : noise;
      // Use hash to create micro-variations for uniqueness
      const baseLevel = noises[noiseKey] || 4;
      // console.log(`${baseLevel}\n`);
      return hash + baseLevel;
    };

    const noises = {
      nano:  0.00000001,
      mini:  0.00000002,
      low:   0.00000003,
      mid:   0.00000004,
      bold:  0.00000005,
      high:  0.00000006,
      ultra: 0.00000007,
      super: 0.00000008,
      max:   0.00000009,
      // Add more noise levels as needed
    };
    // Store original methods
    const originals = {
      lastProcessedBuffer: null,
      getChannelData: AudioBuffer.prototype.getChannelData
    };

    if (!window[uuid]["getChannelData"]) {
      window[uuid]["getChannelData"] = true;

      AudioBuffer.prototype.getChannelData = new Proxy(originals.getChannelData, {
      apply(target, self, args) {
        // Call the original method
        const buffer = Reflect.apply(target, self, args);

        // Only modify the buffer if we haven't processed it before
        if (originals.lastProcessedBuffer !== buffer) {
          originals.lastProcessedBuffer = buffer;

          // Specifically target the 4500-5000 range that's used by the analyzer
          // This ensures we affect the SHA1 hash calculation while keeping changes minimal
          for (let i = 500; i < 5000; i += 100) {
            if (i < buffer.length) {
              const data = buffer[i];
              // Use a very subtle noise value to avoid audio quality impact
              const noise = noiseify();
              buffer[i] = data + noise;
            }
          }
        }

        return buffer;
      },
    });
    }
    return true;
  };
}
