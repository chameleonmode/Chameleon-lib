import User from "../user.js";
export default async function (ctx, opts) {
    const { reddit, user } = await User({ ctx, opts }, async (_, __) => await user.follow());
    reddit.opts.args.scope = "People";
    await reddit.player.play();
}
