using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace Tests.HtmlProcessingPipeline;
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
		if (browser is not null) await browser.DisposeAsync();
		playwright?.Dispose();
	}

	[Fact]
	public async Task TestExtractHtmlFromExampleDotCom() {

		var url = "https://example.com";

		var html = await htmlExtractor!.ExtractHtmlAsync(url);

		Assert.False(string.IsNullOrWhiteSpace(html));
		Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task TestNodeExtractionsFromHtml() {

		var page = await browser!.NewPageAsync();
		_ = await page.GotoAsync("https://example.com");

		page.Console += (_, msg) =>
		{
			Console.WriteLine($"[JS Console] {msg.Text}");
		};


		var rootId = await htmlExtractor!.InitializeCrawlerContextAsync(page);
		Console.WriteLine($"Root node ID is: {rootId}");

		var hasCrawler = await page.EvaluateAsync<bool>("() => typeof window.__crawler !== 'undefined'");
		if (!hasCrawler) {
			Console.WriteLine("Crawler is not defined on the page. Possibly injection failed or the page reloaded.");
		}


		var htmlChildSummaries = await page.EvaluateAsync($@"() => window.__crawler.getChildSummaries('{rootId}')");
		var childSummaries = JsonConvert.DeserializeObject<List<HtmlChildSummary>>(htmlChildSummaries.Value.GetRawText());
		if(childSummaries is not null) {
			foreach (var child in childSummaries) {
				var grandchildren = await page.EvaluateAsync($@"() => window.__crawler.getChildSummaries('{child.NodeId}')");
				var grandChildSummaries = JsonConvert.DeserializeObject<List<HtmlChildSummary>>(grandchildren.Value.GetRawText());
				Console.WriteLine($"Child node ID: {child.NodeId}, tag: {child.TagName}, text: {child.ShortText}");
			}
		}

		Assert.False(string.IsNullOrWhiteSpace(rootId));
		Assert.True(childSummaries.Any());
	}

	public static string UnescapeQuotes(string doubleEscapedJson) {
		return Regex.Replace(doubleEscapedJson, "\"", "\"");
	}

}
