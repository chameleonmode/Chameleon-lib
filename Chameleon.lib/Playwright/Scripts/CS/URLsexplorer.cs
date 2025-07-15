using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Scripts.CS;
public class URLsexplorer : Base, IBundledCSScript {
	public string TableName => nameof(URLsexplorer);
	public string File => "URLsexplorer";
	public string Title => "Explorer";
	public string Description => "Opens a list of URLs in the browser.";

	public Dictionary<string, string> Args => new() {
		{ "urls", "Urls" },
		{ "delay" , "Time to wait each visit" },
	};

	public async Task Run(IBrowserContext context, IDictionary<string, string>? args = null) {
		var urls = args![Args.Keys.ElementAt(0)].Split(',', StringSplitOptions.TrimEntries);
		var delay = int.Parse(args[Args.Keys.ElementAt(1)]) * 1000;

		var page = await NewPage(context);
		foreach (var url in urls) {
			try {
				var link = url.Contains("://") ? url : $"https://{url}";
				_ = await page.GotoAsync(link); // Navigate to url
				await Task.Delay(delay); // Wait for N seconds
			} catch {
				// go to next url ignoring errors
			}
		}
	}
}

