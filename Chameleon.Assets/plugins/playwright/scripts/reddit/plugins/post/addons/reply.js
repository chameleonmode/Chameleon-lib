import { promptee } from "../../../../../lib/requests.js";
import Reddit from "../../../reddit.js";
import { Post } from "../post.js";
export default async function (context, opts) {
    const b64 = [];
    const comments = [];
    const target = { type: "unknown" };
    const { reddit } = await Reddit(context, opts, async (_, thread) => {
        const post = new Post(reddit);
        const posts = (await reddit.navigateIntoPost()) ?? (await reddit.joinConversation());
        const raw = Array.isArray(posts)
            ? await reddit.findo(posts, async (thread) => {
                await post.archived(reddit.assert);
                const raw = await post.raw();
                if (reddit.scopeulate().people && thread.attributes?.["data-ks-id"]) {
                    reddit.page.goBack();
                    await reddit.nap();
                    comments.push(...(await reddit.getComments()));
                    target.comment = post.pager.banger(comments.find((c) => thread?.attributes["data-ks-id"] === c.attributes.thingid));
                    target.type = "comment";
                }
                return raw;
            })
            : await post.raw();
        b64.push(raw.screenshot);
        comments.push(...raw.comments.slice(0, 36));
        const result = await promptee.robot({
            model: "o4-mini",
            decorators: reddit.opts.ai.decorators,
            task: "generate_reddit_reply",
            image: {
                des: "relevant page screenshots",
                b64,
            },
            generations: {
                type: "reply",
                range: { min: 1, max: 1 },
                input: {
                    data: {
                        target,
                        post: {
                            id: raw.id,
                            url: raw.url,
                            content: raw.content,
                            comments,
                        },
                    },
                    user_intent: target.type === "unknown"
                        ? "Select a comment aligned with users metadata and generate a reply to it"
                        : "Generate a reply to this comment",
                },
            },
        });
        const comment = reddit.banger(comments.find((c) => (c.id === target.comment?.id ? target.comment.id : result[0].id)));
        await post.replyToComment(comment.locator, async () => {
            await reddit.nap();
            return result[0].data;
        });
    });
    await reddit.player.play();
}
