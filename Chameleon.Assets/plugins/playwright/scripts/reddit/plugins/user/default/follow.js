import { configure } from "../../../configure.js";
import Reddit from "../../../reddit.js";
import { User } from "../user.js";
export default async function (context, opts) {
    const options = configure(opts);
    options.args.scope = "People";
    const { reddit } = await Reddit(context, options, async () => await new User(reddit).follow());
    await reddit.player.play();
}
