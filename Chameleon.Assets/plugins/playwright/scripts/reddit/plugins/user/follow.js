import Reddit from "../../page.js";
import { User } from "./page.js";
export default async function (context, opts) {
    const { reddit } = await Reddit(context, opts, async () => await user.follow());
    const user = new User(reddit);
    await reddit.player.play();
}
