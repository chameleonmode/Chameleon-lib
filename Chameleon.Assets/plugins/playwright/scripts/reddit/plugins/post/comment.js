import Reddit from "../../page.js";
export default async function (ctx, opts) {
    const { reddit } = await Reddit(ctx, opts, async (url) => {
        await reddit.post.assert();
        const title = await reddit.post.title();
        const comments = await reddit.post.getComments(3);
        await reddit.post.addComment(async () => {
            const result = await reddit.ask({
                task: `respond to a reddit post with a comment`,
                generate: {
                    sys: "Your commenting on a post",
                    terms: reddit.opts.ai.generations.terms,
                    type: "comment",
                    context: `some of the comments on the post at ${reddit.page.url()} are ${comments.join(", ")}`,
                    input: {
                        type: "title",
                        data: title,
                        reason: "this is the title of the post i want to comment on",
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
