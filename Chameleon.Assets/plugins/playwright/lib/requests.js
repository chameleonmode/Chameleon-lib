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
export async function req(path, args) {
    const from = `${await endpoint()}${path}`;
    const init = {
        headers: {
            "Content-Type": "application/json",
            ...args.headers,
        },
        method: args.method ?? "POST",
        body: args.body ? JSON.stringify(args.body) : undefined,
    };
    Logger.log("Request:", { from, args: JSON.stringify(args), init: JSON.stringify(init) });
    const request = await fetch(from, init);
    const response = await request.json();
    Logger.log("Generated:", response);
    return response;
}
