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
                    sys: `1. You are replying to a comment\n2. Match word count to the range of existing comments and replies on the page`,
                    range: { min: 1, max: 1 },
                    context: reddit.page.url(),
                    input: {
                        type: "comment",
                        data: [text],
                        reason: "comment to reply to",
                    },
                },
            });
            return result[0].data;
        });
    });
    await reddit.player.play();
}
