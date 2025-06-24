import Pager from "../../reddit.js";
import { promptee } from "../../../../lib/requests.js";
export default async function (context, opts) {
    const { reddit } = await Pager(context, opts, async (_) => {
        await reddit.post.assert();
        await reddit.post.archived(reddit.assert);
        const { content, screenshot, comments, id, url } = await reddit.post.raw();
        const result = await promptee.robot({
            model: "o4-mini",
            decorators: reddit.opts.ai.decorators,
            task: "generate_reddit_reply",
            image: {
                des: "page screenshot",
                b64: [screenshot],
            },
            generations: {
                type: "reply",
                range: { min: 1, max: 1 },
                input: {
                    data: {
                        post: { id, url, content, comments },
                        target: {
                            type: "unknown",
                        },
                    },
                    user_intent: "Select a comment aligned with users metadata and generate a reply to it",
                },
            },
        });
        const comment = reddit.banger(comments.find((c) => c.id === result[0].id));
        await reddit.post.replyToComment(comment.locator, async () => {
            await reddit.nap();
            return result[0].data;
        });
    });
    await reddit.player.play();
}
