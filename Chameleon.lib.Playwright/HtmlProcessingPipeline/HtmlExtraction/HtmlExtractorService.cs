using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
public class HtmlExtractorService(IBrowser browser) : IHtmlExtractor, IDisposable {

	private readonly IBrowser browser = browser ?? throw new ArgumentNullException(nameof(browser));

	public async Task<string> ExtractHtmlAsync(string url, ExtractionOptions? options = null, CancellationToken cancellationToken = default) {
		options ??= new ExtractionOptions();

		var page = await browser.NewPageAsync();

		page.SetDefaultNavigationTimeout(options.NavigationTimeout);
		page.SetDefaultTimeout(options.WaitTimeout);

		_ = await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

		if (!string.IsNullOrEmpty(options.WaitForSelector)) _ = await page.WaitForSelectorAsync(options.WaitForSelector, new PageWaitForSelectorOptions { Timeout = options.WaitTimeout });

		var html = await page.EvaluateAsync<string>("() => document.documentElement.outerHTML");
		return html;
	}

	public void Dispose() {
		browser?.DisposeAsync().GetAwaiter().GetResult();
	}
}
