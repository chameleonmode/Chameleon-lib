import { Logger } from "./logger.js";
export const state = { api: undefined };
export async function endpoint() {
    return (state.api ||= await (async () => {
        try {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 300);
            await fetch("http://127.0.0.1:3042", { signal: controller.signal });
            clearTimeout(timeoutId);
            return "http://127.0.0.1:3042";
        }
        catch (error) {
            return "https://chameleon-ws.onrender.com";
        }
    })());
}
export async function req(route, args) {
    const from = `${await endpoint()}${route}`;
    const init = {
        headers: {
            "Content-Type": "application/json",
            ...args.headers,
        },
        method: args.method ?? "POST",
        body: args.body ? JSON.stringify(args.body) : undefined,
    };
    Logger.log("Request:", { to: from });
    const request = await fetch(from, init);
    const response = await request.json();
    Logger.log("Response:", response);
    return response;
}
export var promptee;
(function (promptee) {
    async function requesito(route, ctx) {
        ctx.decorators.tone ||= "adaptive to the task, data, user metadata and user intent";
        const args = { headers: { ai: "origato", model: ctx.model }, body: ctx };
        Logger.log("Requesting:", ctx.generations);
        return await req("/robo/" + route, args);
    }
    function responsito(request) {
        const out = request.reply;
        return out;
    }
    async function prompt(ctx) {
        const request = await requesito("prompt", ctx);
        return responsito(request);
    }
    promptee.prompt = prompt;
    async function genorate(ctx) {
        const request = await requesito("genorate", ctx);
        return responsito(request);
    }
    promptee.genorate = genorate;
    async function robot(ctx) {
        const request = await requesito("robot", ctx);
        return responsito(request);
    }
    promptee.robot = robot;
})(promptee || (promptee = {}));
