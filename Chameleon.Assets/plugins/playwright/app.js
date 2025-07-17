import readline from "node:readline";
import { Browzer, delay, Logger, Playwrighteer, state } from "./lib/index.js";
async function main() {
    const args = process.argv.slice(2);
    Logger.log("Starting Runner...", args);
    const play = async (argz) => {
        const computer = new Browzer();
        await computer.runner(argz);
    };
    const commander = async (line, argz) => {
        const command = line.match(/(?:[^\s"]+|"[^"]*")+/g) || [];
        switch (command.shift()) {
            case "exit":
                Logger.log("Exiting...");
                process.exit(0);
            case "play":
                if (argz)
                    await play(argz);
                break;
            default:
                Logger.log(`Unknown command ${command}`);
        }
    };
    readline
        .createInterface({ input: process.stdin, output: process.stdout, terminal: false })
        .on("line", async (line) => {
        Logger.log(`Received`, line);
        if (line.startsWith("{")) {
            const { arg, file, port, opts, testing } = JSON.parse(line);
            switch (arg) {
                case "run":
                    await delay(1000);
                    state.testing = testing;
                    play({ file, port, opts });
                    break;
                case "cua":
                    await new Playwrighteer().cua(line);
                    break;
                default:
                    Logger.log(`Unknown command ${arg}`);
                    Logger.log("Available commands: run, exit");
            }
        }
        else {
            await commander(line);
        }
    });
    Logger.log("command ({arg: 'run', file, port, opts}, play, exit):");
    if (args.length) {
        const [file, port, opts] = args;
        state.testing = true;
        play({ file, port, opts });
    }
}
main().catch((error) => {
    Logger.log(`Error ${error}`, error);
    process.exit(1);
});
