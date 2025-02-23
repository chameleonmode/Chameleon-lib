using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.node;

namespace Chameleon.lib.Playwright.Scripts;
public class Reddit1Comment : IBundledJSScript {
	public string Title => "Reddit Search And Comment";
	public string Description => "Search for reddit thread comment vote and reply";
	public string Name => "reddit1comment";
	public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>() {
		{ "search" , "Search" },
		{ "comment" , "Comment" },
	};

	public async Task Run(int port, IDictionary<string, string>? args = null) {
		ArgumentNullException.ThrowIfNull(args, nameof(args));

		using var runner = PlaywrightTestRunner.Create(Name);
		await runner.RunTestAsync(new {
			search = args["search"],
			comment = args["comment"],
		}, port);
	}
}
