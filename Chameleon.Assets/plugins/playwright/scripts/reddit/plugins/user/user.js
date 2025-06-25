export class User {
    pager;
    constructor(pager) {
        this.pager = pager;
    }
    async follow() {
        await this.pager.click('div[slot="button-follow"] button:has-text("Follow")');
    }
}
