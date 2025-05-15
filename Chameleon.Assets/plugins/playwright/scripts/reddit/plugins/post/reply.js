import Pager from "../../page.js";
export default async function (context, opts) {
    const { reddit, } = await Pager(context, opts, async (url) => {
        if (reddit.opts.args.search || url)
            await reddit.post.assert();
        const title = await reddit.post.title();
        const comments = await reddit.post.getComments(6);
        const { locator, text } = await reddit.post.getComment();
        await reddit.post.replyToComment(locator, async () => {
            const result = await reddit.ask({
                task: `create a reply to a reddit comment `,
                generate: {
                    sys: "Your replying to a comment",
                    terms: reddit.opts.ai.generations.terms,
                    type: "reply",
                    context: `the post at ${reddit.page.url()} is titled ${title}, some of the comments on the post are ${comments.join(", ")}`,
                    input: {
                        type: "comment",
                        data: text,
                        reason: "this is the comment i want to reply to",
                    },
                    range: {
                        min: 9,
                        max: 54,
                    },
                },
            });
            return result[0].data;
        });
    });
    await reddit.player.play();
}
