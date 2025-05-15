import { Logger } from "./logger.js";
import { req } from "./requests.js";
import { rando } from "./utils.js";
export const tones = ["sarcastic", "informative", "relatable", "straightforward"];
export async function askConsole(input) {
    console.log(`Ask:${input}`);
    const rl = (await import("node:readline")).createInterface({
        input: process.stdin,
        output: process.stdout,
    });
    return new Promise((resolve) => {
        rl.question(`> `, async (answer) => {
            if (!answer.startsWith("Ans:"))
                return;
            rl.close();
            resolve(answer.slice(4).trim());
        });
    });
}
export async function promptee(ctx) {
    ctx.decorators.tone ||= rando(tones);
    const request = await req("/promptee/prompter", {
        body: ctx,
        headers: {
            ai: "origato",
            type: ctx.generations.type,
        },
    });
    const response = request.res;
    Logger.log("Reply:", response);
    return response;
}
