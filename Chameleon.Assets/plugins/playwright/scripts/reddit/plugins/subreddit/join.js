import { configure } from "../../configure.js";
import Reddit from "../../reddit.js";
export default async function (context, opts) {
    const options = configure(opts);
    options.settings.start.iterations.min = Math.max(options.settings.start.rando.min, options.settings.start.iterations.min);
    options.settings.start.iterations.max = options.settings.start.iterations.min;
    const { reddit } = await Reddit(context, options, async (_) => {
        await reddit.subreddit.joiner();
    });
    await reddit.player.play();
}
