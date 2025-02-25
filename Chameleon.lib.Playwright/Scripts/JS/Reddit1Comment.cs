using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.node;

namespace Chameleon.lib.Playwright.Scripts.JS;
public class Reddit1Comment : IBundledJSScript {
	public string Name => "reddit1comment";
	public string Title => "Reddit Search And Comment";
	public string Description => "Search for reddit thread comment vote and reply";
	public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>() {
		{ "search" , "Search" },
		{ "comment" , "Comment" },
	};

	public async Task Run(int port, IDictionary<string, string>? args = null) {
		using var runner = PlaywrightTestRunner.Create(Name);
		await runner.RunTestAsync(port, args);
	}
}
