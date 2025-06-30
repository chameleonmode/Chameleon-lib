import Reddito from "../../reddit.js";
export class Post {
    actor;
    constructor(actor) {
        this.actor = actor;
    }
    async title() {
        return this.actor.txtContent('h1[id^="post-title-"][slot="title"]');
    }
    async raw() {
        const locator = this.actor.page.locator("shreddit-post").first();
        await locator.waitFor();
        const screenshot = await this.actor.screenshot(locator);
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
        return { id: crypto.randomUUID(), url: this.actor.page.url(), content, screenshot };
    }
    async addComment(comment) {
        await this.actor.pressSequentially(this.actor.page.locator("#subgrid-container").getByRole("textbox"), await comment());
        await this.actor.click(this.actor.page.locator('button.button-primary[slot="submit-button"]'));
    }
    async replyToComment(locator, reply) {
        await locator.scrollIntoViewIfNeeded();
        await this.actor.nap();
        const comment = locator.locator('button:has-text("Reply")').first();
        await this.actor.click(comment);
        const replyBox = locator.locator("shreddit-comment-action-row shreddit-async-loader comment-composer-host faceplate-form shreddit-composer");
        await replyBox.waitFor();
        await this.actor.type(await reply());
        await this.actor.click(replyBox.locator("button[slot='submit-button']").first());
    }
}
export default async function (params, action) {
    const { reddit } = await Reddito(params, action);
    const post = new Post(reddit);
    return { reddit, post };
}
