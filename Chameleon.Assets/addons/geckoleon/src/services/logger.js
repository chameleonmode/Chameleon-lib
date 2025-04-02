import App from "../app.js";

const LOG_LEVELS = {
  none: -1,
  log: 0,
  debug: 1,
  info: 2,
  warn: 3,
  error: 4,
  all: 5,
};

const config = {
  currentLogLevel: LOG_LEVELS.all,
};

// Define colors for different log levels with more distinct differences
const LOG_COLORS = {
  LOG: "color: #6C757D", // gray
  DEBUG: "color: #17A2B8", // cyan
  INFO: "color: #28A745", // green
  WARN: "color: #FFC107; background: rgba(255, 193, 7, 0.1)", // yellow with light background
  ERROR: "color: #DC3545; font-weight: bold; background: rgba(220, 53, 69, 0.1)", // red and bold with light background
};
// Get the caller info from stack trace
const getCallerInfo = () => {
  const err = new Error();
  const stack = err.stack.split("\n");
  const callerLine = stack.slice(-2).find(item => item !== undefined) || "";

  const match =
    callerLine.match(/at\s+(.*)\s+\((.*):(\d+):(\d+)\)/) || 
    callerLine.match(/at\s+(.*):(\d+):(\d+)/) ||
    callerLine.match(/@(.*):(\d+):(\d+)/);

  if (match) {
    const isDetailed = match.length > 4;
    const filePath = isDetailed ? match[2] : match[1];
    return {
      file: filePath.split("/").pop(),
      line: isDetailed ? match[3] : match[2],
    };
  }

  return { file: "unknown", line: "?" };
};

// The console method mapping to ensure we're using the right methods
const CONSOLE_METHODS = {
  LOG: "log",
  DEBUG: "debug",
  INFO: "info",
  WARN: "warn",
  ERROR: "error",
};

// Logger factory function with fixed console method mapping
const createLogMethod = (level, levelValue) => {
  return (message, args = {}) => {
    if (levelValue <= LOG_LEVELS[App.config.log]) {
      const caller = getCallerInfo();
      const timestamp = new Date().toISOString();
      const logPrefix = `[Chromeleon] [${level}] [${caller.file}:${caller.line}]`;
      const consoleMethod = CONSOLE_METHODS[level];

      try {
        // For browsers that support CSS styling in console
        console[consoleMethod](
          `%c${timestamp} ${logPrefix}%c ${message}`,
          LOG_COLORS[level],
          "color: inherit",
          args
        );
      } catch (e) {
        // Fallback for browsers that don't support console methods
        console[consoleMethod](logPrefix, message);
      }
    }
  };
};

// Define the log object explicitly for intellisense
export const log = {
  log: createLogMethod("LOG", 0),
  debug: createLogMethod("DEBUG", 1),
  info: createLogMethod("INFO", 2),
  warn: createLogMethod("WARN", 3),
  error: createLogMethod("ERROR", 4),
  setLogLevel: (level) => {
    config.currentLogLevel = LOG_LEVELS[level];
  },
};
