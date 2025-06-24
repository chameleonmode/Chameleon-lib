export class Login {
    reddit;
    constructor(reddit) {
        this.reddit = reddit;
    }
    async checkLoginAuthentication() {
        const locato = this.reddit.page.locator("#login-button").first();
        this.reddit.bang("Login button", await locato.isVisible(), locato);
        await this.reddit.click(locato);
    }
    async loginWithCredentials(email, password) {
        await this.checkLoginAuthentication();
        const loginUserNameInput = this.reddit.page.locator("faceplate-text-input#login-username input");
        await this.reddit.pressSequentially(loginUserNameInput, email);
        await this.reddit.page.keyboard.press("Tab");
        const loginUserPassword = this.reddit.page.locator("faceplate-text-input#login-password input");
        await this.reddit.pressSequentially(loginUserPassword, password);
        const loginUserButton = this.reddit.page.getByRole("button", { name: "Log In" });
        await this.reddit.click(loginUserButton);
    }
    async loginWithGoogle(email, password) {
        await this.checkLoginAuthentication();
        const { frame } = await this.reddit.findFrame([
            'iframe[src*="accounts.google.com/gsi/button"]',
            'iframe[allow="identity-credentials-get"]',
            'iframe[id^="gsi_"]',
            'iframe[title="Sign in with Google Button"]',
            'iframe[title*="Google"]',
        ]);
        await frame.locator('div[role="button"]').click();
        const popup = await this.reddit.page.waitForEvent("popup");
        await popup.waitForLoadState();
        const emailButtons = popup.locator("[data-email]");
        if ((await emailButtons.count()) > 0) {
            return await emailButtons.first().click();
        }
        const emailInput = popup.getByLabel("Email or phone");
        await this.reddit.pressSequentially(emailInput, email);
        const nextButton = popup.locator("div#identifierNext button");
        await this.reddit.click(nextButton);
        const passwordInput = popup.getByLabel("Enter your password");
        await this.reddit.pressSequentially(passwordInput, password);
        const passwordNextButton = popup.locator("div#passwordNext button");
        await this.reddit.click(passwordNextButton);
    }
}
