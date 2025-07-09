import { Logger, bang, promptee, randy } from "../../../../lib/index.js";
import Reddito from "../../reddit.js";
export class Subreddit {
    reddit;
    constructor(reddit) {
        this.reddit = reddit;
    }
    async canPost() {
        await this.reddit.nap();
        const locator = this.reddit.page.locator("#subgrid-container faceplate-tracker[noun=create_post]");
        await this.reddit.click(locator);
    }
    async visitCommunity() {
        const locator = this.reddit.page.locator('span.avatar a[href^="/r/"]');
        await this.reddit.click(locator);
    }
    async voter() {
        const ups = [];
        if (await this.reddit.joinConversation()) {
            const these = await this.reddit.getComments();
            const min = Math.min(these.length, this.reddit.opts.settings.start.rando.min);
            bang("Vote count", min > 0);
            try {
                const data = these.sort(() => randy()).slice(0, min);
                const promptmise = promptee.ranking({
                    task: `rank these reddit comments for voting positively ${min} times on. your reply data needs to be a ordered array of the provided comment id and your ranking number.`,
                    generations: {
                        type: "ranking",
                        range: { min: 1, max: 1 },
                        input: {
                            data: data.sort(() => randy()),
                            user_intent: `This batch comments are @${this.reddit.page.url()}`,
                        },
                    },
                });
                const reply = await this.reddit.waitabit(promptmise);
                const ranked = reply[0].data
                    .map((item) => these.find((c) => c.id === item.id))
                    .filter((comment) => comment !== undefined);
                ups.push(...ranked.map((c) => c.locator.getByRole("button", { name: "Upvote" })));
            }
            catch (error) {
                Logger.warn("Error in ranking wait", error);
            }
        }
        else
            await this.reddit.scrollabit();
        if (ups.length === 0) {
            const locator = await this.reddit.page.getByRole("button", { name: "Upvote" }).all();
            ups.push(...locator);
        }
        const downs = await this.reddit.page.getByRole("button", { name: "Downvote" }).all();
        const count = Math.min(ups.length, downs.length) - 1;
        const length = Math.min(count, this.reddit.opts.settings.start.rando.min);
        bang("Vote count", length > 0, { upCount: ups.length, downCount: downs.length, count, length });
        for (let i = 0; i < length; i++) {
            await this.reddit.click(Math.random() * 69 <= 96 ? ups[i] : downs[i]);
        }
        return {
            ups: { locator: ups, count: ups.length },
            downs: { locator: downs, count: downs.length },
        };
    }
    async joiner() {
        await this.reddit.scrollabit();
        const locator = this.reddit.page.getByRole("button", { name: "Join", exact: true });
        await this.reddit.click(locator);
    }
    async poster(contents) {
        await this.reddit.nap();
        const titleLocator = this.reddit.page.locator("#innerTextArea").first();
        const bodyLocator = this.reddit.page.locator('div[slot="rte"][aria-label="Post body text field"]');
        const postTypeValue = await this.reddit.page
            .locator('r-post-type-select[name="type"]')
            .getAttribute("value");
        bang("Post type", postTypeValue === "TEXT", { postTypeValue });
        bang("Post body text field", await bodyLocator.innerText(), { bodyLocator });
        bang("Post title text field", await titleLocator.count(), { titleLocator });
        const { title, content } = await contents();
        await this.reddit.pressSequentially(titleLocator, title);
        await this.reddit.pressSequentially(bodyLocator, content);
        const submitButton = this.reddit.page
            .locator("r-post-form-submit-button#submit-post-button")
            .getByRole("button");
        await this.reddit.click(submitButton);
    }
}
export default async function (opts, action) {
    const { reddit } = await Reddito(opts, action);
    const subreddit = new Subreddit(reddit);
    return { reddit, subreddit };
}
