using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;

namespace Tests.HtmlProcessingPipeline;
public class HtmlExtractionTests : Base {
	private HtmlExtractorService? htmlExtractor;

	public override async Task InitializeAsync() {
		await base.InitializeAsync();
		Assert.NotNull(playwright);
		Assert.NotNull(headlessBrowser);
		htmlExtractor = new HtmlExtractorService(headlessBrowser);
	}

	[Fact]
	public async Task TestExtractHtmlFromExampleDotCom() {
		var url = "https://example.com";

		var html = await htmlExtractor!.ExtractHtmlAsync(url);

		Assert.False(string.IsNullOrWhiteSpace(html));
		Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
	}

}
