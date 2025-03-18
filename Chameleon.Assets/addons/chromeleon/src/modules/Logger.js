/**
 * Logger
 *
 * A singleton logger module with support for different log levels and colorized output.
 */

// Log levels in ascending order of severity
const LOG_LEVELS = {
  none: -1,
  log: 0,
  debug: 1,
  info: 2,
  warn: 3,
  error: 4,
  all: 5,
};

// Define colors for different log levels with distinct differences
const LOG_COLORS = {
  LOG: "color: #6C757D", // gray
  DEBUG: "color: #17A2B8", // cyan
  INFO: "color: #28A745", // green
  WARN: "color: #FFC107; background: rgba(255, 193, 7, 0.1)", // yellow with light background
  ERROR: "color: #DC3545; font-weight: bold; background: rgba(220, 53, 69, 0.1)", // red and bold with light background
};

// The console method mapping to ensure we're using the right methods
const CONSOLE_METHODS = {
  LOG: "log",
  DEBUG: "debug",
  INFO: "info",
  WARN: "warn",
  ERROR: "error",
};

/**
 * Singleton Logger class
 */
class Logger {
  /**
   * Private constructor for singleton implementation
   */
  constructor() {
    // Initialize with default config
    this._config = {
      currentLogLevel: LOG_LEVELS.all,
      namespace: "App",
      showTimestamp: true,
      showCaller: true,
    };

    // Create log methods
    this._createLogMethods();
  }

  /**
   * Get the singleton instance
   * @returns {Logger} The singleton logger instance
   */
  static getInstance() {
    if (!Logger._instance) {
      Logger._instance = new Logger();
    }

    return Logger._instance;
  }

  /**
   * Set the current log level
   * @param {string|number} level - Log level (name or number)
   * @returns {Logger} - Returns this for chaining
   */
  setLogLevel(level) {
    if (typeof level === "string") {
      if (LOG_LEVELS[level] !== undefined) {
        this._config.currentLogLevel = LOG_LEVELS[level];
      } else {
        console.error(`Unknown log level: ${level}`);
      }
    } else if (typeof level === "number") {
      if (level >= LOG_LEVELS.none && level <= LOG_LEVELS.all) {
        this._config.currentLogLevel = level;
      } else {
        console.error(`Invalid log level number: ${level}`);
      }
    }

    return this;
  }

  /**
   * Set the logger namespace
   * @param {string} namespace - Namespace to use in log prefixes
   * @returns {Logger} - Returns this for chaining
   */
  setNamespace(namespace) {
    this._config.namespace = namespace;
    return this;
  }

  /**
   * Set whether to show timestamps in logs
   * @param {boolean} show - Whether to show timestamps
   * @returns {Logger} - Returns this for chaining
   */
  showTimestamp(show) {
    this._config.showTimestamp = !!show;
    return this;
  }

  /**
   * Set whether to show caller information in logs
   * @param {boolean} show - Whether to show caller info
   * @returns {Logger} - Returns this for chaining
   */
  showCaller(show) {
    this._config.showCaller = !!show;
    return this;
  }

  /**
   * Create a child logger with a new namespace
   * @param {string} namespace - Namespace for the child logger
   * @returns {Logger} - A new logger instance with the specified namespace
   */
  createChild(namespace) {
    const childLogger = new Logger();

    // Inherit settings from parent
    childLogger._config = { ...this._config };

    // Set the new namespace
    childLogger._config.namespace = namespace;

    return childLogger;
  }

  /**
   * Get the caller information from stack trace
   * @private
   * @returns {Object} - Object with file and line information
   */
  _getCallerInfo() {
    const err = new Error();
    const stack = err.stack.split("\n");
    // We need to go deeper in the stack to skip the logger methods
    const callerLine = stack[4] || "";

    const match =
      callerLine.match(/at\s+(.*)\s+\((.*):(\d+):(\d+)\)/) || callerLine.match(/at\s+(.*):(\d+):(\d+)/);

    if (match) {
      const isDetailed = match.length > 4;
      return {
        file: isDetailed ? match[2].split("/").pop() : match[1].split("/").pop(),
        line: isDetailed ? match[3] : match[2],
      };
    }

    return { file: "unknown", line: "?" };
  }

  /**
   * Create log methods for each log level
   * @private
   */
  _createLogMethods() {
    Object.keys(CONSOLE_METHODS).forEach((level) => {
      const levelValue = LOG_LEVELS[level.toLowerCase()];

      // Create the method and bind it to this instance
      this[level.toLowerCase()] = (message, args = {}) => {
        if (levelValue <= this._config.currentLogLevel) {
          let logPrefix = `[${this._config.namespace}] [${level}]`;

          // Add caller info if enabled
          if (this._config.showCaller) {
            const caller = this._getCallerInfo();
            logPrefix += ` [${caller.file}:${caller.line}]`;
          }

          // Add timestamp if enabled
          const timestampPrefix = this._config.showTimestamp ? `${new Date().toISOString()} ` : "";

          // Get the appropriate console method
          const consoleMethod = CONSOLE_METHODS[level];

          // For browsers that support CSS styling in console
          console[consoleMethod](
            `%c${timestampPrefix}${logPrefix}%c ${message}`,
            LOG_COLORS[level],
            "color: inherit",
            args
          );
        }
      };
    });
  }

  /**
   * Get all available log levels
   * @returns {Object} - The log levels object
   */
  get levels() {
    return { ...LOG_LEVELS };
  }

  /**
   * Get the current log level
   * @returns {number} - The current log level value
   */
  get currentLevel() {
    return this._config.currentLogLevel;
  }

  /**
   * Get the current log level name
   * @returns {string} - The current log level name
   */
  get currentLevelName() {
    return Object.keys(LOG_LEVELS).find((key) => LOG_LEVELS[key] === this._config.currentLogLevel);
  }
}

// Initialize the singleton instance
Logger._instance = null;

// Export a pre-created instance
export const logger = Logger.getInstance();


// For cases where direct access to the class is needed
export default Logger;
