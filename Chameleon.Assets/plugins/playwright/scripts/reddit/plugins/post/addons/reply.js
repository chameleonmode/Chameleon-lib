import { promptee } from "../../../../../lib/requests.js";
import Post from "../post.js";
export default async function (ctx, opts) {
    const target = { type: "unknown" };
    const { reddit, post } = await Post({ ctx, opts }, async (_, __) => {
        const posts = (await reddit.navigateIntoPost()) ?? (await reddit.joinConversation());
        const { raw, comments } = Array.isArray(posts)
            ? await reddit.findo(posts.sort(() => Math.random() - 0.5), async (thread) => {
                await reddit.joinConversation();
                const raw = await post.raw();
                const comments = await reddit.getComments();
                if (reddit.scopeulate().people && thread.attributes?.["data-ks-id"]) {
                    reddit.page.goBack();
                    await reddit.nap();
                    const discussion = await reddit.getComments();
                    target.comment = reddit.bang("checking comment match", discussion.find((c) => thread?.attributes["data-ks-id"] === c.attributes.thingid), { thread, discussion });
                    target.type = "comment";
                    comments.push(...discussion);
                }
                return { raw, comments };
            })
            : await (async () => {
                const raw = await post.raw();
                return { raw, comments: await reddit.getComments() };
            })();
        const postee = { id: raw.id, url: raw.url, content: raw.content, comments };
        const result = await promptee.robot({
            model: "o4-mini",
            decorators: reddit.opts.ai.decorators,
            task: "generate_reddit_reply",
            image: { des: "page screenshots", b64: [raw.screenshot] },
            generations: {
                type: "reply",
                range: { min: 1, max: 1 },
                input: {
                    data: { target, post: postee },
                    user_intent: target.type === "unknown"
                        ? "Select a comment aligned with users metadata and generate a reply to it"
                        : "Generate a reply to this comment",
                },
            },
        });
        const comment = reddit.bang("finding comment", comments.find((c) => c.id === (target.comment?.id ? target.comment.id : result[0].id)), { target, result });
        await post.replyToComment(comment.locator, async () => {
            await reddit.nap();
            return result[0].data;
        });
    });
    await reddit.player.play();
}
