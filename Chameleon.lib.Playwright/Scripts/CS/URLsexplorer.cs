using Chameleon.lib.Playwright.Interfaces;
using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Scripts.CS;
public class URLsexplorer : Base, IBundledCSScript {
	public string Name => "URLsexplorer";
	public string Title => "URLs Explorer";
	public string Description => "Opens a list of URLs in the browser.";

	public IDictionary<string, string> Parameters => new Dictionary<string, string>() {
		{ "urls", "Urls" },
		{ "delay" , "Time to wait each visit" },
	};

	public async Task Run(IBrowserContext context, IDictionary<string, string>? args = null) {
		var urls = args![Parameters.Keys.ElementAt(0)].Split(',', StringSplitOptions.TrimEntries);
		var delay = int.Parse(args[Parameters.Keys.ElementAt(1)]) * 1000;

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

