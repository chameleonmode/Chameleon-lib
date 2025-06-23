import Reddit from "../../page.js";
import { promptee } from "../../../../lib/requests.js";
export default async function (ctx, opts) {
    const { reddit } = await Reddit(ctx, opts, async (url) => {
        await reddit.post.assert();
        const b64 = [await reddit.screenshot()];
        const rawHTML = await reddit.post.raw();
        const comments = await reddit.post.getComments();
        await reddit.post.addComment(async () => {
            const result = await promptee.robot({
                model: "o4-mini",
                decorators: reddit.opts.ai.decorators,
                task: "generate_reddit_comment",
                image: {
                    des: "page screenshot",
                    b64: b64,
                },
                generations: {
                    type: "comment",
                    range: { min: 1, max: 1 },
                    input: {
                        data: {
                            post: {
                                url: reddit.page.url(),
                                rawHTML,
                                comments: comments.map((c) => ({
                                    index: c.index,
                                    text: c.text,
                                    attributes: c.attributes,
                                })),
                            },
                            target: {
                                type: "post",
                            },
                        },
                        reason: "Commenting on a reddit post.",
                        user_intent: "Generate a comment to this post",
                    },
                },
            });
            return result[0].data;
        });
    });
    await reddit.player.play();
}
