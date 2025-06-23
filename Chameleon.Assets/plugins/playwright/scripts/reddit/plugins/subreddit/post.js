import Reddit from "../../page.js";
export default async function (context, opts) {
    const { reddit } = await Reddit(context, opts, async (url) => {
        await reddit.post.assert();
        const b64 = [await reddit.screenshot()];
        const comments = await reddit.post.getComments();
        const context = `The post will be about this post at ${reddit.page.url()}`;
        await reddit.post.visitCommunity();
        await reddit.subreddit.canPost();
        b64.push(await reddit.screenshot());
        await reddit.poster(async () => {
            const titlee = await reddit.ask({
                task: `generate a post title for this subreddit community`,
                image: { des: "page screenshots", b64 },
                generations: {
                    type: "title",
                    range: { min: 1, max: 1 },
                    context: `context: ${context}\ncurrent page url: ${reddit.page.url()}`,
                    input: {
                        type: "comment",
                        data: comments.map((c) => c.text),
                        reason: "existing array of comments on the post",
                        user_intent: `Creating a post on a subreddit community page`,
                    },
                },
            });
            const titler = reddit.bang("post title response", titlee.find((data) => {
                if (data.type === "title")
                    return data;
            }));
            b64.push(await reddit.screenshot());
            const contentlee = await reddit.ask({
                task: `create the post content`,
                image: { des: "page screenshots", b64 },
                generations: {
                    type: "post",
                    range: { min: 1, max: 1 },
                    context: `context: ${context}\ncurrent page url: ${reddit.page.url()}`,
                    input: {
                        type: "title",
                        data: [titler.data],
                        reason: "the title of the post to generate content for",
                        user_intent: `Creating a post on a subreddit community page`,
                    },
                },
            });
            const contentler = reddit.bang("post content response", contentlee.find((data) => {
                if (data.type === "post")
                    return data;
            }));
            return { title: titler.data, content: contentler.data };
        });
        await reddit.nap();
    });
    await reddit.player.play();
}
