import { random } from "../../lib/utils.js";
import { configure, BASE_URL } from "./configure.js";
import { Pager, ror } from "../pager.js";
import { Player } from "../player.js";
import { Logger } from "../../lib/logger.js";
class Scopeulation {
    visited = [];
    searched = [];
    constructor() { }
    subreddit(url) {
        const pattern = /\/r\/[^/]+\/?$/;
        return pattern.test(url);
    }
    comments(url) {
        const pattern = /\/r\/[^/]+\/comments(?:\/.*)?$/;
        return pattern.test(url);
    }
    search(url) {
        const pattern = /\/r\/[^/]+\/search(?:\/.*)?$/;
        return pattern.test(url);
    }
    user(url) {
        const pattern = /\.com\/user\/[^/]+/;
        return pattern.test(url);
    }
}
export const scopeulation = new Scopeulation();
export class Reddit extends Pager {
    ctx;
    opts;
    action;
    player = new Player(this);
    constructor(ctx, opts, action) {
        super(ctx, opts, async (url) => {
            Logger.log("Scenario URL:", url);
            const pre = async () => {
                if (scopeulation.user(this.page.url()) || scopeulation.user(url)) {
                    await this.click(this.opts.args.sort === "Posts"
                        ? 'a[slot="page-2"]:has-text("Posts")'
                        : this.opts.args.sort === "Comments"
                            ? 'a[slot="page-3"]:has-text("Comments")'
                            : 'a[slot="page-1"]:has-text("Overview")');
                }
            };
            if (action && (scopeulation.comments(url) || scopeulation.user(url))) {
                for (let i = 0; i < this.opts.settings.start.attempts; i++) {
                    try {
                        this.opts.settings.start.iterations = { min: 1, max: 1 };
                        await pre();
                        return await action(url);
                    }
                    catch (e) {
                        Logger.warn("Error in action function:", e);
                        this.opts.settings.start.attempts--;
                        while (!this.page.url().startsWith(url) && this.opts.settings.start.attempts > 0) {
                            await this.page.goBack();
                            await this.nap({ min: 50, max: 75, multiplier: random(3, 6) });
                        }
                    }
                    finally {
                        Logger.log("Action function completed");
                    }
                }
            }
            else if (action) {
                try {
                    const scopeulator = this.scopeulate();
                    try {
                        await scopeulator.click();
                        await scopeulator.clickSortOptionByText();
                        await scopeulator.clickTimeRangeByText();
                    }
                    catch (e) {
                        Logger.warn("Error in findo setup:", e);
                    }
                    const findulator = await scopeulator.findulator();
                    await this.scrollabit();
                    const threads = await findulator.find.locator.all();
                    const expecto = await this.findo(threads, async (thread) => {
                        await pre();
                        return await action(url, thread);
                    });
                    return expecto;
                }
                catch (e) {
                    Logger.warn("Error in action function:", e);
                }
                finally {
                    const text = this.opts.args.search[scopeulation.searched.length];
                    scopeulation.searched.push(text);
                    Logger.log("Action function completed", text, scopeulation.searched);
                }
            }
            else {
                Logger.warn("No action provided", url);
            }
            return undefined;
        });
        this.ctx = ctx;
        this.opts = opts;
        this.action = action;
    }
    scopeulate() {
        const url = scopeulation.visited[scopeulation.visited.length - 1];
        const scope = ["People", "Communities"].includes(this.opts.args.scope) &&
            (scopeulation.subreddit(url) || scopeulation.comments(url) || scopeulation.search(url))
            ? "Posts"
            : this.opts.args.scope;
        const Url = new URL(url);
        const type = Url.searchParams.get("type");
        const sort = Url.searchParams.get("sort");
        const t = Url.searchParams.get("t");
        const community = scope === "Communities" || type === "communities";
        const people = scope === "People" || type === "people" || scopeulation.user(url);
        const scoped = {
            url,
            scope,
            Url,
            type,
            sort,
            t,
            community,
            people,
            findulator: async () => {
                const mapper = {
                    Posts: {
                        ids: ["search-post-unit", "search-post-with-content-preview"],
                        strat: "testId",
                    },
                    Media: { ids: ["div[data-id='search-media-post-unit']"], strat: "selector" },
                    Comments: { ids: ["search-sdui-comment-unit"], strat: "testId" },
                    Communities: { ids: ["search-community"], strat: "testId" },
                    People: { ids: ["search-author"], strat: "testId" },
                };
                const scoped = mapper[scope];
                return { scope: scoped, find: await this.find(scoped.ids, scoped.strat) };
            },
            clickSortOptionByText: async () => {
                const scopes = ["Posts", "Comments", "Media"];
                const sorts = ["Hot", "Top", "New", "Comments"];
                const skips = sort || !scopes.includes(scope) || !sorts.includes(this.opts.args.sort);
                if (skips)
                    return;
                const sortLocator = this.page.locator(`search-sort-dropdown-menu`).first();
                await this.click(sortLocator);
                const normalizedText = this.opts.args.scope === "Comments" &&
                    (this.opts.args.sort === "Comments" || this.opts.args.sort === "Hot")
                    ? "Top"
                    : this.opts.args.sort.trim();
                const normalizedOption = normalizedText === "Comments" ? "Comment count" : normalizedText;
                const sortOption = this.page.locator(`li a span:has-text("${normalizedOption}")`).first();
                await sortOption.scrollIntoViewIfNeeded();
                const parentLink = sortOption.locator("xpath=./ancestor::a");
                await this.click(parentLink);
                await this.nap();
            },
            clickTimeRangeByText: async () => {
                const scopes = ["Posts", "Media"];
                const sorts = ["Relevance", "Top", "Comments"];
                const filters = ["Year", "Month", "Week", "Today", "Hour"];
                const skips = t ||
                    !scopes.includes(scope) ||
                    !sorts.includes(this.opts.args.sort) ||
                    !filters.includes(this.opts.args.filter);
                if (skips)
                    return;
                const sortLocator = this.page.locator(`search-sort-dropdown-menu`);
                await this.click(sortLocator.nth(1));
                const optionText = this.opts.args.filter === "Today"
                    ? this.opts.args.filter.trim()
                    : "Past " + this.opts.args.filter.trim().toLowerCase();
                const exactOption = this.page.locator(`li a span:has-text("${optionText}")`).first();
                const linkElement = exactOption.locator("xpath=./ancestor::a");
                await linkElement.scrollIntoViewIfNeeded();
                await this.click(linkElement);
                Logger.log(`Clicked on "${optionText}" time range option`);
            },
            click: async () => {
                if (type)
                    return;
                await this.click(scope === "Posts"
                    ? this.page.getByRole("button", { name: scope }).first()
                    : this.page.locator(`#search-results-page-tab-${scope.toLowerCase()}`).first());
            },
        };
        return Logger.return(`Scoped:`, scoped);
    }
    status() {
        const todo = this.opts.settings.start.urls.length + this.opts.args.search.length;
        const visit = this.opts.settings.start.urls.length - scopeulation.visited.length;
        const search = this.opts.args.search.length - scopeulation.searched.length;
        const searched = search === 0 && this.opts.args.search.length > 0;
        const done = scopeulation.visited.length + scopeulation.searched.length;
        const stats = { todo, done, visit, search, searched };
        return Logger.return(`Status:`, stats);
    }
    async onWhile(url) {
        const { visit, search, searched } = this.status();
        const basic = scopeulation.subreddit(url) || url === BASE_URL;
        if (searched && scopeulation.subreddit(url) && !scopeulation.visited.includes(url)) {
            scopeulation.searched.length = 0;
            return await this.onWhile(url);
        }
        return search > 0 && basic
            ? await this.searcho()
            : visit > 0 && !scopeulation.visited.includes(url)
                ? await this.navigato(url)
                : Logger.trace(`All terms completed.`);
    }
    async onReIteration(url) {
        await this.nap();
        const until = () => scopeulation.comments(url) || scopeulation.search(url) || scopeulation.user(url)
            ? url
            : url.replace(/\/?$/, "/") + "search";
        while (!this.page.url().startsWith(until())) {
            await this.page.goBack({ waitUntil: "load" });
            await this.nap({ multiplier: random(3, 6) });
        }
    }
    async navigato(url) {
        await this.navigate(url);
        scopeulation.visited.push(url);
    }
    async searcho() {
        const url = this.opts.settings.start.urls[scopeulation.visited.length];
        const navigate = scopeulation.searched.length === 0 && !scopeulation.visited.includes(url);
        if (navigate)
            await this.navigato(url);
        else
            await this.onReIteration(scopeulation.visited[scopeulation.visited.length - 1]);
        const text = this.opts.args.search[scopeulation.searched.length];
        const locator = this.page.locator(`faceplate-search-input`);
        const textbox = locator.getByRole("textbox");
        await this.click(textbox);
        const clearButton = locator.getByRole("button", { name: "Clear search" });
        if (await clearButton.isVisible().catch(() => false))
            await clearButton.click().catch(() => false);
        await this.pressSequentially(textbox, text, false);
        await this.nap({ multiplier: 3 });
        await textbox.press("Enter");
        await this.nap();
    }
    async joinConversation() {
        await this.click('a:has-text("See full discussion")', { timeout: 600 }).catch(() => false);
        await this.scrollabit(3);
        const archived = this.page.locator('[slot="post-archived-banner"] >> text=Archived post');
        const closed = await archived.isVisible().catch(() => false);
        this.banger(!closed, archived);
        const triggers = this.page.locator('comment-composer-host faceplate-textarea-input[placeholder="Join the conversation"]');
        const count = await triggers.count();
        let clicked = false;
        for (let i = 0; i < count; i++) {
            const trigger = triggers.nth(i);
            if (await trigger.isVisible()) {
                try {
                    await trigger.click({ force: true });
                    clicked = true;
                    break;
                }
                catch (err) {
                    Logger.warn(`Click failed on visible trigger #${i}:`, err);
                }
            }
        }
        if (!clicked) {
            Logger.warn("Trying JS-based fallback trigger...");
            await this.page.evaluate(() => {
                const el = document.querySelector('comment-composer-host faceplate-textarea-input[placeholder="Join the conversation"]');
                if (el)
                    el.dispatchEvent(new Event("click", { bubbles: true, cancelable: true }));
            });
        }
        const editor = this.page.locator('shreddit-composer div[contenteditable="true"]');
        await editor.waitFor({ state: "visible", timeout: 5000 });
        await editor.click({ force: true });
        return editor;
    }
    async findo(posts, funco) {
        const url = new URL(this.page.url());
        for (const listing of posts) {
            this.banger(this.opts.settings.start.attempts > 0, this.opts.settings.start.attempts);
            const existing = this.player.state.visited.some((v) => JSON.stringify(v.listing) === JSON.stringify(listing));
            if (existing)
                continue;
            try {
                const thread = { listing, attributes: await this.attributes(listing) };
                this.player.state.visited.push(thread);
                await thread.listing.scrollIntoViewIfNeeded();
                await this.nap();
                await thread.listing.click({ position: { x: 5, y: 5 } });
                await this.nap();
                return await funco(thread);
            }
            catch {
                this.opts.settings.start.attempts--;
                while (true && this.opts.settings.start.attempts > 0) {
                    const pUrl = new URL(this.page.url());
                    if (pUrl.pathname === url.pathname)
                        break;
                    await this.page.goBack();
                    await this.nap();
                }
            }
        }
        throw ror(`Failed to find a thread with open comments after ${this.opts.settings.start.attempts} attempts.`);
    }
    async navigateIntoPost() {
        const scopeulator = this.scopeulate();
        if (!scopeulator.community && !scopeulator.people)
            return;
        await this.scrollabit();
        const posts = this.page.locator(`a[slot='title'], shreddit-profile-comment a.absolute[href][aria-label^='Thread for']`);
        return await posts.all();
    }
    async getComments(max = 1000) {
        const loca = this.page.locator("shreddit-comment");
        const count = await loca.count();
        const length = Math.min(max, count);
        const comments = [];
        for (let i = 1; i < length; i++) {
            try {
                const locator = loca.nth(i);
                const text = await this.txtContent("div[slot='comment']", locator);
                const attributes = await this.attributes(locator);
                comments.push({ id: crypto.randomUUID(), index: i, text, attributes, locator });
            }
            catch (error) {
                Logger.warn(`Error processing comment ${i}:`, error);
                continue;
            }
        }
        return comments;
    }
}
export default async function (ctx, opts, action) {
    const options = configure(opts);
    const reddit = new Reddit(ctx, options, action);
    await reddit.init();
    Logger.info("Feature:", {
        feature: options.settings.start.feature,
        artifacts: options.args.artifacters,
    });
    Logger.info("Options:", {
        options: options,
    });
    return { reddit };
}
