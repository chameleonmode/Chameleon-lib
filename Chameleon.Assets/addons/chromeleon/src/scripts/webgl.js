// https://gist.github.com/abrahamjuliot/7baf3be8c451d23f7a8693d7e28a35e2
export default function (opts) {
  return function webgl(params) {
    console.log(params);
    const { random, level } = params || {};
    // Define WebGL noise levels with more acceptable ranges
    const amplitudes = {
      micro: 0.00001, // More acceptable range: smaller values
      mini: 0.00002, // cause less noticeable distortion but still
      low: 0.00003, // provide fingerprinting protection
      medium: 0.00004,
      bold: 0.00005,
      high: 0.0001,
      ultra: 0.0002,
      super: 0.0003,
      max: 0.0005,
    };

    // Define noise values for different noise levels
    const noises = {
      micro: 0.1,
      mini: 0.2,
      low: 0.3,
      medium: 0.4,
      bold: 0.5,
      high: 0.6,
      ultra: 0.7,
      super: 0.8,
      max: 0.9,
    };


    // PRNG configuration
    const prngConfig = {
      seed: Math.floor(Math.random() * 1000000),
      // Linear congruential generator for pseudo-random numbers
      generateRandom: function () {
        this.seed = (this.seed * 9301 + 49297) % 233280;
        return this.seed / 233280;
      },
    };

    // Store original methods
    const originalMethods = {
      WebGLBufferData: WebGLRenderingContext.prototype.bufferData,
      WebGL2BufferData: WebGL2RenderingContext.prototype.bufferData,
    };

    // Function to apply buffer data spoofing to a context
    function applyBufferSpoofing(contextType) {
      const proto = contextType.prototype;

      // Override the bufferData method directly
      proto.bufferData = function (target, srcData, usage) {
        try {
          // Apply noise only if srcData is an ArrayBuffer or ArrayBufferView
          if (srcData instanceof ArrayBuffer || ArrayBuffer.isView(srcData)) {
            const length = srcData.byteLength;
            const dataView = new DataView(srcData.buffer || srcData);

            // Add noise to each float value
            for (let i = 0; i < length; i += 4) {
              if (i + 4 <= length) {
                // Ensure we have enough bytes for a float32
                const value = dataView.getFloat32(i, true);
                
                // Only apply noise to non-zero values to avoid NaN issues
                if (value !== 0) {
                    // Apply appropriate noise with proper operator precedence
                    // Uses WebGLnoiseAmplitude for scale and either random or predefined noise value
                    const noise = amplitudes[level] * ((random ? prngConfig.generateRandom() : noises[level]) * 2 - 1);
                    dataView.setFloat32(i, value + noise, true);
                  }
              }
            }
          }
        } catch (error) {
          console.error("Error in bufferData spoofing:", error);
        }

        // Call original method with potentially modified data
        return originalMethods[
          contextType === WebGLRenderingContext ? "WebGLBufferData" : "WebGL2BufferData"
        ].call(this, target, srcData, usage);
      };
    }

    // Apply spoofing to both WebGL contexts
    applyBufferSpoofing(WebGLRenderingContext);
    applyBufferSpoofing(WebGL2RenderingContext);

    console.log(
      `Applied WebGL spoofing with noise amplitude: ${amplitudes[level]}, noise level: ${noises[level]}`
    );

    return true;
  };
}
