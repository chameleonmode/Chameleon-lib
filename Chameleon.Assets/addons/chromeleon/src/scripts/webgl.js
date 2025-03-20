export default async function (opts) {
  console.log("WebGL Spoofer - Starting", JSON.stringify(opts));
  return function (params) {
    const { random, level } = params || {};

    // PRNG configuration
    const prngConfig = {
      seed: Math.floor(Math.random() * 1000000),
      // Linear congruential generator for pseudo-random numbers
      generateRandom: function () {
        this.seed = (this.seed * 9301 + 49297) % 233280;
        return this.seed / 233280;
      },
    };

    // Define WebGL noise amplitudes with more acceptable ranges
    const amplitudes = {
      micro: 0.00002, // Very subtle noise, minimal visual impact
      mini: 0.00005, // Slight protection with nearly undetectable visual changes
      low: 0.0001, // Good balance for most users
      medium: 0.0002, // Better protection, might cause slight visual changes in precise graphics
      bold: 0.0003, // Strong protection with acceptable visual impact
      high: 0.0004, // Very strong protection with noticeable but acceptable impact
      ultra: 0.0005, // Maximum recommended for daily use
      super: 0.0007, // Only for high-security needs
      max: 0.001, // Maximum protection, may cause visible artifacts
    };

    // Define noise values for different noise levels
    const noiseValues = {
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

    // Use the predefined noise value based on level if not using random
    const noiseValue = noiseValues[level] || 0.5;
    const WebGLnoiseAmplitude = amplitudes[level] || amplitudes.medium;

    // Common WebGL parameter enum values used for fingerprinting
    const IMPORTANT_PARAMS_ENUM = [
      // Viewport and rendering capabilities
      3379, // MAX_TEXTURE_SIZE
      3386, // MAX_VIEWPORT_DIMS
      34076, // MAX_CUBE_MAP_TEXTURE_SIZE
      34024, // MAX_RENDERBUFFER_SIZE
      33902, // ALIASED_LINE_WIDTH_RANGE
      33901, // ALIASED_POINT_SIZE_RANGE

      // Vertex and fragment capabilities
      36347, // MAX_VERTEX_UNIFORM_VECTORS
      36348, // MAX_VARYING_VECTORS
      34921, // MAX_VERTEX_ATTRIBS
      35660, // MAX_VERTEX_TEXTURE_IMAGE_UNITS
      36349, // MAX_FRAGMENT_UNIFORM_VECTORS
      34930, // MAX_TEXTURE_IMAGE_UNITS
      35661, // MAX_COMBINED_TEXTURE_IMAGE_UNITS

      // Bits
      3410, // RED_BITS
      3411, // GREEN_BITS
      3412, // BLUE_BITS
      3413, // ALPHA_BITS
      3414, // DEPTH_BITS
      3415, // STENCIL_BITS
      3408, // SUBPIXEL_BITS
    ];

    // Store original methods
    const originalMethods = {
      WebGLBufferData: WebGLRenderingContext.prototype.bufferData,
      WebGL2BufferData: WebGL2RenderingContext.prototype.bufferData,
      WebGLGetParameter: WebGLRenderingContext.prototype.getParameter,
      WebGL2GetParameter: WebGL2RenderingContext.prototype.getParameter,
      WebGLGetSupportedExtensions: WebGLRenderingContext.prototype.getSupportedExtensions,
      WebGL2GetSupportedExtensions: WebGL2RenderingContext.prototype.getSupportedExtensions,
      WebGLGetShaderPrecisionFormat: WebGLRenderingContext.prototype.getShaderPrecisionFormat,
      WebGL2GetShaderPrecisionFormat: WebGL2RenderingContext.prototype.getShaderPrecisionFormat,
      WebGLGetContextAttributes: WebGLRenderingContext.prototype.getContextAttributes,
      WebGL2GetContextAttributes: WebGL2RenderingContext.prototype.getContextAttributes,
    };

    // Function to apply minimal noise to numeric parameters
    function applyParameterNoise(value, paramName) {
      if (typeof value === "number") {
        // Apply minimal noise to numeric parameters
        const paramNoiseLevel = WebGLnoiseAmplitude * 0.5; // Reduced noise for parameters
        const noiseMultiplier = (random ? prngConfig.generateRandom() : noiseValue) * 2 - 1;
        const noise = paramNoiseLevel * noiseMultiplier;

        // Don't add noise to zero values or very small values
        if (value !== 0 && Math.abs(value) > 0.01) {
          return value + value * noise;
        }
      } else if (value instanceof Float32Array || value instanceof Int32Array) {
        // Apply noise to typed arrays like MAX_VIEWPORT_DIMS, etc.
        const result = new value.constructor(value.length);
        for (let i = 0; i < value.length; i++) {
          if (value[i] !== 0 && Math.abs(value[i]) > 0.01) {
            const paramNoiseLevel = WebGLnoiseAmplitude * 0.5;
            const noiseMultiplier = (random ? prngConfig.generateRandom() : noiseValue) * 2 - 1;
            const noise = paramNoiseLevel * noiseMultiplier;
            result[i] = value[i] + value[i] * noise;
          } else {
            result[i] = value[i];
          }
        }
        return result;
      }
      return value;
    }

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
                  const noise =
                    WebGLnoiseAmplitude * ((random ? prngConfig.generateRandom() : noiseValue) * 2 - 1);
                  dataView.setFloat32(i, value + noise, true);
                }
              }
            }
          }
        } catch (error) { }

        // Call original method with potentially modified data
        return originalMethods[
          contextType === WebGLRenderingContext ? "WebGLBufferData" : "WebGL2BufferData"
        ].call(this, target, srcData, usage);
      };

      // Override getParameter to modify returned values
      proto.getParameter = function (pname) {
        try {
          // Special handling for UNMASKED_VENDOR_WEBGL and UNMASKED_RENDERER_WEBGL
          if (pname === 37445) { // UNMASKED_VENDOR_WEBGL
            // Return one of the most common vendor values
            // return "Google Inc.";
            return "";
          }
          if (pname === 37446) { // UNMASKED_RENDERER_WEBGL
            // Return an appropriate ANGLE implementation based on common platforms
            // For maximum compatibility, use one of these common configurations:
            // return "ANGLE (Intel, Intel(R) UHD Graphics Direct3D11 vs_5_0 ps_5_0, D3D11)";
            // Other common options:
            // "ANGLE (Intel, Intel(R) HD Graphics 620 Direct3D11 vs_5_0 ps_5_0, D3D11)"
            // "ANGLE (NVIDIA, NVIDIA GeForce GTX 1060 Direct3D11 vs_5_0 ps_5_0, D3D11)"
            // "ANGLE (AMD, AMD Radeon(TM) Graphics Direct3D11 vs_5_0 ps_5_0, D3D11)"
            return "";
          }
          // Get the original value first
          const originalValue = originalMethods[
            contextType === WebGLRenderingContext ? "WebGLGetParameter" : "WebGL2GetParameter"
          ].call(this, pname);

          // Only proceed with modification if we got a valid response
          if (originalValue !== null && originalValue !== undefined) {
            // // Special handling for debug info extension
            // if (pname === 37445 || pname === 37446) {
            //   // UNMASKED_VENDOR_WEBGL or UNMASKED_RENDERER_WEBGL
            //   // Don't modify vendor/renderer strings as they might break compatibility
            //   return originalValue;
            // }

            // Only apply noise to important numeric parameters that affect fingerprinting
            if (IMPORTANT_PARAMS_ENUM.includes(pname)) {
              if (
                typeof originalValue === "number" ||
                originalValue instanceof Float32Array ||
                originalValue instanceof Int32Array
              ) {
                return applyParameterNoise(originalValue);
              }
            }
          }

          return originalValue;
        } catch (error) {
          // If anything goes wrong, return the original method's result
          return originalMethods[
            contextType === WebGLRenderingContext ? "WebGLGetParameter" : "WebGL2GetParameter"
          ].call(this, pname);
        }
      };

      // Override getSupportedExtensions to slightly modify the list
      proto.getSupportedExtensions = function () {
        const extensions =
          originalMethods[
            contextType === WebGLRenderingContext
              ? "WebGLGetSupportedExtensions"
              : "WebGL2GetSupportedExtensions"
          ].call(this);

        if (!extensions) return extensions;

        // We'll make minor modifications to the extensions list
        // based on the noise level to affect fingerprinting
        if (WebGLnoiseAmplitude > 0.0001) {
          // For higher noise levels, we might remove a non-critical extension
          const result = extensions.filter((ext) => {
            // Filter out some less essential extensions randomly
            const shouldKeep = (random ? prngConfig.generateRandom() : noiseValue) > 0.95;
            return !shouldKeep || !(ext.includes("debug") || ext.includes("lose_context"));
          });
          return result;
        }

        return extensions;
      };

      // Override getShaderPrecisionFormat to add slight noise to precision formatting
      proto.getShaderPrecisionFormat = function (shaderType, precisionType) {
        const result = originalMethods[
          contextType === WebGLRenderingContext
            ? "WebGLGetShaderPrecisionFormat"
            : "WebGL2GetShaderPrecisionFormat"
        ].call(this, shaderType, precisionType);

        if (result && WebGLnoiseAmplitude > 0.0001) {
          // Only modify the precision for higher noise levels
          // and avoid breaking functionality for lower noise levels
          const noiseFactor = (random ? prngConfig.generateRandom() : noiseValue) * 2 - 1;

          // Clone the result to avoid modifying the original object
          const modified = {
            precision: result.precision,
            rangeMin: result.rangeMin,
            rangeMax: result.rangeMax,
          };

          // Add very subtle noise to precision and ranges
          if (modified.precision > 10) {
            modified.precision += Math.floor(noiseFactor * 2); // Minimal change to precision
          }

          if (modified.rangeMax > 100) {
            modified.rangeMax += Math.floor(noiseFactor * 4); // Minimal change to range
          }

          return modified;
        }

        return result;
      };

      // Override getContextAttributes to slightly modify reported capabilities
      proto.getContextAttributes = function () {
        const attrs =
          originalMethods[
            contextType === WebGLRenderingContext
              ? "WebGLGetContextAttributes"
              : "WebGL2GetContextAttributes"
          ].call(this);

        if (attrs && WebGLnoiseAmplitude > 0.0001) {
          // Clone to avoid modifying the original object
          const modifiedAttrs = { ...attrs };

          // Potentially flip antialias status for fingerprinting protection
          // but only at higher protection levels and with low probability
          if (WebGLnoiseAmplitude > 0.0003 && (random ? prngConfig.generateRandom() : noiseValue) > 0.9) {
            modifiedAttrs.antialias = !modifiedAttrs.antialias;
          }

          return modifiedAttrs;
        }

        return attrs;
      };
    }

    // Apply spoofing to both WebGL contexts
    applyBufferSpoofing(WebGLRenderingContext);
    applyBufferSpoofing(WebGL2RenderingContext);

    console.log(
      `Applied WebGL spoofing with noise amplitude: ${WebGLnoiseAmplitude}, noise level: ${level}`
    );

    return true;
  };
}
