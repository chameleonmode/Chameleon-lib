import Reddit from "../../../reddit.js";
import { Login } from "../login.js";
export default async function (ctx, options) {
    const { reddit } = await Reddit({ ctx, opts: {} }, async () => { });
    await new Login(reddit).loginWithGoogle(options.email, options.password);
}
