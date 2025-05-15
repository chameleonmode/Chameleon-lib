import Reddit from "../../page.js";
export default async function (context, opts) {
    const { reddit } = await Reddit(context, opts, async (url) => {
        await reddit.post.assert();
        const sys = "Your creating a post on a subreddit community page";
        const titled = await reddit.post.title();
        const comments = await reddit.post.getComments(3);
        const terms = reddit.opts.ai.generations.terms;
        const context = `The post will be about a post at ${reddit.page.url()} its title is ${titled}.
      Some of the comments on that post are ${comments.join(", ")}`;
        await reddit.post.visitCommunity();
        await reddit.subreddit.canPost();
        await reddit.poster(async () => {
            const titlee = await reddit.ask({
                task: `generate a post title on a subreddit community`,
                generate: {
                    sys,
                    terms,
                    context,
                    type: "title",
                    input: {
                        type: "title",
                        data: titled,
                        reason: "this is the post title i want to base the new post on",
                    },
                    range: { min: 3, max: 9 },
                },
            });
            const titler = reddit.bang("post title response", titlee.find((data) => {
                if (data.type === "title")
                    return data;
            }));
            const contentlee = await reddit.ask({
                task: `create the reddit post content`,
                generate: {
                    sys,
                    terms,
                    type: "post",
                    context: `${context}
            The post title of your content will be ${titler.data} and the reason is ${titler.reason}`,
                    input: titler,
                    range: { min: 18, max: 54 },
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
