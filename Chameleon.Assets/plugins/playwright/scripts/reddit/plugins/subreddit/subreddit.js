import Reddito from "../../reddit.js";
export class Subreddit {
    pager;
    constructor(pager) {
        this.pager = pager;
    }
    async canPost() {
        await this.pager.nap();
        await this.pager.click(this.pager.page.locator("#subgrid-container faceplate-tracker[noun=create_post]").first());
    }
    async visitCommunity() {
        await this.pager.click(this.pager.bang("'visit' button", this.pager.page.locator('span.avatar a[href^="/r/"]').first()));
    }
    async voter() {
        const scopeulator = this.pager.scopeulate();
        if (!scopeulator.community && !scopeulator.people) {
            const banger = await this.pager.joinConversation();
            this.pager.bang("vote", banger);
        }
        await this.pager.scrollabit();
        const ups = this.pager.page.getByRole("button", { name: "Upvote" });
        const downs = this.pager.page.getByRole("button", { name: "Downvote" });
        const upCount = await ups.count();
        const downCount = await downs.count();
        const count = Math.min(upCount, downCount) - 1;
        const length = Math.min(count, this.pager.opts.settings.start.rando.min);
        this.pager.bang("Vote count", length > 0, { upCount, downCount, count, length });
        for (let i = 0; i < length; i++) {
            await this.pager.click(Math.random() * 100 <= 95 ? ups.nth(i) : downs.nth(i));
        }
        return {
            ups: { locator: ups, count: upCount },
            downs: { locator: downs, count: downCount },
        };
    }
    async joiner() {
        await this.pager.scrollabit();
        await this.pager.click(this.pager.bang("'Join' button", this.pager.page.getByRole("button", { name: "Join", exact: true }).first()));
    }
    async poster(contents) {
        await this.pager.nap();
        const titleLocator = this.pager.page.locator("#innerTextArea").first();
        const bodyLocator = this.pager.page.locator('div[slot="rte"][aria-label="Post body text field"]');
        const postTypeValue = await this.pager.page.locator('r-post-type-select[name="type"]').getAttribute("value");
        this.pager.bang("Post type", postTypeValue === "TEXT");
        this.pager.bang("Post body text field", await bodyLocator.innerText());
        this.pager.bang("Post title text field", await titleLocator.count());
        const { title, content } = await contents();
        await this.pager.pressSequentially(titleLocator, title);
        await this.pager.pressSequentially(bodyLocator, content);
        const submitButton = this.pager.page
            .locator("r-post-form-submit-button#submit-post-button")
            .getByRole("button");
        await this.pager.click(submitButton);
    }
}
export default async function (params, action) {
    const { reddit } = await Reddito(params, action);
    const subreddit = new Subreddit(reddit);
    return { reddit, subreddit };
}
