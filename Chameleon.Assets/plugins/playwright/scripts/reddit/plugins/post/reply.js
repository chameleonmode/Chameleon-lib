import { rando } from "../../../../lib/utils.js";
import Pager from "../../page.js";
export default async function (context, opts) {
    const { reddit } = await Pager(context, opts, async (url) => {
        if (reddit.opts.args.search || url)
            await reddit.post.assert();
        const b64 = [await reddit.screenshot()];
        const comments = await reddit.post.getComments();
        const { locator, text } = rando(comments);
        await reddit.post.replyToComment(locator, async () => {
            const result = await reddit.ask({
                task: `reply to this reddit comment`,
                image: { des: "page screenshot", b64 },
                generations: {
                    type: "reply",
                    sys: `1. Reply to this comment: ${text}\n2. Match word count to the range of existing comments and replies`,
                    range: { min: 1, max: 1 },
                    context: reddit.page.url(),
                    input: {
                        type: "comment",
                        data: comments.map((c) => c.text),
                        reason: "existing array of comments on the post",
                    },
                },
            });
            return result[0].data;
        });
    });
    await reddit.player.play();
}
