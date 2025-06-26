import Subreddit from "../subreddit.js";
export default async function (ctx, opts) {
    const { reddit, subreddit } = await Subreddit({ ctx, opts }, async (_, __) => {
        await subreddit.voter();
    });
    await reddit.player.play();
}
