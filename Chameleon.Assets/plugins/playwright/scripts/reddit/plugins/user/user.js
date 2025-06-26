import Reddito from "../../reddit.js";
export class User {
    pager;
    constructor(pager) {
        this.pager = pager;
    }
    async follow() {
        await this.pager.click('div[slot="button-follow"] button:has-text("Follow")');
    }
}
export default async function (params, action) {
    const { reddit } = await Reddito(params, action);
    const user = new User(reddit);
    return { reddit, user };
}
