using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.node;

namespace Chameleon.lib.Playwright.Scripts.JS.Reddit;
public class Reddit1Comment : IBundledJSScript {
	public string File => "reddit/comment.plugin";
	public string Title => "Reddit Search And Comment";
	public string Description => "Search for reddit thread comment vote and reply";
	public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>() {
		{ "search" , "Search" },
		{ "comment" , "Comment" },
	};

	public async Task Run(int port, IDictionary<string, string>? options = null) {
		using var runner = PlaywrightTestRunner.Create(File);
		await runner.RunTestAsync(port, options);
	}
}