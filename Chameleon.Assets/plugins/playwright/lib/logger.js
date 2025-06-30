import util from "util";
export class Logger {
    static PRINT = true;
    static prefix = () => {
        const date = new Date();
        return `${date.toISOString().split("T")[0]} ${date.toTimeString().split(" ")[0]}`;
    };
    static getCallerLine() {
        const stack = new Error().stack;
        const callerInfo = stack?.split('\n')[4]?.trim().replace(/^at /, '') || 'unknown';
        const match = callerInfo.match(/^(.+?)\s+\((.+)\)$/);
        const method = match?.[1] || 'unknown';
        const filename = match?.[2] || 'unknown';
        if (method === 'unknown' || filename === 'unknown') {
            return { method: 'unknown', filename: stack || 'no stack available' };
        }
        return { method, filename };
    }
    static print(level, color, message, objects) {
        if (!this.PRINT)
            return;
        const callerLine = this.getCallerLine();
        const output = objects.map((o) => typeof o === "string" ? o : util.inspect(o, { depth: null, colors: true, compact: true }));
        console.log(`[${this.prefix()}] \x1b[${color}m${level}\x1b[0m \x1b[95m(${callerLine.method})\x1b[0m ${message} {\n ${callerLine.filename},\n`, ...output, `\n}`);
    }
    static log(message = "INFO", ...objects) {
        this.print("LOG", "32", message, objects);
    }
    static info(message = "INFO", ...objects) {
        this.print("INFO", "35", message, objects);
    }
    static debug(message = "DEBUG", ...objects) {
        this.print("DEBUG", "36", message, objects);
    }
    static warn(message = "WARN", ...objects) {
        this.print("WARN", "33", message, objects);
    }
    static error(message = "ERROR", ...objects) {
        this.print("ERROR", "31", message, objects);
    }
    static trace(message = "TRACE", ...objects) {
        const error = new Error("Trace log");
        this.print("TRACE", "34", message, [...objects, error]);
        return error;
    }
    static ror(message, cause) {
        const error = new Error(`${message}`, { cause });
        const pretty = { cause, stack: error.stack };
        this.error(`(error): ${error.message}`, pretty);
        return error;
    }
}
