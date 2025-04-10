
namespace Chameleon.lib.Playwright.Scripts.JS.Reddit.Login;

public class Credentials : JSScript {
	public Credentials() : base(
		"reddit/plugins/login/credentials",
		"Reddit Google Authentication",
		"Authenticate to reddit using google account"
	) { }

	public override IDictionary<string, string> Parameters => new Dictionary<string, string> {
		{ "username", "Reddit username" },
		{ "password", "Reddit password" }
	};
}