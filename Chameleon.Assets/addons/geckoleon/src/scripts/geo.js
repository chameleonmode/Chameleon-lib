// This function runs in the page context and overrides geolocation APIs
export default function (geoConfig) {
  try {
    const { lat, lon, random, accuracy } = geoConfig;
    console.log("Geckoleon: Geolocation spoofing starting", geoConfig);

    // Counter for watchPosition IDs
    let watchPositionId = 0;

    // Store original methods
    const originals = {
      getCurrentPosition: navigator.geolocation.getCurrentPosition,
      watchPosition: navigator.geolocation.watchPosition,
      clearWatch: navigator.geolocation.clearWatch,
      query: navigator.permissions.query,
      watchPositionId: navigator.geolocation.watchPositionId || 0,
    };

    // Setup geolocation API overrides
    function setupGeoAPIs() {
      // Override getCurrentPosition
      navigator.geolocation.getCurrentPosition = function (success, error, options) {
        console.log("Geckoleon: getCurrentPosition called");

        // Call success callback asynchronously
        setTimeout(() => {
          if (typeof success === "function") {
            success({
              coords: {
                latitude: random ? lat + (Math.random() - 0.5) * random : lat,
                longitude: random ? lon + (Math.random() - 0.5) * random : lon,
                accuracy: accuracy || 100,
                altitude: null,
                altitudeAccuracy: null,
                heading: null,
                speed: null,
              },
              timestamp: Date.now(),
            });
          }
        }, 50);
      };

      // Override watchPosition
      navigator.geolocation.watchPosition = function (success, error, options) {
        console.log("Geckoleon: watchPosition called");

        // Call getCurrentPosition once immediately
        navigator.geolocation.getCurrentPosition(success, error, options);

        // Generate ID
        return ++originals.watchPositionId;
      };

      // Override clearWatch
      navigator.geolocation.clearWatch = function (id) {
        console.log("Geckoleon: clearWatch called", id);
        // Call the original method
        originals.clearWatch.call(navigator.geolocation, id);
      };
    }

    function setPermisions() {
      // Override permissions query
      navigator.permissions.query = function (permissionDesc) {
        if (permissionDesc && permissionDesc.name === "geolocation") {
          console.log("Geckoleon: Permissions query for geolocation intercepted");

          // Return a promise for a fake "granted" status
          return new Promise((resolve) => {
            resolve({
                state: "granted",
                onchange: null,
              });
          });
        } else {
          // Use original for other permissions
          return originals.query.call(navigator.permissions, permissionDesc);
        }
      };
    }
    // Initialize our overrides
    setupGeoAPIs();
    setPermisions();

    console.log("Geckoleon: Geolocation spoofing setup complete");
  } catch (error) {
    console.error("Geckoleon geolocation setup error:", error);
  }
}
