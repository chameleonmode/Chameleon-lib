import { chromium } from "@playwright/test";
import path from "path";
import { fileURLToPath } from "url";
export async function loader(file) {
    const __filename = fileURLToPath(import.meta.url);
    const __dirname = path.dirname(__filename);
    const script = file.endsWith(".js") ? file : path.join(__dirname, `${file}.js`);
    const url = new URL(`file://${path.resolve(script)}`);
    const module = await import(url.href);
    const feature = url.href.split("/").pop()?.split(".")[0];
    return { plugin: module.default || module, feature };
}
export async function run(args) {
    try {
        console.log(`Try: ${args.file} Port: ${args.port}`);
        const { plugin, feature } = await loader(args.file);
        const browser = await chromium.connectOverCDP(`http://localhost:${args.port}`);
        const ctx = browser.contexts()[0];
        await ctx.addInitScript(() => {
            Object.defineProperty(navigator, "webdriver", { get: () => false });
            Object.defineProperty(navigator, "hardwareConcurrency", { get: () => 8 });
            Object.defineProperty(navigator, "deviceMemory", { get: () => 8 });
            const query = window.navigator.permissions.query;
            window.navigator.permissions.query = (parameters) => {
                if (parameters.name === "notifications") {
                    const result = {
                        name: "notifications",
                        state: Notification.permission,
                        onchange: null,
                        addEventListener: function () { },
                        removeEventListener: function () { },
                        dispatchEvent: function () { return false; }
                    };
                    return Promise.resolve(result);
                }
                return query(parameters);
            };
        });
        const op = args.opts;
        const opts = {
            ...op,
            run: { file: args.file, port: args.port },
            settings: {
                start: {
                    feature,
                    ...op?.settings?.start,
                }
            },
        };
        await plugin(ctx, opts);
        console.log(`Try: ${args.file} success`);
    }
    catch (error) {
        console.error(`Catch: ${args.file} ${error instanceof Error ? error.message : String(error)}`);
    }
    finally {
        console.log(`Finally: ${args.file} completed finally block`);
    }
}
