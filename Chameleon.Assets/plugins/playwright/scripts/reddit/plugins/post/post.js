export class Post {
    pager;
    constructor(pager) {
        this.pager = pager;
    }
    async title() {
        return this.pager.txtContent('h1[id^="post-title-"][slot="title"]');
    }
    async raw() {
        const locator = this.pager.page.locator("#i18n-shreddit-post-translator-content >> shreddit-post");
        await locator.waitFor();
        const screenshot = await this.pager.screenshot(locator);
        const content = await locator.evaluate((root) => {
            const relevantAttrPrefixes = [
                "post-",
                "subreddit-",
                "author-",
                "content-",
                "comment-",
                "domain",
                "id",
                "title",
                "href",
                "src",
                "datetime",
            ];
            const extractAttributes = (el) => {
                const data = {};
                for (const { name, value } of el.attributes) {
                    if (relevantAttrPrefixes.some((prefix) => name.startsWith(prefix) || prefix === name)) {
                        data[name] = value;
                    }
                }
                return data;
            };
            const extractTextContent = (node) => {
                if (node.nodeType === Node.TEXT_NODE) {
                    return node.textContent?.trim() || "";
                }
                if (node.nodeType === Node.ELEMENT_NODE) {
                    return Array.from(node.childNodes)
                        .map(extractTextContent)
                        .filter(Boolean)
                        .join(" ")
                        .replace(/\s+/g, " ")
                        .trim();
                }
                return "";
            };
            const extractMedia = (el) => Array.from(el.querySelectorAll("img, video"))
                .map((node) => ({ type: node.tagName.toLowerCase(), src: node.getAttribute("src") }))
                .filter((item) => item.src);
            return {
                tag: "shreddit-post",
                attributes: extractAttributes(root),
                title: root.querySelector("h1")?.textContent?.trim() || null,
                flair: root.querySelector("shreddit-post-flair")?.textContent?.trim() || null,
                body: extractTextContent(root.querySelector('[slot="text-body"]') || root),
                media: extractMedia(root),
            };
        });
        const comments = await this.pager.getComments();
        return { id: crypto.randomUUID(), url: this.pager.page.url(), content, screenshot, comments };
    }
    async archived(func) {
        await this.pager.nap();
        return await this.pager.joinConversation();
    }
    async addComment(comment) {
        await this.pager.pressSequentially(this.pager.page.locator("#subgrid-container").getByRole("textbox"), await comment());
        await this.pager.click(this.pager.page.locator('button.button-primary[slot="submit-button"]'));
    }
    async replyToComment(locator, reply) {
        await locator.scrollIntoViewIfNeeded();
        await this.pager.nap();
        const comment = locator.locator("shreddit-comment-action-row button").first();
        await this.pager.click(comment);
        const replyBox = locator.locator("shreddit-comment-action-row shreddit-async-loader comment-composer-host faceplate-form shreddit-composer");
        await replyBox.waitFor();
        await this.pager.type(await reply());
        await this.pager.click(replyBox.locator("button[slot='submit-button']").first());
    }
}
