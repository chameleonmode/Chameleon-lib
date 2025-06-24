import Reddit from "../../reddit.js";
export default async function (context, opts) {
    const { reddit } = await Reddit(context, opts, async () => {
        await reddit.subreddit.voter();
    });
    await reddit.player.play();
}
