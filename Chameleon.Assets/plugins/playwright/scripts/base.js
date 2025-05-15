import { expect } from "@playwright/test";
import { random, sleepRandom, tryForEach } from "../lib/utils.js";
import { promptee, tones } from "../lib/ask.js";
import { Logger } from "../lib/logger.js";
export class Base {
    ctx;
    opts;
    scenario;
    rando;
    iterations;
    variations;
    timeouts;
    visited = [];
    toner = tones;
    page;
    constructor(ctx, opts, scenario, rando = random(opts.settings.start.rando.min, opts.settings.start.rando.max), iterations = random(opts.settings.start.iterations.min, opts.settings.start.iterations.max), variations = random(opts.settings.start.variations.min, opts.settings.start.variations.max), timeouts = {
        ...opts.settings.timeouts,
        navigate: 1000 * opts.settings.timeouts.navigate,
        default: 1000 * opts.settings.timeouts.default,
        wait: 1000 * opts.settings.timeouts.wait,
    }) {
        this.ctx = ctx;
        this.opts = opts;
        this.scenario = scenario;
        this.rando = rando;
        this.iterations = iterations;
        this.variations = variations;
        this.timeouts = timeouts;
    }
    status() {
        const todo = this.opts.settings.start.urls.length;
        const done = this.visited.length;
        return { todo, done };
    }
    async init() {
        this.page = this.opts.settings.start.new
            ? await this.ctx.newPage()
            : this.ctx.pages()[this.ctx.pages().length - 1];
        this.page.setDefaultTimeout(this.timeouts.default);
        this.page.setDefaultNavigationTimeout(this.timeouts.navigate);
    }
    async navigate(url) {
        try {
            if (url)
                await this.page.goto(url, { waitUntil: "load" });
            await this.waitForNavigation();
            await this.nap();
        }
        catch (e) {
            Logger.error("Error navigating to URL:", e);
            await sleepRandom({
                min: 1000 * 7,
                max: 1000 * 14,
                multiplier: 1,
            });
            await this.navigate(url);
        }
    }
    async waitForNavigation(timeout = this.timeouts.navigate) {
        return await tryForEach([
            this.page.waitForLoadState("load", { timeout }),
            this.page.waitForLoadState("domcontentloaded", { timeout }),
        ]);
    }
    async getFocusedElement() {
        return this.page.evaluate(() => {
            const element = document.activeElement;
            return {
                element,
                tagName: element?.tagName,
                ariaLabel: element?.ariaLabel,
                textContent: element?.textContent,
            };
        });
    }
    async txtContent(selector, locator) {
        const element = locator?.locator(selector).first() || this.page.locator(selector).first();
        await expect(element).toBeVisible();
        return this.bang("Element txt content" + selector, await element.evaluate((ele) => ele?.textContent?.replace(/\s+/g, " ").trim()));
    }
    async selectAll(locator, clear = false) {
        const modifierKey = process.platform === "win32" ? "Control" : "Meta";
        await (locator ? locator.press(`${modifierKey}+A`) : this.page.keyboard.press(`${modifierKey}+A`));
        if (clear) {
            await this.nap();
            await (locator ? locator.press("Backspace") : this.page.keyboard.press("Backspace"));
        }
    }
    async type(text) {
        await this.page.keyboard.type(text, {
            delay: random(64, 128),
        });
    }
    async pressSequentially(locator, text, click = true) {
        if (click)
            await this.click(locator);
        await locator.pressSequentially(text, {
            delay: random(64, 128),
            timeout: 1000 * 60 * 5,
        });
    }
    async click(locator, timeout = this.timeouts.wait) {
        await this.nap();
        const expecto = await tryForEach([
            expect(locator).toBeEnabled({ timeout }),
            expect(locator).toBeVisible({ timeout }),
        ]);
        this.bang(`expecto: ${locator}`, !expecto.errors.length || expecto.fulfilled.length);
        const locato = await tryForEach([
            locator.waitFor({ timeout }),
            locator.scrollIntoViewIfNeeded({ timeout }),
            locator.click({ timeout, force: true }),
        ]);
        this.bang(`locato: ${locator}`, !locato.errors.length || locato.fulfilled.length);
        await this.nap();
    }
    async scrollabit() {
        for (let i = 0; i < random(3, 6); i++) {
            await this.nap();
            try {
                const { scrollTop, scrollHeight, clientHeight } = await this.page.evaluate(() => {
                    return {
                        scrollTop: window.scrollY,
                        clientHeight: document.documentElement.clientHeight,
                        scrollHeight: document.body.scrollHeight,
                    };
                });
                this.bang(`scrollHeight: ${scrollHeight}, scrollTop: ${scrollTop}, clientHeight: ${clientHeight}`, scrollTop + clientHeight <= scrollHeight);
                const direction = i > 0 && Math.random() > 0.875 ? -1 : 1;
                await this.page.mouse.wheel(0, direction * random(clientHeight / 2, clientHeight));
            }
            catch (e) {
                break;
            }
        }
    }
    async nap(args = {
        min: this.timeouts.naps.min,
        max: this.timeouts.naps.max,
        multiplier: this.timeouts.naps.multiplier,
    }) {
        const sleepo = await sleepRandom(args);
        await this.page.waitForTimeout(sleepo);
        await this.waitForNavigation();
    }
    async find(ids, strategy = "testId") {
        for (const id of ids) {
            const locator = strategy === "testId"
                ? this.page.getByTestId(id)
                : strategy === "selector"
                    ? this.page.locator(id)
                    : this.page.getByText(id);
            const count = await locator.count();
            if (count > 0) {
                return { count, locator, id };
            }
        }
        throw this.error(`No elements found for IDs: ${ids.join(", ")} using strategy: ${strategy}`);
    }
    async findFrame(selectors) {
        for (const selector of selectors) {
            try {
                const frame = this.page.frameLocator(selector);
                const frameHandle = await this.page.$(selector);
                const contentFrame = frameHandle ? await frameHandle.contentFrame() : null;
                if (contentFrame) {
                    return { frame, frameHandle, contentFrame, selector };
                }
            }
            catch (e) {
                Logger.warn(`Failed to find frame for selector: ${selector}`, e);
                continue;
            }
        }
        throw this.error(`No frames found for selectors: ${selectors.join(", ")}`);
    }
    async ask(opts) {
        const result = await promptee({
            ...this.opts.ai,
            task: opts.task,
            generations: opts.generate,
        });
        return result;
    }
    async handles(act) {
        try {
            switch (act.type) {
                case "click": {
                    const { x, y, button = "left" } = act.action;
                    Logger.log(`Action: click at (${x}, ${y}) with button '${button}'`);
                    await this.page.mouse.click(x, y, { button: button });
                    break;
                }
                case "scroll": {
                    const { x, y, scroll_x, scroll_y } = act.action;
                    Logger.log(`Action: scroll at (${x}, ${y}) with offsets (scrollX=${scroll_x}, scrollY=${scroll_y})`);
                    await this.page.mouse.move(x, y);
                    await this.page.evaluate(({ sx, sy }) => window.scrollBy(sx, sy), { sx: scroll_x, sy: scroll_y });
                    break;
                }
                case "keypress": {
                    const { keys } = act.action;
                    for (const k of keys) {
                        Logger.log(`Action: keypress '${k}'`);
                        if (k.includes("ENTER")) {
                            await this.page.keyboard.press("Enter");
                        }
                        else if (k.includes("SPACE")) {
                            await this.page.keyboard.press(" ");
                        }
                        else {
                            await this.page.keyboard.press(k);
                        }
                    }
                    break;
                }
                case "type": {
                    const { text } = act.action;
                    Logger.log(`Action: type text '${text}'`);
                    await this.page.keyboard.type(text);
                    break;
                }
                case "wait": {
                    Logger.log(`Action: wait`);
                    await this.page.waitForTimeout(2000);
                    break;
                }
                case "screenshot": {
                    Logger.log(`Action: screenshot`);
                    break;
                }
                default:
                    Logger.log("Unrecognized action:", act);
            }
        }
        catch (e) {
            Logger.error("Error handling action", act, ":", e);
        }
    }
    async handlee(action) {
        const keyMap = {
            ENTER: "Enter",
            ARROWLEFT: "ArrowLeft",
            ARROWRIGHT: "ArrowRight",
            ARROWUP: "ArrowUp",
            ARROWDOWN: "ArrowDown",
            ALT: "Alt",
            CTRL: "Control",
            SHIFT: "Shift",
            CMD: "Meta",
        };
        const modifierKeys = new Set(["Control", "Shift", "Alt", "Meta"]);
        try {
            const page = this.page;
            const { x, y, button, path, scroll_x, scroll_y, text, keys, url } = action;
            switch (action.type) {
                case "click":
                    Logger.log(`Clicking at (${x}, ${y}), ${button} button`);
                    await page.mouse.click(x, y);
                    break;
                case "double_click":
                    Logger.log(`Double clicking at (${x}, ${y})`);
                    await page.mouse.dblclick(x, y);
                    break;
                case "move":
                    Logger.log(`Moving mouse to (${x}, ${y})`);
                    await page.mouse.move(x, y);
                    break;
                case "drag":
                    Logger.log("Dragging along path", path);
                    if (Array.isArray(path) && path.length > 0) {
                        const [firstPoint, ...restPoints] = path;
                        await page.mouse.move(firstPoint.x, firstPoint.y);
                        await page.mouse.down();
                        for (const point of restPoints) {
                            await page.mouse.move(point.x, point.y);
                        }
                        await page.mouse.up();
                    }
                    else {
                        Logger.log("Drag action missing a valid path");
                    }
                    break;
                case "scroll":
                    Logger.log(`Scrolling by (${scroll_x}, ${scroll_y})`);
                    await page.mouse.wheel(scroll_x, scroll_y);
                    break;
                case "type":
                    Logger.log(`Typing text: ${text}`);
                    await page.keyboard.type(text);
                    break;
                case "keypress":
                    Logger.log(`Pressing key: ${keys}`);
                    const mappedKeys = keys.map((key) => keyMap[key.toUpperCase()] || key);
                    const modifiers = mappedKeys.filter((key) => modifierKeys.has(key));
                    const normalKeys = mappedKeys.filter((key) => !modifierKeys.has(key));
                    if ((mappedKeys[0] === "Meta" && mappedKeys[1] === "[") ||
                        (mappedKeys[0] === "Alt" && mappedKeys[1] === "ArrowLeft")) {
                        await page.goBack();
                        break;
                    }
                    for (const key of modifiers) {
                        await page.keyboard.down(key);
                    }
                    for (const key of normalKeys) {
                        await page.keyboard.press(key);
                    }
                    for (const key of modifiers) {
                        await page.keyboard.up(key);
                    }
                    break;
                case "wait":
                    Logger.log("Waiting for browser...");
                    await page.waitForTimeout(1000);
                    break;
                case "goto":
                    Logger.log(`Navigating to ${url}`);
                    await page.goto(url);
                    break;
                case "back":
                    Logger.log("Navigating back");
                    await page.goBack();
                    break;
                case "forward":
                    Logger.log("Navigating forward");
                    await page.goForward();
                    break;
                case "screenshot":
                    Logger.log("Taking a screenshot");
                    break;
                default:
                    Logger.log("Unknown action:", action);
            }
        }
        catch (error) {
            Logger.error("Error executing action:", action, error);
        }
    }
    async getScreenshotAsBase64() {
        const screenshotBuffer = await this.page.screenshot({ fullPage: true });
        return screenshotBuffer.toString("base64");
    }
    error(message, cause) {
        const error = new Error(`[${this.opts.settings.start.feature}] - [${JSON.stringify(this.opts.settings.start)}] ${message}`, { cause });
        Logger.error(`${message}`, cause);
        return error;
    }
    bang(message, expect, source) {
        Logger.debug(`Banging: ${message}`, expect, source);
        if (expect)
            return expect;
        throw this.error(message, { source, expect });
    }
}
