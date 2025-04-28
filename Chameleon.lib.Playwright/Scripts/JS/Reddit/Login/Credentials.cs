
using Chameleon.AIR.Scripts.Models;

namespace Chameleon.lib.Playwright.Scripts.JS.Reddit.Login;

public record Credentials : JSScript {
	public Credentials() : base(
		"reddit/plugins/login/credentials",
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