import Reddito from "../../reddit.js";
export class Subreddit {
    reddit;
    constructor(reddit) {
        this.reddit = reddit;
    }
    async canPost() {
        await this.reddit.nap();
        await this.reddit.click(this.reddit.page.locator("#subgrid-container faceplate-tracker[noun=create_post]").first());
    }
    async visitCommunity() {
        const locator = this.reddit.page.locator('span.avatar a[href^="/r/"]');
        await this.reddit.click(this.reddit.bang("'visit' button", locator.first(), { locator }));
    }
    async voter() {
        const scopeulator = this.reddit.scopeulate();
        if (!scopeulator.community && !scopeulator.people) {
            const banger = await this.reddit.joinConversation();
            this.reddit.bang("vote", banger, { scopeulator });
        }
        await this.reddit.scrollabit();
        const ups = this.reddit.page.getByRole("button", { name: "Upvote" });
        const downs = this.reddit.page.getByRole("button", { name: "Downvote" });
        const upCount = await ups.count();
        const downCount = await downs.count();
        const count = Math.min(upCount, downCount) - 1;
        const length = Math.min(count, this.reddit.opts.settings.start.rando.min);
        this.reddit.bang("Vote count", length > 0, { upCount, downCount, count, length });
        for (let i = 0; i < length; i++) {
            await this.reddit.click(Math.random() * 100 <= 95 ? ups.nth(i) : downs.nth(i));
        }
        return {
            ups: { locator: ups, count: upCount },
            downs: { locator: downs, count: downCount },
        };
    }
    async joiner() {
        await this.reddit.scrollabit();
        const locator = this.reddit.page.getByRole("button", { name: "Join", exact: true }).first();
        await this.reddit.click(this.reddit.bang("'Join' button", locator.first(), { locator }));
    }
    async poster(contents) {
        await this.reddit.nap();
        const titleLocator = this.reddit.page.locator("#innerTextArea").first();
        const bodyLocator = this.reddit.page.locator('div[slot="rte"][aria-label="Post body text field"]');
        const postTypeValue = await this.reddit.page.locator('r-post-type-select[name="type"]').getAttribute("value");
        this.reddit.bang("Post type", postTypeValue === "TEXT", { postTypeValue });
        this.reddit.bang("Post body text field", await bodyLocator.innerText(), { bodyLocator });
        this.reddit.bang("Post title text field", await titleLocator.count(), { titleLocator });
        const { title, content } = await contents();
        await this.reddit.pressSequentially(titleLocator, title);
        await this.reddit.pressSequentially(bodyLocator, content);
        const submitButton = this.reddit.page
            .locator("r-post-form-submit-button#submit-post-button")
            .getByRole("button");
        await this.reddit.click(submitButton);
    }
}
export default async function (params, action) {
    const { reddit } = await Reddito(params, action);
    const subreddit = new Subreddit(reddit);
    return { reddit, subreddit };
}
