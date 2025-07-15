import { er, bang, bing, Logger, promptee, state } from "../../lib/index.js";
import { configure, BASE_URL } from "./configure.js";
import { Actor } from "../actor.js";
export class Scopeulation {
    threaded = [];
    visited = [];
    searched = [];
    base = (url) => new URL(url).href === new URL(BASE_URL).href;
    user = (url) => /\.com\/user\/[^/]+/.test(url);
    subreddit = (url) => /\/r\/[^/]+\/?$/.test(url);
    comments = (url) => /\/r\/[^/]+\/comments(?:\/.*)?$/.test(url);
    search = (url) => /\/r\/[^/]+\/search(?:\/.*)?$/.test(url);
    iterative = (url) => this.comments(url) || this.search(url) || this.user(url)
        ? url
        : url.replace(/\/?(search)?$/, "/search");
    existing(thread) {
        if (!this.threaded.some((v) => JSON.stringify(v.listing) === JSON.stringify(thread.listing))) {
            scopeulation.threaded.push(thread);
            return thread;
        }
    }
    scoped(current) {
        const url = this.visited[this.visited.length - 1];
        const scope = ["People", "Communities"].includes(current) &&
            (this.subreddit(url) || this.comments(url) || this.search(url))
            ? "Posts"
            : current;
        const Url = new URL(url);
        const type = Url.searchParams.get("type");
        const sort = Url.searchParams.get("sort");
        const t = Url.searchParams.get("t");
        const community = scope === "Communities" || type === "communities";
        const people = scope === "People" || type === "people" || this.user(url);
        return { url, scope, type, sort, t, community, people };
    }
}
export const scopeulation = new Scopeulation();
export class Reddit extends Actor {
    constructor(setup) {
        super(setup.page, setup.options, async (url) => {
            if (!setup.funco)
                return bang("No action function provided", undefined, { url });
            else
                Logger.log("Scenario URL", url);
            const pre = async () => {
                if (scopeulation.user(this.page.url()) || scopeulation.user(url)) {
                    await this.click(this.opts.args.sort === "Posts"
                        ? 'a[slot="page-2"]:has-text("Posts")'
                        : this.opts.args.sort === "Comments"
                            ? 'a[slot="page-3"]:has-text("Comments")'
                            : 'a[slot="page-1"]:has-text("Overview")');
                }
            };
            const attempter = async (func) => {
                try {
                    return await func();
                }
                catch (e) {
                    await this.backscratcher(new URL(url), e);
                    return await attempter(func);
                }
            };
            if (this.scopeulate().direct(url)) {
                return await attempter(async () => {
                    await pre();
                    return await setup.funco(url);
                });
            }
            else {
                const rank = async (func) => {
                    const locatorz = await this.navigateIntoPost().catch(async () => {
                        const scopeulator = this.scopeulate();
                        const finder = await scopeulator.findulator();
                        await this.scrollabit(6);
                        return await finder.find.locator.all();
                    });
                    try {
                        const batches = [[]];
                        for (const listing of locatorz) {
                            const idx = batches.length - 1;
                            if (batches[idx].length >= 10)
                                batches.push([]);
                            if (!(await listing.isVisible()))
                                continue;
                            const { id, content, attributes } = await this.raw(listing, false).catch();
                            batches[idx].push({ id, content, listing, attributes });
                        }
                        for (const data of batches) {
                            const promptmise = promptee.ranking({
                                task: `Score these reddit threads by relevance to for ${this.opts.settings.start.feature}. Include a rank number along with the thread ID provided.`,
                                generations: {
                                    type: "ranking",
                                    range: { min: 1, max: 1 },
                                    input: {
                                        data: data,
                                        user_intent: `This batch of threads is @${this.page.url()}`,
                                    },
                                },
                            });
                            const reply = await this.waitabit(promptmise);
                            return await func(reply[0].data.map((i) => data.find((t) => t.id === i.id)?.listing));
                        }
                    }
                    catch (error) {
                        return await func(locatorz);
                    }
                };
                if (state.testing) {
                    return await attempter(async () => {
                        const scopeulator = this.scopeulate();
                        const finder = await scopeulator.findulator();
                        await this.scrollabit(1);
                        return await this.findo(await finder.find.locator.all(), async (thread) => {
                            await pre();
                            return await setup.funco(url, thread);
                        });
                    });
                }
                else {
                    return await attempter(async () => {
                        return await rank(async (threads) => {
                            return await this.findo(threads, async (thread) => {
                                await pre();
                                return await setup.funco(url, thread);
                            });
                        });
                    });
                }
            }
        });
    }
    scopeulate() {
        const scoped = scopeulation.scoped(this.opts.args.scope);
        const click = async () => {
            if (scoped.type)
                return;
            const tab = scoped.scope === "Posts"
                ? this.page.getByRole("button", { name: scoped.scope })
                : this.page.locator(`#search-results-page-tab-${scoped.scope.toLowerCase()}`);
            await this.click(tab);
        };
        const clickSort = async () => {
            const scopes = ["Posts", "Comments", "Media"];
            const sorts = ["Hot", "Top", "New", "Comments"];
            const skips = scoped.sort || !scopes.includes(scoped.scope) || !sorts.includes(this.opts.args.sort);
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
        };
        const clickRange = async () => {
            const scopes = ["Posts", "Media"];
            const sorts = ["Relevance", "Top", "Comments"];
            const filters = ["Year", "Month", "Week", "Today", "Hour"];
            const skips = scoped.t ||
                !scopes.includes(scoped.scope) ||
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
        };
        const homepage = scopeulation.base(this.page.url()) && this.opts.settings.start.search.length === 0;
        const scopeulated = {
            ...scoped,
            homepage,
            direct: (url) => {
                const base = url ? scopeulation.comments(url) || scopeulation.user(url) : homepage;
                return base || scoped.people || scoped.community;
            },
            findulator: async () => {
                try {
                    await click();
                    await clickSort();
                    await clickRange();
                }
                catch (e) {
                    Logger.warn("Error in scopeulator setup", e);
                }
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
                const scope = mapper[scoped.scope];
                return { scope, find: await this.find(scope.ids, scope.strat) };
            },
            click,
            clickSort,
            clickRange,
        };
        return bang(`scopeulate`, scopeulated, scoped);
    }
    async backscratcher(url, error) {
        bang("backscratcher checking listing attempts", this.opts.settings.start.attempts > 0, {
            attempts: this.opts.settings.start.attempts,
            error,
        });
        if (error)
            this.opts.settings.start.attempts--;
        while (await this.page.evaluate(() => window.history.length > 1)) {
            if (new URL(this.page.url()).pathname === url.pathname)
                break;
            await this.page.goBack();
            await this.nap();
        }
    }
    async raw(locator = this.page.locator("shreddit-post").first(), screenshots = true) {
        await locator.waitFor();
        const id = crypto.randomUUID();
        const url = new URL(this.page.url());
        const attributes = await this.attributes(locator);
        const screenshot = screenshots ? await this.screenshot(locator) : "";
        const content = await locator.evaluate((root) => {
            const relevantAttrPrefixes = [
                "post-",
                "subreddit-",
                "author-",
                "content-",
                "comment-",
                "domain",
                "id",
                "title",
                "href",
                "src",
                "datetime",
            ];
            const extractAttributes = (el) => {
                const data = {};
                for (const { name, value } of el.attributes) {
                    if (relevantAttrPrefixes.some((prefix) => name.startsWith(prefix) || prefix === name)) {
                        data[name] = value;
                    }
                }
                return data;
            };
            const extractTextContent = (node) => {
                if (node.nodeType === Node.TEXT_NODE) {
                    return node.textContent?.trim() || "";
                }
                if (node.nodeType === Node.ELEMENT_NODE) {
                    return Array.from(node.childNodes)
                        .map(extractTextContent)
                        .filter(Boolean)
                        .join(" ")
                        .replace(/\s+/g, " ")
                        .trim();
                }
                return "";
            };
            const extractMedia = (el) => Array.from(el.querySelectorAll("img, video"))
                .map((node) => ({ type: node.tagName.toLowerCase(), src: node.getAttribute("src") }))
                .filter((item) => item.src);
            return {
                tag: root.tagName.toLowerCase(),
                attributes: extractAttributes(root),
                title: root.querySelector("h1")?.textContent?.trim(),
                flair: root.querySelector("shreddit-post-flair")?.textContent?.trim(),
                body: extractTextContent(root.querySelector('[slot="text-body"]') || root),
                media: extractMedia(root),
            };
        });
        return { id, url, attributes, content, screenshot };
    }
    async onWhile(url) {
        const basic = scopeulation.subreddit(url) || url === BASE_URL;
        const todo = this.opts.settings.start.urls.length + this.opts.settings.start.search.length;
        const visit = this.opts.settings.start.urls.length - scopeulation.visited.length;
        const search = this.opts.settings.start.search.length - scopeulation.searched.length;
        const searched = search === 0 && this.opts.settings.start.search.length > 0;
        const done = scopeulation.visited.length + scopeulation.searched.length;
        const stats = { todo, done, visit, search, searched, basic };
        Logger.log(`Status`, stats, scopeulation);
        return search > 0 && basic
            ? await this.searcho()
            : visit && !scopeulation.visited.includes(url)
                ? await this.navigato(url)
                : Logger.trace(`All terms completed.`);
    }
    async onReIteration(url) {
        await this.backscratcher(new URL(scopeulation.iterative(url)));
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
        const text = this.opts.settings.start.search[scopeulation.searched.length];
        const locator = this.page.locator(`faceplate-search-input`);
        const textbox = locator.getByRole("textbox");
        await this.click(textbox);
        const clearButton = locator.getByRole("button", { name: "Clear search" });
        if (await clearButton.isVisible())
            await clearButton.click().catch(() => false);
        await this.pressSequentially(textbox, text, false);
        await textbox.press("Enter");
        await this.nap();
        scopeulation.searched.push(text);
    }
    async findo(posts, funco) {
        const url = new URL(this.page.url());
        for (const listing of posts) {
            try {
                const thread = scopeulation.existing({
                    id: crypto.randomUUID(),
                    listing,
                    attributes: await this.attributes(listing),
                });
                if (!thread || !thread.listing)
                    continue;
                await this.click(thread.listing);
                await this.nap();
                return await funco(thread);
            }
            catch (error) {
                await this.backscratcher(url, error);
            }
        }
        throw er(`Failed to find a thread.`, this.opts.settings.start.attempts);
    }
    async joinConversation() {
        if (this.scopeulate().direct())
            return false;
        await this.click('a:has-text("See full discussion")', { timeout: 600 }).catch(() => false);
        await this.scrollabit();
        const archived = this.page.locator('[slot="post-archived-banner"] >> text=Archived post');
        const closed = await archived.isVisible().catch(() => false);
        bang(`checking archive`, closed === false, { closed, archived });
        const triggers = this.page.locator('comment-composer-host faceplate-textarea-input[placeholder="Join the conversation"]');
        const count = await triggers.count();
        for (let i = 0; i < count; i++) {
            const trigger = triggers.nth(i);
            if (await trigger.isVisible())
                try {
                    await trigger.click({ force: true });
                    return trigger;
                }
                catch { }
        }
        Logger.warn("Trying JS-based fallback trigger...");
        return await this.page.evaluate(() => {
            const el = document.querySelector('comment-composer-host faceplate-textarea-input[placeholder="Join the conversation"]');
            if (el)
                el.dispatchEvent(new Event("click", { bubbles: true, cancelable: true }));
            return el;
        });
    }
    async navigateIntoPost() {
        const scopeulator = this.scopeulate();
        bang("navigateIntoPost", scopeulator.direct(), { scopeulator });
        await this.scrollabit();
        const locator = this.page.locator(`a[slot='title'], shreddit-profile-comment a.absolute[href][aria-label^='Thread for']`);
        const posts = await locator.all();
        return bing("found posts", posts.length, posts, { locator, scopeulator });
    }
    async getComments(max = 36) {
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
export default async function (params, funco) {
    const config = await configure(params.ctx, params.opts);
    const reddit = new Reddit({ ...config, funco });
    await reddit.init();
    return { reddit };
}
export * from "./configure.js";
