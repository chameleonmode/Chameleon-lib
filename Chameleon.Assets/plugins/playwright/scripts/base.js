import { expect } from "@playwright/test";
import { rando, sleepRandom, tryForEach, trySequentially } from "../lib/utils.js";
import { promptee } from "../lib/requests.js";
import { Logger } from "../lib/logger.js";
export class Base {
    ctx;
    opts;
    scenario;
    iterations;
    timeouts;
    visited = [];
    page;
    constructor(ctx, opts, scenario, iterations = rando(opts.settings.start.iterations.min, opts.settings.start.iterations.max), timeouts = {
        ...opts.settings.timeouts,
        navigate: 1000 * opts.settings.timeouts.navigate,
        default: 1000 * opts.settings.timeouts.default,
        wait: 1000 * opts.settings.timeouts.wait,
    }) {
        this.ctx = ctx;
        this.opts = opts;
        this.scenario = scenario;
        this.iterations = iterations;
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
        const result = await trySequentially([
            async () => await element.scrollIntoViewIfNeeded({ timeout: this.timeouts.wait }),
        ]);
        this.banger(result, result);
        const text = await element.evaluate((ele) => ele?.textContent?.replace(/\s+/g, " ").trim());
        return this.bang("Element txt content" + selector, text);
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
            delay: rando(64, 128),
        });
    }
    async pressSequentially(locator, text, click = true) {
        if (click)
            await this.click(locator);
        await locator.pressSequentially(text, {
            delay: rando(64, 128),
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
        for (let i = 0; i < rando(3, 6); i++) {
            await this.nap();
            try {
                const { scrollTop, scrollHeight, clientHeight } = await this.page.evaluate(() => {
                    return {
                        scrollTop: window.scrollY,
                        clientHeight: document.documentElement.clientHeight,
                        scrollHeight: document.body.scrollHeight,
                    };
                });
                const direction = i > 0 && Math.random() > 0.875 ? -1 : 1;
                const y = direction * rando(clientHeight / 2, clientHeight);
                this.bang({ y, scrollTop, clientHeight, scrollHeight }, y + clientHeight <= scrollHeight || scrollTop + clientHeight <= scrollHeight);
                if (rando())
                    await this.page.mouse.wheel(0, y);
                else
                    direction > 0
                        ? await this.page.keyboard.press("PageDown")
                        : await this.page.keyboard.press("PageUp");
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
    async dimensions() {
        return await this.page.evaluate(() => {
            return {
                width: document.documentElement.scrollWidth,
                height: document.documentElement.scrollHeight,
            };
        });
    }
    async screenshot(clip = true) {
        if (clip) {
            const { width, height } = await this.dimensions();
            return (await this.page.screenshot({
                fullPage: true,
                scale: "css",
                type: "jpeg",
                quality: 18,
                clip: { x: 0, y: 0, width, height: height - height / 2 },
            })).toString("base64");
        }
        return (await this.page.screenshot({ fullPage: false })).toString("base64");
    }
    async ask(opts) {
        return await promptee.prompt({
            model: this.opts.ai.model,
            decorators: this.opts.ai.decorators,
            ...opts,
        });
    }
    error(message, cause) {
        const error = new Error(`[${this.opts.settings.start.feature}] - [${JSON.stringify(this.opts)}] ${message}`, { cause });
        Logger.error(`(error/${this.opts.settings.start.feature}) ${error.message}`, cause);
        return error;
    }
    bang(message, expect, source) {
        Logger.debug(`(bang/${this.opts.settings.start.feature}) ${message}`, expect, source);
        if (expect)
            return expect;
        throw this.error(message, { source, expect });
    }
    banger(expect, source) {
        return this.bang(``, expect, source);
    }
}
