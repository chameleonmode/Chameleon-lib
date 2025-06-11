import Reddit from "../../page.js";
export default async function (ctx, opts) {
    const { reddit } = await Reddit(ctx, opts, async (url) => {
        await reddit.post.assert();
        const comments = await reddit.post.getComments();
        await reddit.post.addComment(async () => {
            const result = await reddit.ask({
                task: `respond to this reddit post`,
                image: { des: "page screenshot", b64: [await reddit.screenshot()] },
                generations: {
                    type: "comment",
                    sys: "Match word count to the range of existing comments",
                    range: { min: 1, max: 1 },
                    context: reddit.page.url(),
                    input: {
                        type: "comment",
                        data: [comments[0].text],
                        reason: "the top comment",
                    },
                },
            });
            return result[0].data;
        });
    });
    await reddit.player.play();
}
