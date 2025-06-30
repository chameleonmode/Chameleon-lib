import Reddito from "../../reddit.js";
export class User {
    reddit;
    constructor(reddit) {
        this.reddit = reddit;
    }
    async follow() {
        await this.reddit.click('div[slot="button-follow"] button:has-text("Follow")');
    }
}
export default async function (params, action) {
    const { reddit } = await Reddito(params, action);
    const user = new User(reddit);
    return { reddit, user };
}
