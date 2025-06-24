import { random, rando, trySequentially, error } from "../../lib/utils.js";
import { configure, BASE_URL } from "./configure.js";
import { Base } from "../base.js";
import { Player } from "../player.js";
import { Logger } from "../../lib/logger.js";
export class Reddit extends Base {
    ctx;
    opts;
    action;
    player = new Player(this);
    searched = [];
    constructor(ctx, opts, action) {
        super(ctx, opts, async (url) => {
            Logger.log("Scenario URL:", url);
            const pre = async () => {
                if (this.scopeulation.user(url)) {
                    await this.click(this.opts.args.sort === "Posts"
                        ? 'a[slot="page-2"]:has-text("Posts")'
                        : this.opts.args.sort === "Comments"
                            ? 'a[slot="page-3"]:has-text("Comments")'
                            : 'a[slot="page-1"]:has-text("Overview")');
                }
            };
            if (action && (this.scopeulation.comments(url) || this.scopeulation.user(url))) {
                for (let i = 0; i < this.opts.settings.start.attempts; i++) {
                    try {
                        this.opts.settings.start.iterations = { min: 1, max: 1 };
                        await pre();
                        return await action(url);
                    }
                    catch (e) {
                        Logger.warn("Error in action function:", e);
                        await this.page.reload({ waitUntil: "load" });
                    }
                    finally {
                        Logger.log("Action function completed");
                    }
                }
            }
            else if (action) {
                try {
                    const expecto = await this.findo(async () => {
                        await pre();
                        return await action();
                    });
                    return expecto.index;
                }
                catch (e) {
                    Logger.warn("Error in action function:", e);
                }
                finally {
                    const text = this.opts.args.search[this.searched.length];
                    this.searched.push(text);
                    Logger.log("Action function completed", text, this.searched);
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
    status() {
        const todo = this.opts.settings.start.urls.length + this.opts.args.search.length;
        const visit = this.opts.settings.start.urls.length - this.visited.length;
        const search = this.opts.args.search.length - this.searched.length;
        const searched = search === 0 && this.opts.args.search.length > 0;
        const done = this.visited.length + this.searched.length;
        const stats = { todo, done, visit, search, searched };
        Logger.log(`Status:`, stats);
        return stats;
    }
    async onTry(url) {
        const { visit, search, searched } = this.status();
        const basic = this.scopeulation.subreddit(url) || url === BASE_URL;
        if (searched && this.scopeulation.subreddit(url) && !this.visited.includes(url)) {
            this.searched.length = 0;
            return await this.onTry(url);
        }
        return search > 0 && basic
            ? await this.searcho()
            : visit > 0 && !this.visited.includes(url)
                ? await this.navigato(url)
                : error(`All terms completed.`);
    }
    async onIteration(url) {
        await this.nap();
        const started = this.scopeulation.comments(url) || this.scopeulation.search(url)
            ? url
            : url.replace(/\/?$/, "/") + "search";
        while (!this.page.url().startsWith(started)) {
            await this.page.goBack({ waitUntil: "load" });
            await this.nap({ multiplier: random(3, 6) });
        }
    }
    async navigato(url) {
        await this.navigate(url);
        this.visited.push(url);
    }
    async searcho() {
        const url = this.opts.settings.start.urls[this.visited.length];
        const navigate = this.searched.length === 0 && !this.visited.includes(url);
        if (navigate)
            await this.navigato(url);
        else
            await this.onIteration(this.visited[this.visited.length - 1]);
        const text = this.opts.args.search[this.searched.length];
        const locator = this.page.locator(`faceplate-search-input`);
        const textbox = locator.getByRole("textbox");
        await this.click(textbox);
        const clearButton = locator.getByRole("button", { name: "Clear search" });
        await this.click(clearButton, { timeout: 1500, strict: false }).catch(() => false);
        await this.pressSequentially(textbox, text, false);
        await this.nap({ multiplier: 3 });
        await textbox.press("Enter");
        await this.nap();
    }
    async findo(funco, visited = this.player.state.visited) {
        const scopeulator = this.scopeulation.tranform();
        const findulator = (() => {
            const mapper = {
                Posts: { ids: ["search-post-unit", "search-sdui-unit", "search-post-with-content-preview",], strat: "testId" },
                Media: { ids: ["div[data-id='search-media-post-unit']"], strat: "selector" },
                Comments: { ids: ["search-sdui-comment-unit"], strat: "testId" },
                Communities: { ids: ["search-community"], strat: "testId" },
                People: { ids: ["search-author"], strat: "testId" },
            };
            return mapper[scopeulator.scope];
        })();
        try {
            if (!scopeulator.type) {
                await this.click(scopeulator.scope === "Posts"
                    ? this.page.getByRole("button", { name: scopeulator.scope }).first()
                    : this.page.locator(`#search-results-page-tab-${scopeulator.scope.toLowerCase()}`).first());
            }
            if (visited.length === 0) {
                const clickSortOptionByText = async () => {
                    const scopes = ["Posts", "Comments", "Media"];
                    const sorts = ["Hot", "Top", "New", "Comments"];
                    if (scopeulator.sort ||
                        !scopes.includes(scopeulator.scope) ||
                        !sorts.includes(this.opts.args.sort)) {
                        return;
                    }
                    const sortLocator = this.page.locator(`search-sort-dropdown-menu`).first();
                    await this.click(sortLocator);
                    const normalizedText = this.opts.args.scope === "Comments" &&
                        (this.opts.args.sort === "Comments" || this.opts.args.sort === "Hot")
                        ? "Top"
                        : this.opts.args.sort.trim();
                    const normalizedOption = normalizedText === "Comments" ? "Comment count" : normalizedText;
                    try {
                        const sortOption = this.page.locator(`li a span:has-text("${normalizedOption}")`).first();
                        await sortOption.scrollIntoViewIfNeeded();
                        const parentLink = sortOption.locator("xpath=./ancestor::a");
                        await this.click(parentLink);
                        Logger.log(`Successfully clicked on the "${normalizedOption}" sort option`);
                    }
                    catch (error) {
                        Logger.error(`Failed to click sort option "${normalizedOption}":`, error);
                        try {
                            await this.page.evaluate((text) => {
                                const elements = Array.from(document.querySelectorAll("li a span"));
                                const targetElement = elements.find((el) => el.textContent?.includes(text));
                                if (targetElement) {
                                    targetElement.closest("a")?.click();
                                    return true;
                                }
                                return false;
                            }, normalizedOption);
                            Logger.log(`Clicked on "${normalizedOption}" using evaluate method`);
                        }
                        catch (evalError) {
                            Logger.error(`Alternative method also failed:`, evalError);
                        }
                    }
                };
                await clickSortOptionByText();
                await this.nap();
                const clickTimeRangeByText = async () => {
                    const scopes = ["Posts", "Media"];
                    const sorts = ["Relevance", "Top", "Comments"];
                    const filters = ["Year", "Month", "Week", "Today", "Hour"];
                    if (scopeulator.t ||
                        scopeulator.sort === "communities" ||
                        !scopes.includes(scopeulator.scope) ||
                        !sorts.includes(this.opts.args.sort) ||
                        !filters.includes(this.opts.args.filter)) {
                        return;
                    }
                    const sortLocator = this.page.locator(`search-sort-dropdown-menu`);
                    await this.click(sortLocator.nth(1));
                    const optionText = this.opts.args.filter === "Today"
                        ? this.opts.args.filter.trim()
                        : "Past " + this.opts.args.filter.trim().toLowerCase();
                    try {
                        const exactOption = this.page.locator(`li a span:has-text("${optionText}")`).first();
                        const linkElement = exactOption.locator("xpath=./ancestor::a");
                        await linkElement.scrollIntoViewIfNeeded();
                        await this.click(linkElement);
                        Logger.log(`Clicked on "${optionText}" time range option`);
                        return true;
                    }
                    catch (error) {
                        Logger.error(`Failed to click time range "${optionText}":`, error);
                        try {
                            const listItems = this.page.locator("search-sort-dropdown-menu#search_modifier_time_range li");
                            const count = await listItems.count();
                            for (let i = 0; i < count; i++) {
                                const item = listItems.nth(i);
                                const text = await item.locator("span span.text-14").textContent();
                                if (text?.trim().includes(optionText)) {
                                    const link = item.locator("a");
                                    await link.scrollIntoViewIfNeeded();
                                    await this.page.waitForTimeout(200);
                                    await link.click();
                                    Logger.log(`Clicked on "${optionText}" time range option (alternative method)`);
                                    return true;
                                }
                            }
                            Logger.error(`Could not find time range option "${optionText}" among ${count} options`);
                            return false;
                        }
                        catch (alternativeError) {
                            Logger.error(`Alternative method also failed:`, alternativeError);
                            return false;
                        }
                    }
                };
                await clickTimeRangeByText();
            }
        }
        catch (e) {
            Logger.warn("Error in findo function:", e);
        }
        for (let i = 0; i < Math.max(this.opts.settings.start.attempts, 1); i++) {
            Logger.debug(`Attempts remaining: ${this.opts.settings.start.attempts}`, i);
            await this.nap();
            await this.scrollabit();
            const { locator, count } = await this.find(findulator.ids, findulator.strat);
            const availableIndices = Array.from({ length: count }, (_, i) => i).filter((index) => !visited.includes(index));
            this.bang("available threads", availableIndices.length > 0, {
                triedIndices: visited,
                availableIndices,
            });
            const index = availableIndices[Math.floor(Math.random() * availableIndices.length)];
            try {
                const thread = locator.nth(index);
                await this.click(thread);
                try {
                    this.bang("max attempts", this.opts.settings.start.attempts === 0, this.opts.settings.start.attempts);
                    return { index, visited };
                }
                catch (e) {
                    const funky = await funco();
                    return { index, funky, visited };
                }
            }
            catch (e) {
                Logger.warn("error in findo loop", e);
                visited.push(index);
                await this.page.reload({ waitUntil: "load" });
                await this.onIteration(this.visited[this.visited.length - 1]);
            }
        }
        throw error(`Failed to find a thread with open comments after ${this.opts.settings.start.attempts} attempts.`);
    }
    async joinConversation() {
        await this.click('a:has-text("See full discussion")', { timeout: 1500 }).catch(() => false);
        const { locator } = await this.find([
            'div[contenteditable="true"][data-lexical-editor="true"]',
            'shreddit-composer div[contenteditable="true"]',
        ], "selector");
        return locator;
    }
    scopeulation = {
        subreddit(url) {
            const pattern = /\/r\/[^/]+\/?$/;
            return pattern.test(url);
        },
        comments(url) {
            const pattern = /\/r\/[^/]+\/comments(?:\/.*)?$/;
            return pattern.test(url);
        },
        search(url) {
            const pattern = /\/r\/[^/]+\/search(?:\/.*)?$/;
            return pattern.test(url);
        },
        user(url) {
            const pattern = /\/user\/[^/]+\/?$/;
            return pattern.test(url);
        },
        tranform: () => {
            const scopes = ["People", "Communities"];
            const url = this.visited[this.visited.length - 1];
            const scope = scopes.includes(this.opts.args.scope) &&
                (this.scopeulation.subreddit(url) ||
                    this.scopeulation.comments(url) ||
                    this.scopeulation.search(url))
                ? "Posts"
                : this.opts.args.scope;
            const Url = new URL(url);
            const type = Url.searchParams.get("type");
            const sort = Url.searchParams.get("sort");
            const t = Url.searchParams.get("t");
            const community = scope === "Communities" || type === "communities";
            const people = scope === "People" || type === "people" || this.scopeulation.user(url);
            return { url, scope, Url, type, sort, t, community, people };
        },
    };
    post = {
        title: () => this.txtContent('h1[id^="post-title-"][slot="title"]'),
        raw: async () => {
            const locator = this.page.locator("#i18n-shreddit-post-translator-content >> shreddit-post");
            await locator.waitFor();
            const screenshot = (await locator.screenshot({ scale: "css", type: "jpeg", quality: 72 })).toString("base64");
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
                    tag: "shreddit-post",
                    attributes: extractAttributes(root),
                    title: root.querySelector("h1")?.textContent?.trim() || null,
                    flair: root.querySelector("shreddit-post-flair")?.textContent?.trim() || null,
                    body: extractTextContent(root.querySelector('[slot="text-body"]') || root),
                    media: extractMedia(root),
                };
            });
            const comments = await this.post.getComments();
            return { id: crypto.randomUUID(), url: this.page.url(), content, screenshot, comments };
        },
        assert: async () => {
            const scopeulator = this.scopeulation.tranform();
            if (scopeulator.community || scopeulator.people) {
                await this.scrollabit();
                const posts = this.page.locator("a[slot='title']");
                const count = await posts.count();
                const index = random(0, count);
                const randomPost = posts.nth(index);
                await this.click(randomPost);
            }
        },
        archived: async (func) => {
            await this.nap();
            const result = await trySequentially([
                async () => await func.call(this, await this.joinConversation()),
                async () => await func.call(this, this.page.getByRole("button", { name: "Add a comment" })),
            ]);
            this.bang("Archived or Comment button", result.fulfilled.length > 0, {
                fulfilled: result.fulfilled,
                rejected: result.errors,
            });
        },
        getComments: async (max = 36) => {
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
                    Logger.log(`Error processing comment ${i}:`, error);
                    continue;
                }
            }
            return comments;
        },
        addComment: async (comment) => {
            await this.pressSequentially(this.page.locator("#subgrid-container").getByRole("textbox"), await comment());
            await this.click(this.page.locator('button.button-primary[slot="submit-button"]'));
        },
        replyToComment: async (locator, reply) => {
            await locator.scrollIntoViewIfNeeded();
            await this.nap();
            const comment = locator.locator("shreddit-comment-action-row button").first();
            await this.click(comment);
            const replyBox = locator.locator("shreddit-comment-action-row shreddit-async-loader comment-composer-host faceplate-form shreddit-composer");
            await replyBox.waitFor();
            await this.type(await reply());
            await this.click(replyBox.locator("button[slot='submit-button']").first());
        },
        visitCommunity: async () => {
            await this.click(this.bang("'visit' button", this.page.locator('span.avatar a[href^="/r/"]').first()));
        },
    };
    subreddit = {
        canPost: async () => {
            await this.nap();
            await this.click(this.page.locator("#subgrid-container faceplate-tracker[noun=create_post]").first());
        },
        voter: async () => {
            const scopeulator = this.scopeulation.tranform();
            if (!scopeulator.community && !scopeulator.people) {
                const banger = await this.joinConversation();
                this.bang("vote", banger);
            }
            await this.scrollabit();
            const ups = this.page.getByRole("button", { name: "Upvote" });
            const downs = this.page.getByRole("button", { name: "Downvote" });
            const upCount = await ups.count();
            const downCount = await downs.count();
            const count = Math.min(upCount, downCount) - 1;
            const length = Math.min(count, rando(this.opts.settings.start.rando.min, this.opts.settings.start.rando.max));
            this.bang("Vote count", length, { upCount, downCount, count, length });
            for (let i = 0; i < length; i++) {
                await this.click(rando(100) <= 95 ? ups.nth(i) : downs.nth(i));
            }
            return {
                ups: { locator: ups, count: upCount },
                downs: { locator: downs, count: downCount },
            };
        },
        joiner: async () => {
            await this.scrollabit();
            await this.click(this.bang("'Join' button", this.page.getByRole("button", { name: "Join", exact: true }).first()));
        },
    };
    async poster(contents) {
        await this.nap();
        const titleLocator = this.page.locator("#innerTextArea").first();
        const bodyLocator = this.page.locator('div[slot="rte"][aria-label="Post body text field"]');
        const postTypeValue = await this.page.locator('r-post-type-select[name="type"]').getAttribute("value");
        this.bang("Post type", postTypeValue === "TEXT");
        this.bang("Post body text field", await bodyLocator.innerText());
        this.bang("Post title text field", await titleLocator.count());
        const { title, content } = await contents();
        await this.pressSequentially(titleLocator, title);
        await this.pressSequentially(bodyLocator, content);
        const submitButton = this.page
            .locator("r-post-form-submit-button#submit-post-button")
            .getByRole("button");
        await this.click(submitButton);
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
