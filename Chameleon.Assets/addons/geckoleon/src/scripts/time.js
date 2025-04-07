// The function to override the Date object
export default function (opts) {
  const { zone, locale } = opts;
  console.log(`Setting timezone to: ${zone}, locale: ${locale}`);

  // Store original methods
  const originals = {
    toLocaleString: Date.prototype.toLocaleString,
    toLocaleDateString: Date.prototype.toLocaleDateString,
    toLocaleTimeString: Date.prototype.toLocaleTimeString,
    toString: Date.prototype.toString,
    DateTimeFormat: Intl.DateTimeFormat,
  };
  // Override Date.prototype.toString with a timezone-aware formatter
  Date.prototype.toString = function () {
    console.warn("Date.prototype.toString is deprecated. Use toLocaleString instead.");
    const formatter = new Intl.DateTimeFormat("en-US", {
      timeZone: zone,
      weekday: "short",
      year: "numeric",
      month: "short",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    });
    const original = originals.toString.call(this);
    const formatted = formatter.format();
    
    // For debugging
    console.log("original:", original, "formatted:", formatted);
    
    // Return original with formatted parts
    const parts = formatter.formatToParts();
    
    // Extract all parts from the formatter
    const formattedParts = {};
    parts.forEach(part => {
      formattedParts[part.type] = part.value;
    });
    
    // Create a new string in the original format but with the timezone-adjusted values
    return `${formattedParts.weekday} ${formattedParts.month} ${formattedParts.day} ${formattedParts.year} ${formattedParts.hour}:${formattedParts.minute}:${formattedParts.second} GMT${formattedParts.timeZoneName || ''}`;
  };

  // // Override Date.prototype.toString with a timezone-aware formatter
  // Date.prototype.toString = function () {
  //   const formatter = new Intl.DateTimeFormat("en-US", {
  //     timeZone: zone,
  //     weekday: "short",
  //     year: "numeric",
  //     month: "short",
  //     day: "2-digit",
  //     hour: "2-digit",
  //     minute: "2-digit",
  //     second: "2-digit",
  //   });
  //   const original =  originals.toString.call(this);
  //   console.log("original:",original, "formatted", formatter);
  //   return formatter.format(date);

  //   // Get the formatted parts
  //   // const parts = formatter.formatToParts(date);
  //   // const partValues = {};

  //   // // Map the parts for easy access
  //   // parts.forEach((part) => {
  //   //   partValues[part.type] = part.value;
  //   // });

  //   // // Get timezone offset string (GMT+XX:XX format)
  //   // const tzFormatter = new Intl.DateTimeFormat("en-US", {
  //   //   timeZone: zone,
  //   //   timeZoneName: "longOffset",
  //   // });
  //   // const tzParts = tzFormatter.formatToParts(date);
  //   // const tzOffsetPart = tzParts.find((p) => p.type === "timeZoneName");
  //   // const tzOffset = tzOffsetPart ? tzOffsetPart.value : "";

  //   // // Get the full timezone name
  //   // const tzNameFormatter = new Intl.DateTimeFormat("en-US", {
  //   //   timeZone: zone,
  //   //   timeZoneName: "long",
  //   // });
  //   // const tzNameParts = tzNameFormatter.formatToParts(date);
  //   // const tzNamePart = tzNameParts.find((p) => p.type === "timeZoneName");
  //   // const tzName = tzNamePart ? tzNamePart.value : "";

  //   // Build the final string in the format:
  //   // "Weekday Month DD YYYY HH:MM:SS GMT+XXXX (Timezone Name)"
  //   // return `${partValues.weekday} ${partValues.month} ${partValues.day} ${partValues.year} ${partValues.hour}:${partValues.minute}:${partValues.second} ${tzOffset} (${tzName})`;
  // };

  // Override toLocaleString to use our preferred timezone
  Date.prototype.toLocaleString = function (userLocale = locale, options = {}) {
    options = options || {};
    options.timeZone = zone;
    return originals.toLocaleString.call(this, userLocale, options);
  };

  // Override toLocaleDateString
  Date.prototype.toLocaleDateString = function (userLocale = locale, options = {}) {
    options = options || {};
    options.timeZone = zone;
    return originals.toLocaleDateString.call(this, userLocale, options);
  };

  // Override toLocaleTimeString
  Date.prototype.toLocaleTimeString = function (userLocale = locale, options = {}) {
    options = options || {};
    options.timeZone = zone;
    return originals.toLocaleTimeString.call(this, userLocale, options);
  };

  // Override Intl.DateTimeFormat to default to our timezone
  Intl.DateTimeFormat = function (userLocale = locale, options = {}) {
    options = options || {};
    if (!options.timeZone) {
      options.timeZone = zone;
    }
    return new originals.DateTimeFormat(userLocale, options);
  };
  Intl.DateTimeFormat.prototype = originals.DateTimeFormat.prototype;

  console.log("Date object successfully overridden with timezone:", zone, "and locale:", locale);
}
