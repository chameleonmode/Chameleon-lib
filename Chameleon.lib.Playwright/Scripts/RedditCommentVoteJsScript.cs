using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.node;

namespace Chameleon.lib.Playwright.Scripts;
public class RedditCommentVoteJsScript : IBundledJSScript {
	public string Title => "Reddit Comment Vote";
	public string Description => "Search for reddit thread comment vote and reply";
	public string Name => "redditCommentVote";
	public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>() {
		{ "textToSearch" , "Search Key Word" },
		{ "commenttoMainthread" , "First Comment" },
		{ "commenttoMainthread2" , "Second Comment" },
		{ "replToComment" , "Reply To Comment" },
		{ "reddit_username", "Username" },
		{ "test_password", "Password" },
	};

	public async Task Run(int port, IDictionary<string, string>? args = null)
	{
		ArgumentNullException.ThrowIfNull(args, nameof(args));

		var data = new
		{
			textToSearch = args["textToSearch"],
			commenttoMainthread = args["commenttoMainthread"],
			commenttoMainthread2 = args["commenttoMainthread2"],
			replToComment = args["replToComment"],
			reddit_username = args.ContainsKey("reddit_username") ? args["reddit_username"] : Parameters["reddit_username"],
			test_password = args.ContainsKey("test_password") ? args["test_password"] : Parameters["test_password"],
		};

		using var runner = PlaywrightTestRunner.Create(Name);
		await runner.RunTestAsync(data, port);
	}
}
