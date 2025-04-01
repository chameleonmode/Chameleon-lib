
// The function to override the Date object
export default function (timezone, locale = "en-US") {
    console.log(`Setting timezone to: ${timezone}, locale: ${locale}`);
  
    // Check if the timezone is valid
    try {
      new Intl.DateTimeFormat(locale, { timeZone: timezone });
    } catch (e) {
      console.error(`Invalid timezone: ${timezone}`);
      return;
    }
  
    // Store original methods
    const originalMethods = {
      toLocaleString: Date.prototype.toLocaleString,
      toLocaleDateString: Date.prototype.toLocaleDateString,
      toLocaleTimeString: Date.prototype.toLocaleTimeString,
      toString: Date.prototype.toString,
    };

    // 1. Format a Date for Display in a Different Timezone
    function formatDateInTimezone(date, timeZone) {
        // Format the date in the target timezone to get the components
        const formatter = new Intl.DateTimeFormat('en-US', {
          timeZone,
          weekday: 'short',
          year: 'numeric',
          month: 'short',
          day: '2-digit',
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit'
        });
        
        // Get the formatted parts
        const parts = formatter.formatToParts(date);
        const partValues = {};
        
        // Map the parts for easy access
        parts.forEach(part => {
          partValues[part.type] = part.value;
        });
        
        // Get timezone offset string (GMT+XX:XX format)
        const tzFormatter = new Intl.DateTimeFormat('en-US', {
          timeZone,
          timeZoneName: 'longOffset'
        });
        const tzParts = tzFormatter.formatToParts(date);
        const tzOffsetPart = tzParts.find(p => p.type === 'timeZoneName');
        const tzOffset = tzOffsetPart ? tzOffsetPart.value : '';
        
        // Get the full timezone name
        const tzNameFormatter = new Intl.DateTimeFormat('en-US', {
          timeZone,
          timeZoneName: 'long'
        });
        const tzNameParts = tzNameFormatter.formatToParts(date);
        const tzNamePart = tzNameParts.find(p => p.type === 'timeZoneName');
        const tzName = tzNamePart ? tzNamePart.value : '';
        
        // Build the final string in the format: 
        // "Weekday Month DD YYYY HH:MM:SS GMT+XXXX (Timezone Name)"
        return `${partValues.weekday} ${partValues.month} ${partValues.day} ${partValues.year} ${partValues.hour}:${partValues.minute}:${partValues.second} ${tzOffset} (${tzName})`;
      }
      
      // Example
      const now = new Date();
      console.log("formatDateInTimezone ", formatDateInTimezone(now, 'America/New_York'));
      // April 1, 2025 at 11:57:52 AM Eastern Daylight Time

      function getFullTimeZoneName() {
        // Create a formatter that focuses on the timezone information
        const formatter = new Intl.DateTimeFormat('en-US', {
          timeZoneName: 'long'
        });
        
        // Use formatToParts to extract just the timezone part
        const parts = formatter.formatToParts(new Date());
        const timeZonePart = parts.find(part => part.type === 'timeZoneName');
        
        // Return the full name
        return timeZonePart ? timeZonePart.value : null;
      }
      
      // Usage
      const fullTimeZoneName = getFullTimeZoneName();
      console.log("getFullTimeZoneName " +fullTimeZoneName); // "Eastern European Standard Time", "Pacific Daylight Time", etc.
  
    // Simple timezone cache to avoid recalculating for the same date
    const tzCache = new Map();
    const cacheLimit = 100; // Limit cache size to prevent memory issues

    function formatDateCustom(date, locale = "en", options = {}) {
      // Create a formatter with the options provided by the user
      const formatter = new Intl.DateTimeFormat(locale, options);

      // For most cases, just return the default formatting
      // This uses whatever Intl.DateTimeFormat would normally do
      return formatter.format(date);
    }

    // Example usage:
    const date = new Date();
    console.log("formatDateCustom " + formatDateCustom(date)); // Uses completely default formatting


    // If you want the specific "Tuesday, 1 April 2025 at 4:49:39 pm Eastern European Standard Time" format:
    console.log( "formatDateCustom " +
      formatDateCustom(date, "en-GB", {
        weekday: "long",
        day: "numeric",
        month: "long",
        year: "numeric",
        hour: "numeric",
        minute: "numeric",
        second: "numeric",
        hour12: true,
        timeZoneName: "long",
      })
    );
  
    // Helper function to get timezone-adjusted values (with caching)
    function getAdjustedDate(date) {
      const timestamp = date.getTime();
  
      // Check cache first
      if (tzCache.has(timestamp)) {
        return tzCache.get(timestamp);
      }
  
      // Format date in target timezone
      const formatter = new Intl.DateTimeFormat(locale, {
        timeZone: timezone,
        hour12: false,
        year: "numeric",
        month: "numeric",
        day: "numeric",
        hour: "numeric",
        minute: "numeric",
        second: "numeric",
      });
  
      // Parse parts
      const parts = {};
      formatter.formatToParts(date).forEach((part) => {
        if (part.type !== "literal") {
          parts[part.type] = part.value;
        }
      });
  
      // Convert to proper types
      const result = {
        year: parseInt(parts.year, 10),
        month: parseInt(parts.month, 10) - 1, // Convert to 0-indexed
        day: parseInt(parts.day, 10),
        hour: parseInt(parts.hour, 10),
        minute: parseInt(parts.minute, 10),
        second: parseInt(parts.second, 10),
      };
  
      // Store in cache
      if (tzCache.size >= cacheLimit) {
        // Remove oldest entry if cache is full
        const oldestKey = tzCache.keys().next().value;
        tzCache.delete(oldestKey);
      }
  
      tzCache.set(timestamp, result);
      return result;
    }
  
    // Get timezone abbreviation (simple version)
    function formatTimezone(timeZoneName) {
      try {
        const now = new Date();
        const short = new Intl.DateTimeFormat(locale, {
            timeZone: timezone,
            timeZoneName,
        }).formatToParts(now).find((part) => part.type === "timeZoneName");
        return short.value || timezone;
      } catch (e) {
        return timezone.split("/").pop();
      }
    }
  
    // Get localized day and month names
    function getLocalizedNames() {
      // For English formatting even if locale is different
      const enFormatter = new Intl.DateTimeFormat("en-US", {
        timeZone: "UTC",
        weekday: "short",
        month: "short",
      });
  
      const days = [];
      const months = [];
  
      // Get day names
      for (let i = 0; i < 7; i++) {
        const date = new Date(Date.UTC(2023, 0, i + 1)); // Jan 1, 2023 was a Sunday
        const parts = enFormatter.formatToParts(date);
        const weekday = parts.find((p) => p.type === "weekday").value;
        days.push(weekday);
      }
  
      // Get month names
      for (let i = 0; i < 12; i++) {
        const date = new Date(Date.UTC(2023, i, 1));
        const parts = enFormatter.formatToParts(date);
        const month = parts.find((p) => p.type === "month").value;
        months.push(month);
      }
  
      return { days, months };
    }
  
    // Get the localized names once
    const { days, months } = getLocalizedNames();
  
    // Get a simple but efficient implementation of toString that shows the correct timezone
    Date.prototype.toString = function () {
      try {
        console.log("Geckoleon: toString called", originalMethods.toString.call(this));

       // Get your current offset from UTC
       const offset = new Date().getTimezoneOffset();
       const offsetHours = Math.abs(offset) / 60;
       console.log(`UTC${offset <= 0 ? '+' : '-'}${offsetHours}`);
  
        // Get the adjusted date for the chosen timezone
        const adjusted = getAdjustedDate(this);
  
        // The adjusted date will have the correct day of week
        const tempDate = new Date(adjusted.year, adjusted.month, adjusted.day);
        const dayOfWeek = days[tempDate.getDay()];
        const monthName = months[adjusted.month];
  
        // Format the date string manually (much faster than complex timezone math)
        // return (
        //   `${dayOfWeek} ${monthName} ${String(adjusted.day).padStart(2, " ")} ${adjusted.year} ` +
        //   `${String(adjusted.hour).padStart(2, "0")}:${String(adjusted.minute).padStart(2, "0")}:${String(
        //     adjusted.second
        //   ).padStart(2, "0")}` +
        //   ` ${formatTimezone("short")}` +
        //   ` ${formatTimezone("long")}`
        // );
        return formatDateInTimezone(this, timezone);
      } catch (e) {
        // Fallback to the original toString implementation
        return originalMethods.toString.call(this);
      }
    };
  
    // Override toLocaleString to use our preferred timezone
    Date.prototype.toLocaleString = function (userLocale = locale, options = {}) {
      options = options || {};
      options.timeZone = options.timeZone || timezone;
      return originalMethods.toLocaleString.call(this, userLocale, options);
    };
  
    // Override toLocaleDateString
    Date.prototype.toLocaleDateString = function (userLocale = locale, options = {}) {
      options = options || {};
      options.timeZone = options.timeZone || timezone;
      return originalMethods.toLocaleDateString.call(this, userLocale, options);
    };
  
    // Override toLocaleTimeString
    Date.prototype.toLocaleTimeString = function (userLocale = locale, options = {}) {
      options = options || {};
      options.timeZone = options.timeZone || timezone;
      return originalMethods.toLocaleTimeString.call(this, userLocale, options);
    };
  
    // Override Intl.DateTimeFormat to default to our timezone
    const OriginalDateTimeFormat = Intl.DateTimeFormat;
    Intl.DateTimeFormat = function (userLocale = locale, options = {}) {
      options = options || {};
      if (!options.timeZone) {
        options.timeZone = timezone;
      }
      return new OriginalDateTimeFormat(userLocale, options);
    };
    Intl.DateTimeFormat.prototype = OriginalDateTimeFormat.prototype;
  
    console.log("Date object successfully overridden with timezone:", timezone, "and locale:", locale);
  }