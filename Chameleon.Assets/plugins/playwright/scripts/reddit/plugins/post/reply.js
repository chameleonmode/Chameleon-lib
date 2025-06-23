import { rando } from "../../../../lib/utils.js";
import Pager from "../../page.js";
import { promptee } from "../../../../lib/requests.js";
export default async function (context, opts) {
    const { reddit } = await Pager(context, opts, async (url) => {
        if (reddit.opts.args.search || url)
            await reddit.post.assert();
        const b64 = [await reddit.screenshot()];
        const rawHTML = await reddit.post.raw();
        const comments = await reddit.post.getComments();
        const comment = rando(comments);
        await reddit.post.replyToComment(comment.locator, async () => {
            const result = await promptee.robot({
                model: "o4-mini",
                decorators: reddit.opts.ai.decorators,
                task: "generate_reddit_reply",
                image: {
                    des: "page screenshot",
                    b64: b64,
                },
                generations: {
                    type: "reply",
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
                                type: "comment",
                                text: comment.text,
                                attributes: comment.attributes,
                                index: comment.index,
                            },
                            user_intent: "Generate a reply to this comment",
                        },
                        reason: "Replying to a reddit comment.",
                    },
                },
            });
            return result[0].data;
        });
    });
    await reddit.player.play();
}
