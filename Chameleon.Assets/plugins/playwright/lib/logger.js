export class Logger {
    static prefix = () => {
        const date = new Date();
        return `${date.toISOString().split("T")[0]} ${date.toTimeString().split(" ")[0]}`;
    };
    static suffix = (message, objects) => {
        return {
            message: JSON.stringify(message),
            objects: JSON.stringify(objects, null, 2),
        };
    };
    static log(message = "Chamelioneer", ...objects) {
        console.log(`[${this.prefix()}] \x1b[32mLOG\x1b[0m`, this.suffix(message, objects));
    }
    static info(message = "Chamelioneer", ...objects) {
        console.log(`[${this.prefix()}] \x1b[35mINFO\x1b[0m`, this.suffix(message, objects));
    }
    static debug(message = "Chamelioneer", ...objects) {
        console.log(`[${this.prefix()}] \x1b[36mDEBUG\x1b[0m`, this.suffix(message, objects));
    }
    static warn(message = "Chamelioneer", ...objects) {
        console.warn(`[${this.prefix()}()] \x1b[33mWARN\x1b[0m`, this.suffix(message, objects));
    }
    static error(message = "Chamelioneer", ...objects) {
        console.error(`[${this.prefix()}] \x1b[31mERROR\x1b[0m`, this.suffix(message, objects));
    }
}
