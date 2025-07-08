import { Logger, promptee } from "../../../../lib/index.js";
import { bang } from "../../../../lib/utils.js";
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
        const comments = [];
        if (await this.reddit.joinConversation()) {
            const these = await this.reddit.getComments();
            const min = Math.min(these.length, this.reddit.opts.settings.start.rando.min);
            try {
                while (comments.length < min) {
                    const promptmise = promptee.ranking({
                        task: `rank these reddit comments for up-voting make sure to mix and match the best comments that relate to the users incception metadata.
				do not only rank the top comments, but also include some of the lower ranked comments that are relevant to the users metadata.`,
                        generations: {
                            type: "ranking",
                            range: { min: 1, max: 1 },
                            input: {
                                data: these.filter((comment) => !comments.some((c) => c.id === comment.id)),
                                user_intent: `Rank all of these comments to up-vote on @${this.reddit.page.url()}`,
                            },
                        },
                    });
                    const reply = await this.reddit.waitabit(promptmise);
                    const ranked = reply[0].data
                        .sort((a) => a.rank)
                        .map((item) => these.find((c) => c.id === item.id))
                        .filter((comment) => comment !== undefined);
                    comments.push(...ranked);
                }
            }
            catch (error) {
                Logger.warn("Error in ranking wait", error);
            }
        }
        else {
            await this.reddit.scrollabit();
        }
        const ups = comments.length
            ? comments.map((c) => c.locator.getByRole("button", { name: "Upvote" }))
            : this.reddit.page.getByRole("button", { name: "Upvote" });
        const downs = comments.length
            ? comments.map((c) => c.locator.getByRole("button", { name: "Downvote" }))
            : this.reddit.page.getByRole("button", { name: "Downvote" });
        const upCount = Array.isArray(ups) ? ups.length : await ups.count();
        const downCount = Array.isArray(downs) ? downs.length : await downs.count();
        const count = Math.min(upCount, downCount) - 1;
        const length = Math.min(count, this.reddit.opts.settings.start.rando.min);
        bang("Vote count", length > 0, { upCount, downCount, count, length });
        for (let i = 0; i < length; i++) {
            const upLocator = Array.isArray(ups) ? ups[i] : ups.nth(i);
            const downLocator = Array.isArray(downs) ? downs[i] : downs.nth(i);
            await this.reddit.click(Math.random() * 100 <= 96 ? upLocator : downLocator);
        }
        return {
            ups: { locator: ups, count: upCount },
            downs: { locator: downs, count: downCount },
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
