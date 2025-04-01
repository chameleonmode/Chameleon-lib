// The function to override the Date object
export default function (timeZone, locale = "en-US") {
  console.log(`Setting timezone to: ${timeZone}, locale: ${locale}`);

  // Store original methods
  const originals = {
    toLocaleString: Date.prototype.toLocaleString,
    toLocaleDateString: Date.prototype.toLocaleDateString,
    toLocaleTimeString: Date.prototype.toLocaleTimeString,
    toString: Date.prototype.toString,
    DateTimeFormat: Intl.DateTimeFormat,
  };

  // 1. Format a Date for Display in a Different Timezone

  // Get a simple but efficient implementation of toString that shows the correct timezone
  Date.prototype.toString = function () {
    // // Example
    // const now = new Date();
    // console.log("formatDateInTimezone ", formatDateInTimezone(now, 'America/New_York'));
    // // April 1, 2025 at 11:57:52 AM Eastern Daylight Time
    // Format the date in the target timezone to get the components
    const date = new Date(this);
    const formatter = new Intl.DateTimeFormat("en-US", {
      timeZone,
      weekday: "short",
      year: "numeric",
      month: "short",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    });

    // Get the formatted parts
    const parts = formatter.formatToParts(date);
    const partValues = {};

    // Map the parts for easy access
    parts.forEach((part) => {
      partValues[part.type] = part.value;
    });

    // Get timezone offset string (GMT+XX:XX format)
    const tzFormatter = new Intl.DateTimeFormat("en-US", {
      timeZone,
      timeZoneName: "longOffset",
    });
    const tzParts = tzFormatter.formatToParts(date);
    const tzOffsetPart = tzParts.find((p) => p.type === "timeZoneName");
    const tzOffset = tzOffsetPart ? tzOffsetPart.value : "";

    // Get the full timezone name
    const tzNameFormatter = new Intl.DateTimeFormat("en-US", {
      timeZone,
      timeZoneName: "long",
    });
    const tzNameParts = tzNameFormatter.formatToParts(date);
    const tzNamePart = tzNameParts.find((p) => p.type === "timeZoneName");
    const tzName = tzNamePart ? tzNamePart.value : "";

    // Build the final string in the format:
    // "Weekday Month DD YYYY HH:MM:SS GMT+XXXX (Timezone Name)"
    return `${partValues.weekday} ${partValues.month} ${partValues.day} ${partValues.year} ${partValues.hour}:${partValues.minute}:${partValues.second} ${tzOffset} (${tzName})`;
  };

  // Override toLocaleString to use our preferred timezone
  Date.prototype.toLocaleString = function (userLocale = locale, options = {}) {
    options = options || {};
    options.timeZone = options.timeZone || timeZone;
    return originals.toLocaleString.call(this, userLocale, options);
  };

  // Override toLocaleDateString
  Date.prototype.toLocaleDateString = function (userLocale = locale, options = {}) {
    options = options || {};
    options.timeZone = options.timeZone || timeZone;
    return originals.toLocaleDateString.call(this, userLocale, options);
  };

  // Override toLocaleTimeString
  Date.prototype.toLocaleTimeString = function (userLocale = locale, options = {}) {
    options = options || {};
    options.timeZone = options.timeZone || timeZone;
    return originals.toLocaleTimeString.call(this, userLocale, options);
  };

  // Override Intl.DateTimeFormat to default to our timezone
  Intl.DateTimeFormat = function (userLocale = locale, options = {}) {
    options = options || {};
    if (!options.timeZone) {
      options.timeZone = timeZone;
    }
    return new originals.DateTimeFormat(userLocale, options);
  };
  Intl.DateTimeFormat.prototype = originals.DateTimeFormat.prototype;

  console.log("Date object successfully overridden with timezone:", timeZone, "and locale:", locale);
}
