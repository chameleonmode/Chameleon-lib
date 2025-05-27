import { configure } from "../../reddit.js";
import Reddit from "../../page.js";
export default async function (context, opts) {
    const options = configure(opts);
    options.settings.start.iterations.min = Math.max(options.settings.start.rando.min, options.settings.start.iterations.min);
    options.settings.start.iterations.max = Math.max(options.settings.start.rando.max, options.settings.start.iterations.max);
    const { reddit } = await Reddit(context, options, async (_) => {
        await reddit.subreddit.joiner();
    });
    await reddit.player.play();
}
