import { state } from "./index.js";
import { Logger } from "./logger.js";
import { bang } from "./utils.js";
export var promptee;
(function (promptee) {
    promptee.heading = { "Content-Type": "application/json", ai: "origato" };
    async function endpoint(route) {
        const from = `${(state.api ||= await (async () => {
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
        })())}/${route}`;
        return bang("Fetching", from);
    }
    promptee.endpoint = endpoint;
    function promptio(ctx) {
        const prompt = {
            ...ctx,
            model: ctx.model || "o4-mini",
            task: bang("prompt request task", ctx.task),
            decorators: bang("prompt request decorators", state.ai?.decorators, state),
            generations: bang("prompt request generations", ctx.generations),
        };
        const headers = { ...promptee.heading, model: prompt.model };
        return { method: "POST", headers, body: JSON.stringify(prompt) };
    }
    async function requesito(route, ctx) {
        const request = await fetch(await endpoint(route), promptio(ctx));
        const out = bang("request response", await request.json());
        return out.reply;
    }
    async function ranking(ctx) {
        return await requesito("robo/ranking", ctx);
    }
    promptee.ranking = ranking;
    async function content(ctx) {
        return await requesito("robo/content", ctx);
    }
    promptee.content = content;
})(promptee || (promptee = {}));
export async function req(route, args) {
    const from = await promptee.endpoint(route);
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
