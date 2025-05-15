export class User {
    reddit;
    constructor(reddit) {
        this.reddit = reddit;
    }
    async follow() {
        await this.reddit.click(this.reddit.bang("'Follow' button", this.reddit.page.locator("div[slot='button-follow']").first()));
    }
}
