import Reddit from "../../../reddit.js";
import { Subreddit } from "../subreddit.js";
export default async function (context, opts) {
    const { reddit } = await Reddit(context, opts, async () => {
        const subreddit = new Subreddit(reddit);
        await subreddit.voter();
    });
    await reddit.player.play();
}
