import Reddit from "../../page.js";
export default async function (context, options) {
    const { reddit } = await Reddit(context, {}, async () => { });
    await reddit.login.loginWithGoogle(options.email, options.password);
}
