import Subreddit from "../subreddit.js";
export default async function (ctx, opts) {
    const { reddit, subreddit } = await Subreddit({ ctx, opts }, async (_, __) => {
        await subreddit.joiner();
    });
    reddit.opts.settings.start.iterations.min = Math.max(reddit.opts.settings.start.rando.min, reddit.opts.settings.start.iterations.min);
    reddit.opts.settings.start.iterations.max = reddit.opts.settings.start.iterations.min;
    await reddit.player.play();
}
