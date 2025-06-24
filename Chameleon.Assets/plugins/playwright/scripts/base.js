import { expect } from "@playwright/test";
import { error, rando, sleepo, tryForEach, trySequentially } from "../lib/utils.js";
import { promptee } from "../lib/requests.js";
import { Logger } from "../lib/logger.js";
export class Base {
    ctx;
    opts;
    scenario;
    visited = [];
    page;
    constructor(ctx, opts, scenario) {
        this.ctx = ctx;
        this.opts = opts;
        this.scenario = scenario;
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
        this.page.setDefaultTimeout(this.opts.settings.timeouts.default);
        this.page.setDefaultNavigationTimeout(this.opts.settings.timeouts.navigate);
    }
    async navigate(url, attempt = 0) {
        try {
            if (url)
                await this.page.goto(url, { waitUntil: "load" });
            await this.waitForNavigation();
            await this.nap();
        }
        catch (e) {
            Logger.error("Error navigating to URL:", e);
            await sleepo({ min: 1000 * 7, max: 1000 * 14, multiplier: 1 });
            this.banger(this.opts.settings.start.attempts > attempt++);
            await this.navigate(url, attempt);
        }
    }
    async waitForNavigation(timeout = this.opts.settings.timeouts.navigate) {
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
        const location = locator?.locator(selector) || this.page.locator(selector);
        const locations = await location.count();
        this.bang(`firstVisible: ${location}`, locations > 0, { location, locations });
        for (let i = 0; i < locations; i++) {
            const element = location.nth(i);
            if (await element.isVisible()) {
                await element.scrollIntoViewIfNeeded();
                const text = await element.evaluate((ele) => ele?.textContent?.replace(/\s+/g, " ").trim());
                if (!text)
                    continue;
                return this.bang("txtContent: " + selector, text, { element, text });
            }
        }
        throw error(`No visible elements found for selector: ${location}`, { locations, location });
    }
    async attributes(locator) {
        const attributes = await locator.evaluate((node) => {
            const attrs = {};
            for (const attr of node.attributes) {
                attrs[attr.name] = attr.value;
            }
            return attrs;
        });
        return this.bang("attributes: " + locator, attributes, { locator, attributes });
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
    async assert(locator, { timeout = 1000 * 6 } = {}) {
        const expecto = await tryForEach([
            expect(locator).toBeEnabled({ timeout }),
            expect(locator).toBeVisible({ timeout }),
        ]);
        this.bang(`expecto: ${locator}`, !expecto.errors.length || expecto.fulfilled.length, expecto);
        await locator.waitFor({ timeout });
        return this.bang(`assert: ${locator}`, locator, { timeout, locator });
    }
    async click(thang, options = {}) {
        const locator = typeof thang === "string" ? this.page.locator(thang).first() : thang;
        const { strict = true, timeout = this.opts.settings.timeouts.wait } = options;
        await this.nap();
        const count = await locator.count();
        this.banger(count, { locator, count });
        if (strict) {
            await this.assert(locator, { timeout });
        }
        const locato = await trySequentially([() => locator.scrollIntoViewIfNeeded({ timeout }), () => locator.click({ timeout, force: true })], { first: false });
        this.bang(`locato: ${locator}`, !locato.errors.length || locato.fulfilled.length, locato);
        await this.nap();
        return this.bang(`click: ${locator}`, locator, { options, locator });
    }
    async scrollabit(times = rando(3, 6)) {
        for (let i = 0; i < times; i++) {
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
    async nap(args) {
        const qargs = { ...this.opts.settings.timeouts.naps, ...args };
        const sleep = await sleepo(qargs);
        await this.page.waitForTimeout(sleep);
        await this.waitForNavigation();
    }
    async find(ids, strategy = "testId") {
        for (const selector of ids) {
            const target = (() => {
                switch (strategy) {
                    case "testId":
                        return this.page.getByTestId(selector);
                    case "selector":
                        return this.page.locator(selector);
                    case "text":
                        return this.page.getByText(selector);
                    default:
                        throw error(`Unknown strategy: ${strategy}`);
                }
            })();
            try {
                const findVisibleAncestor = async (current, maxDepth = 25, timeout = 50) => {
                    Logger.log(`Finding visible ancestor for ${selector} with max depth ${maxDepth}`);
                    if (maxDepth < 0)
                        throw error(`Max depth reached while finding visible ancestor for ${selector}`);
                    for (const location of await current.all()) {
                        if (await location.isVisible({ timeout }).catch(() => false))
                            return location;
                        const siblings = location.locator(":scope > *");
                        for (const sibling of await siblings.all()) {
                            Logger.log(`Sibling: <${location}>`, sibling);
                            if (await sibling.isVisible({ timeout }).catch(() => false))
                                return sibling;
                        }
                        return findVisibleAncestor(location.locator(".."), maxDepth - 1);
                    }
                    throw error(`No visible ancestor found for ${selector}`);
                };
                const locator = strategy === "testId" ? target : await findVisibleAncestor(target);
                return { target, locator, selector, count: await locator.count() };
            }
            catch (e) {
                Logger.warn(`Failed to resolve ${strategy} locator for ${selector}`, e);
            }
        }
        throw error(`No elements found for IDs: ${ids.join(", ")} using strategy: ${strategy}`);
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
        throw error(`No frames found for selectors: ${selectors.join(", ")}`);
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
    bang(message, expect, source) {
        Logger.debug(`(bang/${this.opts.settings.start.feature}): ${message}`, expect, source);
        if (expect)
            return expect;
        throw error(message, { source, expect });
    }
    banger(expect, source) {
        return this.bang(``, expect, source);
    }
    bing(expect, returnz, source) {
        if (this.banger(expect))
            return returnz;
        throw error("", { source, expect });
    }
}
