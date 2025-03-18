export default function () {
  return function elemenmts() {
    // Store original method
    const originalCreateElement = Document.prototype.createElement;

    // Override createElement
    Document.prototype.createElement = function (tagName, options) {
      console.log("[WebpageMutations] createElement:", tagName);

      return originalCreateElement.call(this, tagName, options);
    };
    return true;
  };
}
