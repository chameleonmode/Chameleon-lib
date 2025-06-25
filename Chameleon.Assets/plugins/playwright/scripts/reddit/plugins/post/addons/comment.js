import { promptee } from "../../../../../lib/requests.js";
import Reddit from "../../../reddit.js";
import { Post } from "../post.js";
export default async function (ctx, opts) {
    const { reddit } = await Reddit(ctx, opts, async (_) => {
        const post = new Post(reddit);
        await reddit.navigateIntoPost();
        await reddit.joinConversation();
        const { content, screenshot, comments } = await post.raw();
        await post.addComment(async () => {
            const result = await promptee.robot({
                model: "o4-mini",
                decorators: reddit.opts.ai.decorators,
                task: "generate_reddit_comment",
                image: { des: "post screenshot", b64: [screenshot] },
                generations: {
                    type: "comment",
                    range: { min: 1, max: 1 },
                    input: {
                        data: {
                            post: {
                                id: crypto.randomUUID(),
                                url: reddit.page.url(),
                                content,
                                comments: comments.slice(0, 36),
                            },
                            target: {
                                type: "post",
                            },
                        },
                        user_intent: "Generate a comment to this post",
                    },
                },
            });
            return result[0].data;
        });
    });
    await reddit.player.play();
}
