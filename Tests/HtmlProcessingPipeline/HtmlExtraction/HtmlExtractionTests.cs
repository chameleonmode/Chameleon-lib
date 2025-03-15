using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Microsoft.Playwright;

namespace Tests.HtmlProcessingPipeline.HtmlExtraction;
public class HtmlExtractionTests : IAsyncLifetime {
	private IPlaywright? playwright;
	private IBrowser? browser;
	private IHtmlExtractor? htmlExtractor;

	public async Task InitializeAsync() {
		playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

		htmlExtractor = new HtmlExtractorService(browser);
	}

	public async Task DisposeAsync() {
		if (browser is not null) 			await browser.DisposeAsync();
		playwright?.Dispose();
	}

	[Fact]
	public async Task TestExtractHtmlFromExampleDotCom() {

		var url = "https://example.com";

		var html = await htmlExtractor!.ExtractHtmlAsync(url);

		Assert.False(string.IsNullOrWhiteSpace(html));
		Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
	}
}
