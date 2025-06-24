import util from "util";
export class Logger {
    static prefix = () => {
        const date = new Date();
        return `${date.toISOString().split("T")[0]} ${date.toTimeString().split(" ")[0]}`;
    };
    static print(level, color, message, objects) {
        const output = objects.map((o) => typeof o === "string" ? o : util.inspect(o, { depth: null, colors: true, compact: true }));
        console.log(`[${this.prefix()}] \x1b[${color}m${level}\x1b[0m`, message, ...output);
    }
    static log(message = "Chamelioneer", ...objects) {
        this.print("LOG", "32", message, objects);
    }
    static info(message = "Chamelioneer", ...objects) {
        this.print("INFO", "35", message, objects);
    }
    static debug(message = "Chamelioneer", ...objects) {
        this.print("DEBUG", "36", message, objects);
    }
    static warn(message = "WARN", ...objects) {
        this.print("WARN", "33", message, objects);
    }
    static error(message = "ERROR", ...objects) {
        this.print("ERROR", "31", message, objects);
    }
}
