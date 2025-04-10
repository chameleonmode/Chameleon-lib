/**
 * Gets an array of timezones with their offsets
 * @return {Array<{zone: string, offset: string}>} Array of timezone objects with zone and offset properties
 */
function getTimezoneArray() {
  const timezones = Intl.supportedValuesOf("timeZone");
  const timezoneArray = [];
  for (const zone of timezones) {
    const now = new Date();
    const formatter = new Intl.DateTimeFormat("en-US", { timeZone: zone, timeZoneName: "shortOffset" });
    const parts = formatter.formatToParts(now);
    let offset = "";
    for (const part of parts) {
      if (part.type === "timeZoneName") {
        offset = part.value;
        break;
      }
    }
    // Extract the numeric offset from the string.
    let numericOffset = null;
    const match = offset.match(/GMT([+-]\d{1,2}:\d{2})/);
    if (match && match[1]) {
      numericOffset = match[1];
    } else {
      const match2 = offset.match(/GMT([+-]\d{1,2})/);
      if (match2 && match2[1]) {
        numericOffset = match2[1] + ":00";
      } else if (offset === "GMT") {
        // Handle UTC/GMT+0 case
        numericOffset = "+0:00";
      }
    }
    timezoneArray.push({
      zone: zone,
      offset: numericOffset,
    });
  }
  return timezoneArray;
}

// List of common country codes to test with the given language
const countryCodes = [
  "US",
  "GB",
  "CA",
  "AU",
  "NZ",
  "IE",
  "ZA",
  "IN",
  "PH",
  "SG",
  "MY",
  "HK",
  "AE",
  "BW",
  "CM",
  "GH",
  "JM",
  "KE",
  "LR",
  "MW",
  "NA",
  "NG",
  "PK",
  "SL",
  "TZ",
  "UG",
  "ZM",
  "ZW",
  "BS",
  "BB",
  "BZ",
  "TT",
  "AG",
  "DM",
  "GD",
  "KN",
  "LC",
  "VC",
  "FJ",
  "MH",
  "SB",
  "TO",
  "VU",
  "IL",
  "MT",
  "BM",
  "GI",
  "KY",
  "FK",
  "SH",
  "TC",
  "MS",
  "JP",
  "CN",
  "TW",
  "KR",
  "FR",
  "DE",
  "IT",
  "ES",
  "PT",
  "BR",
  "MX",
  "AR",
  "CL",
  "CO",
  "PE",
  "VE",
  "RU",
];

// List of common language codes to test with the given country
const languageCodes = [
  "en",
  "es",
  "fr",
  "de",
  "it",
  "pt",
  "ru",
  "zh",
  "ja",
  "ko",
  "ar",
  "hi",
  "bn",
  "pa",
  "te",
  "mr",
  "ta",
  "ur",
  "fa",
  "tr",
  "vi",
  "th",
  "id",
  "ms",
  "fil",
  "nl",
  "da",
  "sv",
  "no",
  "fi",
  "pl",
  "uk",
  "cs",
  "sk",
  "hu",
  "ro",
  "bg",
  "el",
  "he",
  "ca",
  "eu",
  "gl",
  "ast",
  "cy",
  "ga",
  "gd",
  "is",
  "lb",
  "lt",
  "lv",
  "et",
  "sr",
  "hr",
  "bs",
  "sl",
  "mk",
  "sq",
  "mt",
  "kk",
  "uz",
  "az",
  "hy",
  "ka",
  "lo",
  "km",
  "mn",
  "ne",
  "si",
  "my",
  "am",
];

/**
 * Gets an array of supported English locales (en-*) for all countries
 * Usage example:
 * const supportedEnLocales = getEnglishLocales();
 * console.log(supportedEnLocales);
 * @param {string} language - The language code (e.g., 'en', 'fr', 'zh')
 * @return {string[]} Array of supported locales for the specified language
 */
function getLocalesForLanguage(language) {
  // Create an array of locales to test
  const locales = countryCodes.map((code) => `${language}-${code}`);

  // Check each locale and keep those that work
  const workingLocales = [];

  for (const locale of locales) {
    try {
      // Try formatting a number with this locale
      new Intl.NumberFormat(locale).format(1000.5);
      new Intl.DateTimeFormat(locale).format(new Date());

      // If the locale works without throwing an error, add it to the list
      workingLocales.push(locale);
    } catch (e) {
      // If there's an error, the locale is not supported
      continue;
    }
  }

  return workingLocales;
}

/**
 * Gets an array of supported locales for a specific country code
 * Usage example:
 * const supportedLocalesForUS = getLocalesForCountry('US');
 * console.log(supportedLocalesForUS);
 * @param {string} countryCode - The two-letter country code (e.g., 'US', 'FR', 'JP')
 * @return {string[]} Array of supported locales for the specified country
 */
function getLocalesForCountry(countryCode) {
  // Ensure country code is uppercase
  const country = countryCode.toUpperCase();

  // Create an array of locales to test
  const locales = languageCodes.map((lang) => `${lang}-${country}`);

  // Check each locale and keep those that work
  const workingLocales = [];

  for (const locale of locales) {
    try {
      // Try formatting a number with this locale
      new Intl.NumberFormat(locale).format(1000.5);
      new Intl.DateTimeFormat(locale).format(new Date());

      // If the locale works without throwing an error, add it to the list
      workingLocales.push(locale);
    } catch (e) {
      // If there's an error, the locale is not supported
      continue;
    }
  }

  return workingLocales;
}

