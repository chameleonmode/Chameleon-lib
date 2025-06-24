import Reddit from "../../reddit.js";
import { promptee } from "../../../../lib/requests.js";
export default async function (ctx, opts) {
    const { reddit } = await Reddit(ctx, opts, async (url) => {
        await reddit.post.assert();
        await reddit.post.archived(reddit.click);
        const { content, screenshot, comments } = await reddit.post.raw();
        await reddit.post.addComment(async () => {
            const result = await promptee.robot({
                model: "o4-mini",
                decorators: reddit.opts.ai.decorators,
                task: "generate_reddit_comment",
                image: { des: "post screenshot", b64: [screenshot] },
                generations: {
                    type: "comment",
                    range: { min: 1, max: 1 },
                    input: {
                        data: {
                            post: {
                                id: crypto.randomUUID(),
                                url: reddit.page.url(),
                                content,
                                comments,
                            },
                            target: {
                                type: "post",
                            },
                        },
                        user_intent: "Generate a comment to this post",
                    },
                },
            });
            return result[0].data;
        });
    });
    await reddit.player.play();
}
