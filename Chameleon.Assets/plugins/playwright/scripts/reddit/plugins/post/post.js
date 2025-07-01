import Reddito from "../../reddit.js";
export class Post {
    actor;
    constructor(actor) {
        this.actor = actor;
    }
    async title() {
        return this.actor.txtContent('h1[id^="post-title-"][slot="title"]');
    }
    async addComment(comment) {
        await this.actor.pressSequentially(this.actor.page.locator("#subgrid-container").getByRole("textbox"), await comment());
        await this.actor.click(this.actor.page.locator('button.button-primary[slot="submit-button"]'));
    }
    async replyToComment(locator, reply) {
        await locator.scrollIntoViewIfNeeded();
        await this.actor.nap();
        const comment = locator.locator('button:has-text("Reply")');
        await this.actor.click(comment);
        const replyBox = locator.locator("shreddit-comment-action-row shreddit-async-loader comment-composer-host faceplate-form shreddit-composer");
        await replyBox.waitFor();
        await this.actor.type(await reply());
        await this.actor.click(replyBox.locator("button[slot='submit-button']"));
    }
}
export default async function (params, action) {
    const { reddit } = await Reddito(params, action);
    const post = new Post(reddit);
    return { reddit, post };
}
