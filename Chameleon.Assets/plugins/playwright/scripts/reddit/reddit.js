import { er, bang, bing, delay } from "../../lib/utils.js";
import { configure, BASE_URL, scopeulation, } from "./configure.js";
import { Actor } from "../actor.js";
import { Logger } from "../../lib/logger.js";
import { promptee } from "../../lib/requests.js";
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
            if (scopeulation.comments(url) || scopeulation.user(url)) {
                const attempter = async () => {
                    try {
                        await pre();
                        return await setup.funco(url);
                    }
                    catch (e) {
                        await this.backscratcher(new URL(url), e);
                        return attempter();
                    }
                };
            }
            else {
                const scopeulator = this.scopeulate();
                try {
                    await scopeulator.click();
                    await scopeulator.clickSortOptionByText();
                    await scopeulator.clickTimeRangeByText();
                }
                catch (e) {
                    Logger.warn("Error in findo setup", e);
                }
                const findulator = await scopeulator.findulator();
                await this.scrollabit();
                let idx = 0;
                const threads = [[]];
                const count = await findulator.find.locator.count();
                for (let i = 0; i < count; i++) {
                    const thread = findulator.find.locator.nth(i);
                    const { id, content, screenshot } = await this.raw(thread, false);
                    if (threads[idx].length >= 10)
                        threads[++idx] = [];
                    threads[idx].push({
                        id,
                        content,
                        listing: thread,
                        attributes: await this.attributes(thread),
                    });
                }
                const rank = async () => {
                    for (const batch of threads) {
                        const promptmise = promptee.robot({
                            model: "o4-mini",
                            task: "reddit_thread_ranking",
                            decorators: this.opts.ai.decorators,
                            generations: {
                                type: "ranking",
                                range: { min: 1, max: 1 },
                                input: {
                                    data: batch.map((t) => ({
                                        id: t.id,
                                        attributes: t.attributes,
                                        content: t.content,
                                    })),
                                    user_intent: `Rank all of these threads ${this.opts.settings.start.feature} on from ${this.page.url()}`,
                                },
                            },
                        });
                        const wait = async (count = 0) => {
                            try {
                                while (count++ < 10) {
                                    await this.scrollabit();
                                    const racer = await Promise.race([promptmise, delay(100)]);
                                    if (typeof racer === "number")
                                        continue;
                                    return racer[0].data
                                        .sort((a) => a.rank)
                                        .map((item) => {
                                        const thread = batch.find((t) => t.id === item.id);
                                        return thread?.listing;
                                    });
                                }
                            }
                            catch (error) {
                                Logger.warn("Error in ranking wait", error);
                            }
                            return batch.map((t) => t.listing);
                        };
                        try {
                            return await this.findo(await wait(), async (thread) => {
                                await pre();
                                return await setup.funco(url, thread);
                            });
                        }
                        catch (error) {
                            Logger.warn("Error in findo after ranking", batch, error);
                        }
                    }
                };
                return await rank();
            }
            Logger.log("Scenario function completed", scopeulation, url);
        });
    }
    scopeulate() {
        const scoped = scopeulation.scoped(this.opts.args.scope);
        const scopeulated = {
            ...scoped,
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
                const scope = mapper[scoped.scope];
                return { scope, find: await this.find(scope.ids, scope.strat) };
            },
            clickSortOptionByText: async () => {
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
            },
            clickTimeRangeByText: async () => {
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
            },
            click: async () => {
                if (scoped.type)
                    return;
                const tab = scoped.scope === "Posts"
                    ? this.page.getByRole("button", { name: scoped.scope })
                    : this.page.locator(`#search-results-page-tab-${scoped.scope.toLowerCase()}`);
                await this.click(tab);
            },
        };
        return bang(`scopeulate`, scopeulated, scoped);
    }
    async raw(locator = this.page.locator("shreddit-post").first(), screenshots = true) {
        await locator.waitFor();
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
        return { id: crypto.randomUUID(), url: this.page.url(), content, screenshot };
    }
    async backscratcher(url, error) {
        bang("backscratcher checking listing attempts", !error || this.opts.settings.start.attempts-- > 0, {
            attempts: this.opts.settings.start.attempts,
            error,
        });
        while (await this.page.evaluate(() => window.history.length > 1)) {
            if (new URL(this.page.url()).pathname === url.pathname)
                break;
            await this.page.goBack();
            await this.nap();
        }
    }
    async onWhile(url) {
        const basic = scopeulation.subreddit(url) || url === BASE_URL;
        const todo = this.opts.settings.start.urls.length + this.opts.args.search.length;
        const visit = this.opts.settings.start.urls.length - scopeulation.visited.length;
        const search = this.opts.args.search.length - scopeulation.searched.length;
        const searched = search === 0 && this.opts.args.search.length > 0;
        const done = scopeulation.visited.length + scopeulation.searched.length;
        const stats = { todo, done, visit, search, searched, basic };
        Logger.log(`Status`, stats, scopeulation);
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
        const text = this.opts.args.search[scopeulation.searched.length];
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
        await this.click('a:has-text("See full discussion")', { timeout: 600 }).catch(() => false);
        await this.scrollabit(3);
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
        if (!scopeulator.community && !scopeulator.people)
            return;
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