/**
 * Gets all supported locales in the browser
 * @param {Object} options - Configuration options
 * @param {boolean} [options.fullTest=false] - Whether to test all possible combinations (slow but comprehensive)
 * @param {string[]} [options.languages] - Optional specific language codes to test
 * @param {string[]} [options.countries] - Optional specific country codes to test
 * @return {Object<{byLanguage: Object, byCountry: Object, flat: string[]}>} Object with three properties:
 *                  - byLanguage: Object with languages as keys and arrays of countries as values
 *                  - byCountry: Object with countries as keys and arrays of languages as values
 *                  - flat: Array of all supported locale strings
 */
function getAllSupportedLocales(
  options = {
    full: false,
    languages: [
      "en",
      "es",
      "fr",
      "de",
      "it",
      "pt",
      "ru",
      "zh",
      "ja",
      "ko",
      "ar",
      "hi",
      "bn",
      "ur",
      "fa",
      "tr",
      "vi",
      "th",
      "id",
      "ms",
      "nl",
      "sv",
      "no",
      "fi",
      "pl",
      "cs",
      "hu",
      "ro",
      "bg",
      "el",
      "he",
    ],
    countries: [
      "US",
      "GB",
      "CA",
      "AU",
      "NZ",
      "IE",
      "IN",
      "JP",
      "CN",
      "KR",
      "FR",
      "DE",
      "IT",
      "ES",
      "PT",
      "BR",
      "MX",
      "AR",
      "RU",
      "ZA",
      "AE",
      "SA",
      "SG",
      "MY",
      "TH",
      "VN",
      "ID",
      "PH",
      "TR",
      "IL",
    ],
  }
) {
  // If fullTest is true, use expanded lists
  if (options.full) {
    options.languages = languageCodes;
    options.countries = countryCodes;
  }

  // Initialize result objects
  const byLanguage = {};
  const byCountry = {};
  const flat = [];

  // Test all combinations based on configuration
  for (const lang of options.languages) {
    byLanguage[lang] = [];

    for (const country of options.countries) {
      const locale = `${lang}-${country}`;

      try {
        // Try formatting to check if the locale is supported
        new Intl.NumberFormat(locale).format(1000.5);
        new Intl.DateTimeFormat(locale).format(new Date());

        // If no error, this locale is supported
        byLanguage[lang].push(country);

        // Initialize country array if it doesn't exist
        if (!byCountry[country]) {
          byCountry[country] = [];
        }

        byCountry[country].push(lang);
        flat.push(locale);
      } catch (e) {
        // If there's an error, the locale is not supported
        continue;
      }
    }
  }

  return {
    byLanguage,
    byCountry,
    flat,
  };
}

/**
 * Checks if a value matches any of the given patterns
 * // Example usage:
 *
 * // Simple text matching
 * // matchesPattern("hello world", ["hello", "test"]); // true
 * 
 * // Wildcard pattern matching
 * // matchesPattern("hello world", ["hello*"]); // true
 * // matchesPattern("hello world", ["*world"]); // true
 * 
 * // URL domain matching
 * // matchesPattern("https://sub.example.com/page", ["example.com"], { treatAsUrl: true }); // true
 * // matchesPattern("https://example.com/page", ["*://example.com/*"]); // true
 * 
 * // Case-sensitive matching
 * // matchesPattern("Hello World", ["hello world"], { caseSensitive: true }); // false
 * // matchesPattern("Hello World", ["Hello World"], { caseSensitive: true }); // true
 * @param {string} value - The value to check
 * @param {string|string[]} patterns - Single pattern or array of patterns to match against
 * @param {Object} [options] - Optional configuration settings
 * @param {boolean} [options.caseSensitive=false] - Whether matching should be case-sensitive
 * @param {boolean} [options.URL=true] - Whether to treat the value as a URL for domain matching
 * @returns {boolean} - Whether the value matches any pattern
 */
function matchesPattern(value, patterns, options = {}) {
  // Default options
  const opts = {
    caseSensitive: false,
    url: true,
    ...options
  };

  // Convert single pattern to array for consistent handling
  const patternArray = Array.isArray(patterns) ? patterns : [patterns];
  if (patternArray.length === 0) return false;
  
  // Normalize value for case-insensitive matching
  const normalizedValue = opts.caseSensitive ? value : value.toLowerCase();
  
  return patternArray.some(pattern => {
    // Normalize pattern for case-insensitive matching
    const normalizedPattern = opts.caseSensitive ? pattern : pattern.toLowerCase();
    
    // Wildcard pattern matching
    if (normalizedPattern.includes('*')) {
      // Convert wildcard pattern to regex
      const regexPattern = normalizedPattern
        .replace(/\./g, '\\.')       // Escape dots
        .replace(/\//g, '\\/')       // Escape slashes
        .replace(/\*/g, '.*');       // Convert * to .*
      
      const regex = new RegExp(`^${regexPattern}$`);
      return regex.test(normalizedValue);
    }
    
    // URL domain matching (if enabled)
    if (opts.url) {
      try {
        const urlObj = new URL(value);
        const hostname = urlObj.hostname;
        
        // Check if hostname matches pattern or ends with .pattern (subdomain matching)
        return hostname === normalizedPattern || 
               hostname.endsWith(`.${normalizedPattern}`);
      } catch (error) {
        // Fallback to simple includes if URL parsing fails
        return normalizedValue.includes(normalizedPattern);
      }
    }
    
    // Simple exact or substring matching
    return normalizedValue === normalizedPattern || 
           normalizedValue.includes(normalizedPattern);
  });
}

// Example usage of UUID generation
    // this.uuid = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
    //   const r = (Math.random() * 16) | 0;
    //   const v = c === "x" ? r : (r & 0x3) | 0x8;
    //   return v.toString(16);
    // });



// Export functions
export {
  getTimezoneArray,
  countryCodes,
  languageCodes,
  getLocalesForLanguage,
  getLocalesForCountry,
  getAllSupportedLocales,
  matchesPattern,
};
