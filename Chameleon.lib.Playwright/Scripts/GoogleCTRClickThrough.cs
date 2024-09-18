using Chameleon.lib.Playwright.Interfaces;

using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Scripts;
public class GoogleCTRClickThrough : IBundledScript {
	public string Title => "Google Click Through Rate";
	public string Description => "Clicks through Google search results to a target URL";
	public IList<string> parameters => ["keyword", "targetUrl", "pagescount", "timeout"];
	public async Task Run(IBrowserContext context, IDictionary<string, string>? args = null)
	{
		ArgumentNullException.ThrowIfNull(args, nameof(args));
		var keyword = args["keyword"];
		var targetUrl = args["targetUrl"];
		if (!int.TryParse(args["pagescount"], out var pagesCount)) {
			throw new ArgumentException("Argument <pagescount> is not valid");
		}
		if (!int.TryParse(args["timeout"], out var timeout)) {
			throw new ArgumentException("Argument <timeout> is not valid");
		}

		var page = await context.NewPageAsync();

		// Convert to milliseconds
		timeout *= 1000;

		// Go to Google
		_ = await page.GotoAsync("https://www.google.com");

		var searchInput = page.Locator("//*[@id='APjFqb']");

		// Put the keyword into the search bar
		await searchInput.FillAsync(keyword);
		await searchInput.PressAsync("Enter");

		// Wait for results to load
		await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
		var found = false;
		for (var i = 0; i < pagesCount && !found; i++) {
			await Task.Delay(timeout);

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


			var nextPage = page.GetByLabel($"Page {i + 2}");
			// If not found, go to the next page
			if (!found && await nextPage.IsVisibleAsync()) {
				await nextPage.ClickAsync();
				await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
			}
		}

		// If the target URL was found, look for another internal link and click it
		if (found) {
			await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
			var internalLinks = await page.Locator($"a[href*='{targetUrl}'], a[href^='/']").ElementHandlesAsync();
			if (internalLinks.Count > 0) {
				await internalLinks[0].ClickAsync();
			}
		}
	}
}