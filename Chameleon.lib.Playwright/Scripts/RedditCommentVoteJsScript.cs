using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.node;

namespace Chameleon.lib.Playwright.Scripts;
public class RedditCommentVoteJsScript : IBundledJSScript {
	public string Title => "Reddit Search And Comment";
	public string Description => "Search for reddit thread comment vote and reply";
	public string Name => "reddit1comment";
	public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>() {
		{ "search" , "Search" },
		{ "comment" , "Comment" },
		{ "username", "Username" },
		{ "password", "Password" },
	};

	public async Task Run(int port, IDictionary<string, string>? args = null)
	{
		ArgumentNullException.ThrowIfNull(args, nameof(args));

		var data = new
		{
			search = args["search"],
			comment1 = args["comment"],
			username = args["username"] ?? Parameters["username"],
			password = args["password"] ?? Parameters["password"],
		};

		using var runner = PlaywrightTestRunner.Create(Name);
		await runner.RunTestAsync(data, port);
	}
}
