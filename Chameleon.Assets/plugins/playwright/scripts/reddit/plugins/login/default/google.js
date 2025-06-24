import Reddit from "../../../reddit.js";
import { Login } from "../login.js";
export default async function (context, options) {
    const { reddit } = await Reddit(context, {}, async () => { });
    await new Login(reddit).loginWithGoogle(options.email, options.password);
}
