const ADDON_NAME = "Chromeleon Defender";

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
  currentLogLevel: LOG_LEVELS["all"],
};

const formatMessage = (level, message) => {
  const timestamp = new Date().toISOString();
  return `${timestamp} [${ADDON_NAME}] [${level}] ${message}`;
};

export const log = {
  log: (message, args = {}) => {
    if (0 <= config.currentLogLevel) {
      console.log(formatMessage("LOG", message, args));
    }
  },
  debug: (message, args = {}) => {
    if (1 <= config.currentLogLevel) {
      console.debug(formatMessage("DEBUG", message, args));
    }
  },
  info: (message, args = {}) => {
    if (2 <= config.currentLogLevel) {
      console.info(formatMessage("INFO", message, args));
    }
  },
  warn: (message, args = {}) => {
    if (3 <= config.currentLogLevel) {
      console.warn(formatMessage("WARN", message, args));
    }
  },
  error: (message, args = {}) => {
    if (4 <= config.currentLogLevel) {
      console.error(formatMessage("ERROR", message, args));
    }
  },
};


// Example of setting the log level dynamically
export function setLogLevel(level) {
  config.currentLogLevel = LOG_LEVELS[level];
}
