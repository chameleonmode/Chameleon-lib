import Reddito from "../../reddit.js";
export class Post {
    reddit;
    constructor(reddit) {
        this.reddit = reddit;
    }
    async title() {
        return this.reddit.txtContent('h1[id^="post-title-"][slot="title"]');
    }
    async addComment(comment) {
        await this.reddit.pressSequentially(this.reddit.page.locator("#subgrid-container").getByRole("textbox"), await comment());
        await this.reddit.click(this.reddit.page.locator('button.button-primary[slot="submit-button"]'));
    }
    async replyToComment(locator, reply) {
        await locator.scrollIntoViewIfNeeded();
        await this.reddit.nap();
        const comment = locator.locator('button:has-text("Reply")');
        await this.reddit.click(comment);
        const replyBox = locator.locator("shreddit-comment-action-row shreddit-async-loader comment-composer-host faceplate-form shreddit-composer");
        await replyBox.waitFor();
        await this.reddit.type(await reply());
        await this.reddit.click(replyBox.locator("button[slot='submit-button']"));
    }
}
export default async function (opts, action) {
    const { reddit } = await Reddito(opts, action);
    const post = new Post(reddit);
    return { reddit, post };
}
