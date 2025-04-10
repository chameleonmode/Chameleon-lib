export default async function (_) {
  return function (_) {
    // Apply navigator property overrides
    Object.defineProperties(Navigator.prototype, {
      // Always override webdriver to return false
      webdriver: {
        get: function () {
          return false;
        },
      },
    });
    return true;
  };
}
