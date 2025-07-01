import { expect } from "@playwright/test";
import { rando, er, sleepo, tryForEach, bang } from "../lib/utils.js";
import { Logger } from "../lib/logger.js";
import { Player } from "./player.js";
export class Actor {
    page;
    opts;
    scenario;
    player = new Player(this);
    constructor(page, opts, scenario) {
        this.page = page;
        this.opts = opts;
        this.scenario = scenario;
    }
    async init() {
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
            bang("checking navigation attempts", this.opts.settings.start.attempts > attempt, this.opts.settings.start.attempts);
            await this.navigate(url, attempt + 1);
        }
    }
    async waitForNavigation(timeout = this.opts.settings.timeouts.navigate) {
        await this.page.waitForLoadState("load", { timeout });
        await this.page.waitForLoadState("domcontentloaded", { timeout });
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
    async txtContent(selector, within) {
        const locator = within?.locator(selector) || this.page.locator(selector);
        let looper = 0;
        for (const location of await locator.all()) {
            if (!(await location.isVisible()))
                continue;
            if (looper++ > 3) {
                await sleepo(this.opts.settings.timeouts.naps);
                await location.scrollIntoViewIfNeeded();
            }
            const text = await location.evaluate((ele) => ele?.textContent?.replace(/\s+/g, " ").trim());
            if (text)
                return bang("txtContent: " + selector, text, { location, text }, { print: false });
        }
        throw er(`No visible elements found for ${selector}`, locator);
    }
    async attributes(locator) {
        const attributes = await locator.evaluate((node) => {
            const attrs = {};
            for (const attr of node.attributes) {
                attrs[attr.name] = attr.value;
            }
            return attrs;
        });
        return bang("attributes: " + locator, attributes, { locator, attributes }, { print: false });
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
        await this.nap();
    }
    async assert(locator, { timeout = 1000 * 6 } = {}) {
        const expecto = await tryForEach([
            expect(locator).toBeEnabled({ timeout }),
            expect(locator).toBeVisible({ timeout }),
        ]);
        bang(`expecto: ${locator}`, !expecto.errors.length || expecto.fulfilled.length, expecto);
        await locator.waitFor({ timeout });
        return bang(`assert: ${locator}`, locator, { timeout, locator });
    }
    async click(thang, options = {}) {
        const { timeout = this.opts.settings.timeouts.wait } = options;
        await this.nap();
        const things = typeof thang === "string" ? this.page.locator(thang) : thang;
        const count = await things.count();
        const locator = count > 1 ? await (async () => {
            let nth = -1;
            while (++nth < count) {
                const locator = things.nth(nth);
                if (await locator.isVisible({ timeout }))
                    return locator;
            }
        })() : things;
        const locatoree = bang("checking element count", locator, { locator, count });
        await this.assert(locatoree, { timeout });
        await locatoree.click({ timeout, force: true });
        await this.nap();
        return bang(`clicked locator`, locator, { locator });
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
                bang(`Scroll attempt ${i + 1}/${times}: ${y} (direction: ${direction})`, y + clientHeight <= scrollHeight || scrollTop + clientHeight <= scrollHeight, { y, scrollTop, clientHeight, scrollHeight });
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
    async find(ids, strategy) {
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
                        throw er(`Unknown strategy: ${strategy}`);
                }
            })();
            try {
                const firstVisible = async (current, depth = 18, timeout = 36) => {
                    for (const location of await current.all()) {
                        if (await location.isVisible({ timeout }))
                            return location;
                        const siblings = location.locator(":scope > *");
                        for (const sibling of await siblings.all()) {
                            if (await sibling.isVisible({ timeout }))
                                return sibling;
                        }
                        if (depth > 0)
                            return firstVisible(location.locator(".."), depth - 1, timeout * 2);
                    }
                    throw er(`Max depth reached while finding visible ancestor for ${selector}`);
                };
                const locator = strategy === "testId" ? target : await firstVisible(target);
                return { target, locator, selector, count: await locator.count() };
            }
            catch (e) {
                Logger.warn(`Failed to resolve ${strategy} locator for ${selector}`, e);
            }
        }
        throw er(`No elements found for IDs: ${ids.join(", ")} using strategy: ${strategy}`);
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
        throw er(`No frames found for selectors: ${selectors.join(", ")}`);
    }
    async screenshot(locator) {
        return (await locator.screenshot({
            scale: "css",
            type: "jpeg",
            quality: 72,
        })).toString("base64");
    }
}
