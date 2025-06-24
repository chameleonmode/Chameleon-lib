using Chameleon.lib.AIR.Scripts;

namespace Chameleon.lib.Playwright.Scripts.JS.Reddit.Login;

public record Credentials : JSScript {
	public Credentials() : base(
		"../scripts/reddit/plugins/login/default/credentials",
		"Reddit Login",
		"Authenticate to reddit using credentials"
	) { }

	public override Dictionary<string, string> Args => new() {
		{ "email", "email" },
		{ "password", "password" },
    { "title", "Reddit" },
    { "website", "Reddit.com" }
	};
}