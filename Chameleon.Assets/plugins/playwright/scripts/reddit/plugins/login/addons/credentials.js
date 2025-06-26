import Reddit from "../../../reddit.js";
import { Login } from "../login.js";
export default async function (ctx, opts) {
    const { reddit } = await Reddit({ ctx, opts: {} }, async () => { });
    await new Login(reddit).loginWithCredentials(opts.email, opts.password);
}
