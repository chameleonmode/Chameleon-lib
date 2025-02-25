using Chameleon.lib.Playwright.Interfaces;
using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Scripts.CS;
public class GoogleCTR : Base, IBundledCSScript {
	public string Name => "GoogleCTR";
	public string Title => "Google Click Through Rate";
	public string Description => "Clicks through Google search results to a target URL";
	public IDictionary<string, string> Parameters => new Dictionary<string, string>() {
		{ "search", "Search" },
		{ "targetUrl", "Target" },
		{ "maxPages" , "Max Pages To Search Through" },
	};
	public async Task Run(IBrowserContext context, IDictionary<string, string>? args = null) {
		var keyword = args![Parameters.Keys.ElementAt(0)];
		var targetUrl = args![Parameters.Keys.ElementAt(1)];
		var maxPages = int.Parse(Parameters.Keys.ElementAt(2));

		var page = await NewPage(context);
		// Go to Google
		_ = await page.GotoAsync("https://www.google.com");
		await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

		// Put the keyword into the search bar
		var searchInput = page.Locator("//*[@id='APjFqb']");
		await searchInput.FillAsync(keyword);
		await searchInput.PressAsync("Enter");

		// Wait for results to load
		await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
		var found = false;
		for (var i = 0; i < maxPages && !found; i++) {
			// Wait for search results
			_ = await page.WaitForSelectorAsync("a[href]");

			// Look for the target URL in the search results
			var links = await page.Locator("a[href]").ElementHandlesAsync();
			foreach (var link in links) {
				var href = await link.GetAttributeAsync("href");
				if (href != null && href.Contains(targetUrl)) {
					await link.ClickAsync();
					found = true;
					break;
				}
			}


			// If not found, go to the next page
			var nextPage = page.GetByLabel($"Page {i + 2}");
			if (!found && await nextPage.IsVisibleAsync()) {
				await nextPage.ClickAsync();
				await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
			}
		}

		// If the target URL was found, look for another internal link and click it
		if (found) {
			await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
			var internalLinks = await page.Locator($"a[href*='{targetUrl}'], a[href^='/']").ElementHandlesAsync();
			if (internalLinks.Count > 0) await internalLinks[0].ClickAsync();
		}
	}
}