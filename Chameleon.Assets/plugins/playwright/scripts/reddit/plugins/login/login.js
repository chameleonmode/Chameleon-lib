import { bang } from "../../../../lib/utils.js";
export class Login {
    pager;
    constructor(pager) {
        this.pager = pager;
    }
    async checkLoginAuthentication() {
        const locato = this.pager.page.locator("#login-button").first();
        bang("Login button", await locato.isVisible(), locato);
        await this.pager.click(locato);
    }
    async loginWithCredentials(email, password) {
        await this.checkLoginAuthentication();
        const loginUserNameInput = this.pager.page.locator("faceplate-text-input#login-username input");
        await this.pager.pressSequentially(loginUserNameInput, email);
        await this.pager.page.keyboard.press("Tab");
        const loginUserPassword = this.pager.page.locator("faceplate-text-input#login-password input");
        await this.pager.pressSequentially(loginUserPassword, password);
        const loginUserButton = this.pager.page.getByRole("button", { name: "Log In" });
        await this.pager.click(loginUserButton);
    }
    async loginWithGoogle(email, password) {
        await this.checkLoginAuthentication();
        const { frame } = await this.pager.findFrame([
            'iframe[src*="accounts.google.com/gsi/button"]',
            'iframe[allow="identity-credentials-get"]',
            'iframe[id^="gsi_"]',
            'iframe[title="Sign in with Google Button"]',
            'iframe[title*="Google"]',
        ]);
        await frame.locator('div[role="button"]').click();
        const popup = await this.pager.page.waitForEvent("popup");
        await popup.waitForLoadState();
        const emailButtons = popup.locator("[data-email]");
        if ((await emailButtons.count()) > 0) {
            return await emailButtons.first().click();
        }
        const emailInput = popup.getByLabel("Email or phone");
        await this.pager.pressSequentially(emailInput, email);
        const nextButton = popup.locator("div#identifierNext button");
        await this.pager.click(nextButton);
        const passwordInput = popup.getByLabel("Enter your password");
        await this.pager.pressSequentially(passwordInput, password);
        const passwordNextButton = popup.locator("div#passwordNext button");
        await this.pager.click(passwordNextButton);
    }
}
