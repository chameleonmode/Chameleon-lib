const ADDON_NAME = "Chromeleon Defender";

const LOG_LEVELS = {
  LOG: 0,
  DEBUG: 1,
  INFO: 2,
  WARN: 3,
  ERROR: 4,
};

const config = {
  currentLogLevel: LOG_LEVELS["DEBUG"],
};

const formatMessage = (level, message) => {
  const timestamp = new Date().toISOString();
  return `${timestamp} [${ADDON_NAME}] [${level}] ${message}`;
};

export const log = {
  log: (message, ...args) => {
    if (0 <= config.currentLogLevel) {
      console.log(formatMessage("LOG", message), ...args);
    }
  },
  debug: (message, ...args) => {
    if (1 <= config.currentLogLevel) {
      console.debug(formatMessage("DEBUG", message), ...args);
    }
  },
  info: (message, ...args) => {
    if (2 <= config.currentLogLevel) {
      console.info(formatMessage("INFO", message), ...args);
    }
  },
  warn: (message, ...args) => {
    if (3 <= config.currentLogLevel) {
      console.warn(formatMessage("WARN", message), ...args);
    }
  },
  error: (message, ...args) => {
    if (4 <= config.currentLogLevel) {
      console.error(formatMessage("ERROR", message), ...args);
    }
  },
};

export default function logger(type, message, ...args) {
  if (config.currentLogLevel < 0) return;
  switch (type) {
    case "log":
      log.debug(message, ...args);
      break;
    case "error":
      log.error(message, ...args);
      break;
    case "warn":
      log.warn(message, ...args);
      break;
    case "info":
      log.info(message, ...args);
      break;
    default:
      log.debug(message, ...args);
  }
}

// Example of setting the log level dynamically
export function setLogLevel(level) {
  config.currentLogLevel = level;
}
