import readline from "node:readline";
import { Logger } from "./lib/logger.js";
import { Playwrighteer } from "./lib/computers/browser.js";
async function main() {
    readline
        .createInterface({ input: process.stdin, output: process.stdout, terminal: false })
        .on("line", async (line) => {
        if (line.startsWith("{")) {
            const jsonLine = JSON.parse(line);
            switch (jsonLine.arg) {
                case "run":
                    const computer = new Playwrighteer();
                    computer.runner({ file: jsonLine.file, port: jsonLine.port, opts: jsonLine.opts });
                    break;
                default:
                    Logger.log(`Unknown command: ${jsonLine.arg}`);
                    Logger.log("Available commands: run, exit");
            }
        }
        else {
            Logger.log(`Received: ${line}`);
            const command = line.match(/(?:[^\s"]+|"[^"]*")+/g) || ["play"];
            switch (command.shift()) {
                case "exit":
                    Logger.log("Exiting...");
                    process.exit(0);
                case "play":
                    const args = process.argv.slice(2);
                    const playwrighter = new Playwrighteer();
                    if (!args[0].startsWith("{")) {
                        const [file, port, dir, opts] = args;
                        await playwrighter.runner({ file, port, dir, opts: opts ? JSON.parse(opts) : undefined });
                    }
                    else
                        await playwrighter.cua(args[0]);
                    break;
                default:
                    Logger.log(`Unknown command: ${command}`);
            }
        }
    });
    Logger.log("command ({arg: 'run', file, port, options}, play, exit):");
}
main().catch((error) => {
    Logger.log(`Error: ${error}`, error);
    process.exit(1);
});
