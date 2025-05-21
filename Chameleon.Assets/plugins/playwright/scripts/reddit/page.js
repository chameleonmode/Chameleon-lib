import { random, rando, trySequentially } from "../../lib/utils.js";
import { configure, BASE_URL } from "./reddit.js";
import { promptee } from "../../lib/requests.js";
import { Player } from "../player.js";
import { Base } from "../base.js";
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
            if (action && this.scopeulation.comments(url)) {
                for (let i = 0; i < this.opts.settings.start.attempts; i++) {
                    try {
                        this.iterations = 1;
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
                    const expecto = await this.findo(async () => await action(), this.player.visited);
                    return expecto.index;
                }
                catch (e) {
                    Logger.warn("Error in action function:", e);
                }
                finally {
                    const text = this.opts.args.search[this.searched.length];
                    this.searched.push(text);
                    Logger.log("Action function completed");
                }
            }
            else {
                Logger.warn("No action provided");
            }
            return undefined;
        });
        this.ctx = ctx;
        this.opts = opts;
        this.action = action;
    }
    status() {
        const done = this.visited.length + this.searched.length;
        const todo = this.opts.settings.start.urls.length + this.opts.args.search.length;
        const visit = this.opts.settings.start.urls.length - this.visited.length;
        const search = this.opts.args.search.length - this.searched.length;
        const searched = search === 0 && this.opts.args.search.length > 0;
        Logger.log(`Todo: ${todo} of ${done} completed`);
        Logger.log(`Search: ${this.opts.args.search.length} of ${this.searched.length} completed`);
        Logger.log(`Visit: ${this.opts.settings.start.urls.length} of ${this.visited.length} completed`);
        return { todo, done, visit, search, searched };
    }
    async onTry(url) {
        const { todo, done, visit, search, searched } = this.status();
        const basic = this.scopeulation.community(url) || url === BASE_URL;
        if (searched && !this.visited.includes(url) && basic) {
            this.searched.length = 0;
            return await this.onTry(url);
        }
        return search > 0 && basic
            ? await this.searcho()
            : visit > 0 && !this.visited.includes(url)
                ? await this.navigato(url)
                : this.error(`All terms completed.`);
    }
    async onIteration(url) {
        await this.nap();
        const started = this.scopeulation.comments(url) || this.scopeulation.search(url)
            ? url
            : url.endsWith("/")
                ? url + "search"
                : url + "/search";
        while (!this.page.url().startsWith(started)) {
            await this.page.goBack({ waitUntil: "load" });
            await this.nap({
                ...this.timeouts.naps,
                multiplier: random(3, 6),
            });
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
        try {
            const clearButton = locator.getByRole("button", { name: "Clear search" });
            await this.click(clearButton, random(3000, 9000));
        }
        catch (e) {
            Logger.warn("Error clicking clear button:", e);
        }
        await this.pressSequentially(textbox, text, false);
        await this.nap({
            ...this.timeouts.naps,
            multiplier: 3,
        });
        await textbox.press("Enter");
        await this.nap();
    }
    async findo(funco, visited = []) {
        const scopeulator = this.scopeulation.tranform();
        const findulator = (() => {
            const mapper = {
                Posts: { ids: ["search-post-with-content-preview", "search-post-unit"], strat: "testId" },
                Comments: { ids: ["search-sdui-comment-unit"], strat: "testId" },
                Media: { ids: ["div[data-id='search-media-post-unit']"], strat: "selector" },
                People: { ids: ["search-author"], strat: "testId" },
                Communities: { ids: ["search-community"], strat: "testId" },
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
            const { count, locator, id } = await this.find(findulator.ids, findulator.strat);
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
                    Logger.warn("Max attempts reached, restarting next iteration.");
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
        throw this.error(`Failed to find a thread with open comments after ${this.opts.settings.start.attempts} attempts.`);
    }
    async actionado() {
        const compleations = [];
        const acto = rando() && ["comment", "reply"].includes(this.opts.settings.start.feature);
        this.bang("acto?", acto);
        const actionable = this.opts.args.artifacters.find((art) => art.type === "selections" && art.data.find((d) => ["join", "vote"].includes(d)))?.data;
        this.bang("Actionable", actionable.length > 0, { actionable });
        if (!actionable.includes("vote"))
            actionable.push("vote");
        const actions = {
            join: async () => {
                await this.subreddit.joiner();
            },
            vote: async () => {
                await this.subreddit.voter(false);
            },
        };
        for (const selection of actionable) {
            try {
                if (!compleations.includes("join"))
                    this.bang("action", rando(), { selection });
                await actions[selection]();
                compleations.push(selection);
            }
            catch (error) {
                Logger.warn(`Error performing action "${selection}":`, error);
            }
        }
        this.bang("Actionable completions", compleations.length, { compleations });
        return compleations.length;
    }
    scopeulation = {
        community(url) {
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
        tranform: () => {
            const scopes = ["People", "Communities"];
            const url = this.visited[this.visited.length - 1];
            const Url = new URL(url);
            const type = Url.searchParams.get("type");
            const scope = scopes.includes(this.opts.args.scope) &&
                (this.scopeulation.community(url) ||
                    this.scopeulation.comments(url) ||
                    this.scopeulation.search(url))
                ? "Posts"
                : this.opts.args.scope;
            return {
                scope,
                url,
                Url,
                type,
                sort: Url.searchParams.get("sort"),
                t: Url.searchParams.get("t"),
                community: scope === "Communities" || type === "communities",
            };
        },
    };
    login = {
        checkLoginAuthentication: async () => {
            const locato = this.page.locator("#login-button").first();
            this.bang("Login button", await locato.isVisible(), locato);
            await this.click(locato);
        },
        loginWithCredentials: async (email, password) => {
            await this.login.checkLoginAuthentication();
            const loginUserNameInput = this.page.locator("faceplate-text-input#login-username input");
            await this.pressSequentially(loginUserNameInput, email);
            await this.page.keyboard.press("Tab");
            const loginUserPassword = this.page.locator("faceplate-text-input#login-password input");
            await this.pressSequentially(loginUserPassword, password);
            const loginUserButton = this.page.getByRole("button", { name: "Log In" });
            await this.click(loginUserButton);
        },
        loginWithGoogle: async (email, password) => {
            await this.login.checkLoginAuthentication();
            const { frame } = await this.findFrame([
                'iframe[src*="accounts.google.com/gsi/button"]',
                'iframe[allow="identity-credentials-get"]',
                'iframe[id^="gsi_"]',
                'iframe[title="Sign in with Google Button"]',
                'iframe[title*="Google"]',
            ]);
            await frame.locator('div[role="button"]').click();
            const popup = await this.page.waitForEvent("popup");
            await popup.waitForLoadState();
            const emailButtons = popup.locator("[data-email]");
            if ((await emailButtons.count()) > 0) {
                return await emailButtons.first().click();
            }
            const emailInput = popup.getByLabel("Email or phone");
            await this.pressSequentially(emailInput, email);
            const nextButton = popup.locator("div#identifierNext button");
            await this.click(nextButton);
            const passwordInput = popup.getByLabel("Enter your password");
            await this.pressSequentially(passwordInput, password);
            const passwordNextButton = popup.locator("div#passwordNext button");
            await this.click(passwordNextButton);
        },
    };
    post = {
        title: () => this.txtContent('h1[id^="post-title-"][slot="title"]'),
        joinConversation: async () => {
            try {
                const seeFullDiscussionLink = this.page.locator('a:has-text("See full discussion")');
                if ((await seeFullDiscussionLink.count()) > 0)
                    await this.click(seeFullDiscussionLink.first());
            }
            catch (error) {
                Logger.warn("Error clicking 'See full discussion' link:", error);
            }
            await this.scrollabit();
            const { count, locator, id } = await this.find([
                'comment-composer-host slot[name="ready"] faceplate-textarea-input[data-testid="trigger-button"]',
                'comment-composer-host[slot="ready"] faceplate-tracker faceplate-textarea-input[data-testid="trigger-button"]',
            ], "selector");
            return locator.first();
        },
        assert: async () => {
            const scopeulator = this.scopeulation.tranform();
            if (scopeulator.scope === "Communities" || scopeulator.type === "communities") {
                await this.scrollabit();
                const posts = this.page.locator("a[slot='title']");
                const count = await posts.count();
                const index = random(0, count);
                const randomPost = posts.nth(index);
                await this.click(randomPost);
            }
        },
        act: async (inside, acted) => {
            try {
                this.bang("act " + inside, acted === undefined);
                return await this.actionado();
            }
            catch (e) {
                Logger.warn("Error in act function:", e);
            }
        },
        getComments: async (max) => {
            await this.scrollabit();
            const locator = this.page.locator("shreddit-comment");
            const count = await locator.count();
            const length = max ? Math.min(max, count) : count;
            const comments = [];
            for (let i = 0; i < length; i++) {
                comments.push(await this.txtContent("div[slot='comment']", locator.nth(i)));
            }
            return comments;
        },
        getComment: async (nth = -1) => {
            await this.scrollabit();
            const locator = this.page.locator("shreddit-comment");
            const count = await locator.count();
            const index = nth < 0 ? random(0, count - 1) : nth;
            const comment = this.bang("Comment", locator.nth(index));
            await comment.waitFor({ timeout: this.timeouts.wait });
            return {
                text: await this.txtContent("div[slot='comment']", comment),
                locator: comment,
            };
        },
        addComment: async (comment) => {
            const inside = "addComment";
            const acted = await this.post.act(inside);
            const result = await trySequentially([
                async () => await this.click(await this.post.joinConversation()),
                async () => await this.click(this.page.getByRole("button", { name: "Add a comment" })),
                async () => {
                    const triggers = this.page.getByTestId("trigger-button");
                    const count = await triggers.count();
                    for (let i = count - 1; i >= 0; i--) {
                        const trigger = triggers.nth(i);
                        try {
                            await this.click(trigger);
                            break;
                        }
                        catch (error) {
                            Logger.error(`Error clicking trigger button ${i}:`, error);
                        }
                    }
                },
            ]);
            this.bang("Comment button", result);
            await this.pressSequentially(this.page.locator("#subgrid-container").getByRole("textbox"), await comment());
            await this.click(this.page.locator('button.button-primary[slot="submit-button"]'));
            await this.post.act(inside, acted);
        },
        replyToComment: async (locator, reply) => {
            const inside = "replyToComment";
            const acted = await this.post.act(inside);
            await locator.scrollIntoViewIfNeeded();
            await this.nap();
            const comment = locator.locator("shreddit-comment-action-row button").first();
            await this.click(comment);
            const replyBox = locator.locator("shreddit-comment-action-row shreddit-async-loader comment-composer-host faceplate-form shreddit-composer");
            await replyBox.waitFor();
            await this.type(await reply());
            await this.click(replyBox.locator("button[slot='submit-button']").first());
            await this.post.act(inside, acted);
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
        voter: async (pre = true) => {
            await this.scrollabit();
            if (pre) {
                const scopeulator = this.scopeulation.tranform();
                if (!scopeulator.community) {
                    const banger = await this.post.joinConversation();
                    this.bang("vote", banger);
                }
            }
            const ups = this.page.getByRole("button", { name: "Upvote" });
            const downs = this.page.getByRole("button", { name: "Downvote" });
            const [upCount, downCount] = await Promise.all([ups.count(), downs.count()]);
            const count = Math.min(upCount, downCount) - 1;
            const length = Math.min(count, rando(this.opts.settings.start.rando.min, this.opts.settings.start.rando.max));
            this.bang("Vote count", length, { upCount, downCount, count, length });
            for (let i = 0; i < length; i++) {
                const index = random(0, count);
                await (rando() ? this.click(ups.nth(index)) : this.click(downs.nth(index)));
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
    const genorate = options.settings.start.all && options.args.search.length && options.settings.start.variations.max > 0;
    if (genorate) {
        const result = await promptee.genorate({
            model: options.ai.model,
            decorators: options.ai.decorators,
            task: `generate search terms`,
            generations: {
                type: "term",
                sys: "you are creating variations of search terms",
                context: "current search terms",
                range: options.settings.start.variations,
                input: {
                    type: "search",
                    data: options.args.search,
                    reason: "list of search terms to generate variations for",
                },
            },
        });
        const terms = result.map((i) => i.data);
        options.args.search = [...options.args.search, ...terms].sort(() => Math.random() - 0.5);
        Logger.info("Generated search terms:", options.args.search, JSON.stringify(result));
    }
    Logger.info("Feature:", {
        feature: options.settings.start.feature,
        artifacts: JSON.stringify(options.args.artifacters),
    });
    Logger.info("Options:", {
        options: JSON.stringify(options),
    });
    return { reddit };
}
