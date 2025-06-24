export class User {
    reddit;
    constructor(reddit) {
        this.reddit = reddit;
    }
    async follow() {
        await this.reddit.click('div[slot="button-follow"] button:has-text("Follow")');
    }
}
