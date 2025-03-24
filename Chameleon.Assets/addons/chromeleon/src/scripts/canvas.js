export default async function (opts) {
  console.log("Canvas Spoofer - Starting", JSON.stringify(opts));
  // Default settings
  return function (params) {
    const { noise, w, h } = params || {};

    // Store original methods
    const originalMethods = {
      getContext: HTMLCanvasElement.prototype.getContext,
      toDataURL: HTMLCanvasElement.prototype.toDataURL,
      getImageData: CanvasRenderingContext2D.prototype.getImageData,
      fillText: CanvasRenderingContext2D.prototype.fillText,
      fillRect: CanvasRenderingContext2D.prototype.fillRect,
    };

    HTMLCanvasElement.prototype.toDataURL = new Proxy(originalMethods.toDataURL, {
      apply(target, thisArg, argumentsList) {
        // Available contexts to try
        const contexts = ["2d", "webgl", "webgl2", "bitmaprenderer"];
        
        // Get a valid 2D context
        const get2DContext = (canvas) => {
          for (let i = 0; i < contexts.length; i++) {
            try {
              const ctx = canvas.getContext(contexts[i]);
              if (ctx && contexts[i] === "2d") return ctx;
            } catch (e) {
              // Continue to next context
            }
          }
          
          // If we couldn't get a 2D context for the original canvas, try with a temporary one
          if (canvas === thisArg) {
            const tempCanvas = document.createElement("canvas");
            tempCanvas.width = canvas.width || 1;
            tempCanvas.height = canvas.height || 1;
            return get2DContext(tempCanvas);
          }
          
          return null;
        };
    
        // Get 2D context or return original result if not available
        const ctx = get2DContext(thisArg);
        if (!ctx) {
          return Reflect.apply(target, thisArg, argumentsList);
        }
        
        // Save canvas state
        ctx.save();
    
        const width = thisArg.width || 1;
        const height = thisArg.height || 1;
        
        // Generate a deterministic hash based on canvas dimensions
        const dimensionHash = (width * 13 + height * 17) % 256;
        
        // Define the modification points in a standard pattern
        const generatePoints = (w, h) => [
          { x: Math.floor(w / 2), y: Math.floor(h / 2) },             // Center
          { x: Math.floor(w / 4), y: Math.floor(h / 4) },             // Top-left
          { x: Math.floor(w * 3 / 4), y: Math.floor(h / 4) },         // Top-right
          { x: Math.floor(w / 4), y: Math.floor(h * 3 / 4) },         // Bottom-left
          { x: Math.floor(w * 3 / 4), y: Math.floor(h * 3 / 4) }      // Bottom-right
        ];
        
        // Map noise levels to intensity
        const noiseMap = {
          "micro": { intensity: 0, points: 1 },
          "mini":  { intensity: 1, points: 2 },
          "low":   { intensity: 2, points: 2 },
          "medium":{ intensity: 3, points: 3 },
          "bold":  { intensity: 4, points: 3 },
          "high":  { intensity: 5, points: 4 },
          "ultra": { intensity: 6, points: 4 },
          "super": { intensity: 7, points: 5 },
          "max":   { intensity: 8, points: 5 }
        };
        
        // Get noise settings or default to lowest - ensure 'noise' is defined
        // This should be passed in as a parameter elsewhere in the code
        // const noise = noise || "low"; // Default to low if not defined elsewhere
        const noiseSettings = noiseMap[noise] || { intensity: 0, points: 1 };
        
        // Select points to modify - deterministic but varies by noise level
        const selectPoints = (totalPoints, pointCount, noiseType) => {
          // Create a noise-specific ordering based on the noise name
          const noiseHash = noiseType.split('').reduce((sum, char) => sum + char.charCodeAt(0), 0);
          
          // Always include center point
          const selected = [totalPoints[0]];
          
          if (pointCount > 1) {
            // Create a deterministically ordered array based on noise type
            const orderedPoints = [...totalPoints.slice(1)];
            
            // Sort remaining points based on the noise hash (deterministic per noise level)
            orderedPoints.sort((a, b) => {
              const hashA = ((a.x * 31) + (a.y * 17) + noiseHash) % 100;
              const hashB = ((b.x * 31) + (b.y * 17) + noiseHash) % 100;
              return hashA - hashB;
            });
            
            // Add required number of points from our deterministically ordered array
            for (let i = 0; i < Math.min(pointCount - 1, orderedPoints.length); i++) {
              selected.push(orderedPoints[i]);
            }
          }
          
          return selected;
        };
        
        // Function to modify a pixel based on intensity level
        const modifyPixel = (originalPixel, pointHash, intensity) => {
          const modifiedPixel = new Uint8ClampedArray(originalPixel);
          
          // Number of bits to affect (scales with intensity)
          const bitMask = (1 << (1 + Math.floor(intensity / 2))) - 1;
          
          // Channel selection based on hash and intensity
          const primaryChannel = intensity % 3; // 0=R, 1=G, 2=B
          
          // Primary channel - guaranteed change
          modifiedPixel[primaryChannel] = (originalPixel[primaryChannel] & ~bitMask) | 
                                         ((pointHash & bitMask) ^ bitMask); // XOR with mask ensures change
          
          // Secondary channel - for intensity >= 2
          if (intensity >= 2) {
            const secondaryChannel = (primaryChannel + 1) % 3;
            modifiedPixel[secondaryChannel] = (originalPixel[secondaryChannel] & ~bitMask) | 
                                             ((pointHash >> 8) & bitMask);
          }
          
          // Tertiary channel - for intensity >= 4
          if (intensity >= 4) {
            const tertiaryChannel = (primaryChannel + 2) % 3;
            modifiedPixel[tertiaryChannel] = (originalPixel[tertiaryChannel] & ~bitMask) | 
                                            ((pointHash >> 16) & bitMask);
          }
          
          // Channel mixing for higher intensities
          if (intensity >= 6) {
            const mixFactor = (pointHash % 3) + 1; // 1, 2, or 3
            
            // Mix RGB channels
            for (let i = 0; i < 3; i++) {
              const nextChannel = (i + 1) % 3;
              modifiedPixel[i] = Math.min(255, Math.floor(
                (modifiedPixel[i] * (4 - mixFactor) + modifiedPixel[nextChannel] * mixFactor) / 4
              ));
            }
          }
          
          // Most extreme modifications
          if (intensity >= 8) {
            // Invert one channel
            const invertChannel = pointHash % 3;
            modifiedPixel[invertChannel] = 255 - modifiedPixel[invertChannel];
            
            // Slight alpha modification
            if (originalPixel[3] < 255) {
              modifiedPixel[3] = Math.max(1, originalPixel[3] - (pointHash % 4));
            }
          }
          
          return modifiedPixel;
        };
        
        // Generate all potential points
        const allPoints = generatePoints(width, height);
        
        // Select points based on noise level - using deterministic selection that varies by noise type
        const pointsToModify = selectPoints(
          allPoints, 
          noiseSettings.points,
          noise
        );
        
        // Apply modifications to each selected point
        pointsToModify.forEach(point => {
          // Ensure point is within canvas bounds
          const x = Math.min(width - 1, Math.max(0, point.x));
          const y = Math.min(height - 1, Math.max(0, point.y));
          
          // Create point-specific hash
          const pointHash = (dimensionHash + x * 7 + y * 11) % 256;
          
          // Get original pixel data
          const pixelData = ctx.getImageData(x, y, 1, 1);
          
          // Modify pixel based on intensity
          const modifiedPixel = modifyPixel(
            pixelData.data, 
            pointHash, 
            noiseSettings.intensity
          );
          
          // Apply the modified pixel
          ctx.putImageData(new ImageData(modifiedPixel, 1, 1), x, y);
        });
        
        // Generate the dataURL
        return Reflect.apply(target, thisArg, argumentsList);
      },
    });

    // // Override getImageData to add slight randomization
    // CanvasRenderingContext2D.prototype.getImageData = new Proxy(originalMethods.getImageData, {
    //   apply(target, thisArg, argumentsList) {
    //     // Call the original method
    //     const imageData = Reflect.apply(target, thisArg, argumentsList);
    //     // Create a copy of the data to avoid modifying the original
    //     const data = new Uint8ClampedArray(imageData.data);

    //     // Add consistent noise to pixel values
    //     for (let i = 0; i < data.length; i += 4) {
    //       data[i] = Math.max(0, Math.min(255, data[i] + pixels.r));
    //       data[i + 1] = Math.max(0, Math.min(255, data[i + 1] + pixels.g));
    //       data[i + 2] = Math.max(0, Math.min(255, data[i + 2] + pixels.b));
    //       // data[i + 3] = Math.max(0, Math.min(255, data[i + 3] + pixels.a));
    //     }

    //     return new ImageData(data, imageData.width, imageData.height);
    //   },
    // });

    // Add slight offset to text positioning
    // CanvasRenderingContext2D.prototype.fillText = new Proxy(originalMethods.fillText, {
    //   apply(target, thisArg, argumentsList) {
    //     const [text, x, y, maxWidth] = argumentsList;

    //     // Add a small random offset
    //     const offsetX = x + Math.random() * 0.001;
    //     const offsetY = y + Math.random() * 0.001;

    //     return Reflect.apply(target, thisArg, [text, offsetX, offsetY, maxWidth]);
    //   },
    // });

    // Modify rectangle drawing slightly
    // CanvasRenderingContext2D.prototype.fillRect = new Proxy(originalMethods.fillRect, {
    //   apply(target, thisArg, argumentsList) {
    //     const [x, y, width, height] = argumentsList;

    //     // Deterministic modifications based on input values
    //     // Using a simple formula that will always produce the same result for the same inputs
    //     const seed = x * 10000 + y * 1000 + width * 100 + height;
    //     const modifier = Math.abs(Math.sin(seed)) * 0.2;

    //     const newX = x + modifier;
    //     const newY = y + modifier;
    //     const newWidth = width + modifier;
    //     const newHeight = height + modifier;

    //     return Reflect.apply(target, thisArg, [newX, newY, newWidth, newHeight]);
    //   },
    // });

    // Override getContext to always set willReadFrequently for 2d contexts
    HTMLCanvasElement.prototype.getContext = new Proxy(originalMethods.getContext, {
      apply(target, thisArg, argumentsList) {
        const [contextId] = argumentsList;
        const options = {...argumentsList[1], willReadFrequently: true};

        return Reflect.apply(target, thisArg, [contextId, options]);
      },
    });
    // HTMLCanvasElement.prototype.getContext = function (contextId, options) {
    //   // return originalMethods.getContext.call(this, contextId, options);

    //   // If it's a 2d context, ensure willReadFrequently is set
    //   if (contextId === "2d") {
    //     options = options || {};
    //     options.willReadFrequently = true;
    //   }

    //   // Call the original method with our modified attributes
    //   return originalMethods.getContext.call(this, contextId, options);
    // };

    console.log("Canvas Spoofer - Finished");

    return true;
  };
}
