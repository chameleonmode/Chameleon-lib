import { chromium } from "@playwright/test";
import { run } from "../runner.js";
import { req } from "../requests.js";
import { Logger } from "../logger.js";
export const CUA_KEY_TO_PLAYWRIGHT_KEY = {
    "/": "Divide",
    "\\": "Backslash",
    alt: "Alt",
    arrowdown: "ArrowDown",
    arrowleft: "ArrowLeft",
    arrowright: "ArrowRight",
    arrowup: "ArrowUp",
    backspace: "Backspace",
    capslock: "CapsLock",
    cmd: "Meta",
    ctrl: "Control",
    delete: "Delete",
    end: "End",
    enter: "Enter",
    esc: "Escape",
    home: "Home",
    insert: "Insert",
    option: "Alt",
    pagedown: "PageDown",
    pageup: "PageUp",
    shift: "Shift",
    space: " ",
    super: "Meta",
    tab: "Tab",
    win: "Meta",
};
export async function cua(input, display) {
    const body = { input, display };
    const headers = { ai: "cua", type: "roo" };
    return await req("/promptee/agent", { body, headers });
}
export class Playwrighteer {
    browser;
    ctx;
    page;
    funkers = [];
    constructor() { }
    async setup(port) {
        const connect = async () => {
            const browser = await chromium.connectOverCDP(`http://localhost:${port}`);
            return { port, browser };
        };
        try {
            return await connect();
        }
        catch (error) {
            await new Promise((resolve) => setTimeout(resolve, 3000));
            return await this.setup(port);
        }
    }
    async runner(args) {
        const { file, port, dir, opts } = args;
        await run({
            file,
            opts,
            browser: (await this.setup(port ? parseInt(port, 10) : 9613)).browser,
        });
    }
    async cua(args) {
        const { port = 9613, inputs = [
            { role: "user", content: "go to https://loadmill-center-12baa23ad9e4.herokuapp.com/" },
            { role: "user", content: "Start a new chat" },
            { role: "user", content: "Write a hello world message in the chat and Send it" },
            { role: "user", content: "Go back to the previous page" },
            { role: "user", content: "Go to the agent login" },
            { role: "user", content: "Enter user login info a@b.com and the pass 123456 and login" },
            { role: "user", content: "reply 'ok' to the first message" },
        ], } = JSON.parse(args);
        const { browser } = await this.setup(port);
        this.browser = browser;
        this.ctx = this.browser.contexts()[0] || (await this.browser.newContext());
        this.page = await this.ctx.newPage();
        const items = [
            {
                role: "system",
                content: "You running on nodeJS + playwright + " + process.platform,
            },
            {
                role: "developer",
                content: "Use the back() or goto() functions to navigate the browser",
            },
        ];
        const shifted = [];
        while (inputs.length) {
            const input = inputs.shift();
            if (!input)
                break;
            try {
                shifted.push(input);
                const response = await this.runFullTurn([...items, input]);
                items.push(...response);
            }
            catch (e) {
                Logger.warn("", e);
                inputs.unshift(shifted.pop() || input);
            }
        }
    }
    async handleItem(item) {
        Logger.debug("handleItem", { ...item });
        if (item.type === "message") {
            Logger.debug(item.content[0]);
        }
        if (item.type === "reasoning") {
            Logger.debug(item.summary[0]);
        }
        else if (item.type === "function_call") {
            const funk = item.name;
            const args = JSON.parse(item.arguments);
            const functioneer = {
                type: "function_call_output",
                call_id: item.call_id,
                output: await this.funkytime({ funk, args }),
            };
            return [functioneer];
        }
        else if (item.type === "computer_call") {
            const { type: funk, ...args } = item.action;
            await this.funkytime({ funk, args });
            const pendingChecks = item.pending_safety_checks || [];
            for (const check of pendingChecks) {
                const message = check.message;
                this.acknowledgeSafetyCheckCallback(message);
            }
            const callOutput = {
                type: "computer_call_output",
                call_id: item.call_id,
                acknowledged_safety_checks: pendingChecks,
                output: {
                    type: "input_image",
                    image_url: `data:image/png;base64,${await this.screenshot()}`,
                },
            };
            return [callOutput];
        }
        return [];
    }
    async runFullTurn(inputItems) {
        const newItems = [];
        while (newItems.length === 0 || newItems[newItems.length - 1].role !== "assistant") {
            const response = await cua(inputItems.concat(newItems), await this.getDimensions());
            if (!response.output) {
                Logger.error("", response);
                throw new Error("No output from model");
            }
            newItems.push(...response.output);
            for (const item of response.output) {
                const handled = await this.handleItem(item);
                newItems.push(...handled);
            }
        }
        return newItems;
    }
    async teardown() {
        await this.page?.close();
    }
    async funkytime(funka) {
        Logger.log("Funky time:", funka);
        await new Promise((resolve) => setTimeout(resolve, 1000));
        const { funk, args } = funka;
        await this.page.focus("body");
        if (funk !== "screenshot") {
            const frunker = this.funkers.length ? this.funkers[this.funkers.length - 1] : undefined;
            Logger.debug("frunker !== funker", frunker !== funka, JSON.stringify(frunker), JSON.stringify(funka));
            if (!frunker || frunker !== funka)
                await this[funk](args);
            this.funkers.push(funka);
        }
        return await new Promise((resolve) => setTimeout(() => resolve("success"), 3000));
    }
    async getDimensions() {
        const viewport = this.page.viewportSize() ||
            (await this.page.evaluate(() => {
                return {
                    width: window.innerWidth,
                    height: window.innerHeight,
                };
            }));
        return viewport ?? { width: 1024, height: 768 };
    }
    async screenshot() {
        const pngBuffer = await this.page.screenshot({ fullPage: false });
        return pngBuffer.toString("base64");
    }
    async click(args) {
        const { x, y, button = "left" } = args;
        await this.page.mouse.click(x, y, { button: button || "left" });
    }
    async doubleClick(args) {
        const { x, y } = args;
        await this.page.mouse.dblclick(x, y);
    }
    async scroll(args) {
        const { x, y, scroll_x, scroll_y } = args;
        await this.page.mouse.move(x, y);
        await this.page.evaluate(`window.scrollBy(${scroll_x}, ${scroll_y})`);
    }
    async type(args) {
        const { text } = args;
        await this.page.keyboard.type(text);
    }
    async wait(args = {}) {
        const { ms = 1000 } = args;
        await new Promise((resolve) => setTimeout(resolve, ms));
    }
    async move(args) {
        const { x, y } = args;
        await this.page.mouse.move(x, y);
    }
    async keypress(args) {
        const { keys } = args;
        const mappedKeys = keys.map((key) => CUA_KEY_TO_PLAYWRIGHT_KEY[key.toLowerCase()] || key);
        for (const key of mappedKeys) {
            await this.page.keyboard.down(key);
        }
        for (const key of mappedKeys.reverse()) {
            await this.page.keyboard.up(key);
        }
    }
    async drag(args) {
        const { path } = args;
        if (!path || path.length === 0)
            return;
        await this.page.mouse.move(path[0].x, path[0].y);
        await this.page.mouse.down();
        for (const point of path.slice(1)) {
            await this.page.mouse.move(point.x, point.y);
        }
        await this.page.mouse.up();
    }
    async goto(args) {
        const { url } = args;
        try {
            return await this.page.goto(url);
        }
        catch (e) {
            Logger.error(`Error navigating to ${url}: ${e}`);
        }
    }
    async back() {
        return await this.page.goBack();
    }
    async forward() {
        return await this.page.goForward();
    }
}
