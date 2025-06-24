import Reddit from "../../reddit.js";
export default async function (context, opts) {
    const { reddit } = await Reddit(context, opts, async (url) => {
        await reddit.post.assert();
        const b64 = [await reddit.screenshot()];
        const comments = await reddit.post.getComments();
        const context = `The post will be based on ${reddit.page.url()}`;
        await reddit.post.visitCommunity();
        await reddit.subreddit.canPost();
        b64.push(await reddit.screenshot());
        await reddit.poster(async () => {
            const titlee = await reddit.ask({
                task: `generate_post_title.`,
                image: { des: "page screenshots", b64 },
                generations: {
                    type: "title",
                    range: { min: 1, max: 1 },
                    input: {
                        data: comments.map((c) => c.text),
                        user_intent: `Creating a post title on a subreddit community page. ${context}`,
                    },
                },
            });
            const titler = reddit.bang("post title response", titlee.find((data) => {
                if (data.type === "title")
                    return data;
            }));
            b64.push(await reddit.screenshot());
            const contentlee = await reddit.ask({
                task: `create_post_content`,
                image: { des: "page screenshots", b64 },
                generations: {
                    type: "post",
                    range: { min: 1, max: 1 },
                    input: {
                        data: comments.map((c) => c.text),
                        user_intent: `Creating post content for a post titled ${titler.data} on a subreddit community page. ${context}`,
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
