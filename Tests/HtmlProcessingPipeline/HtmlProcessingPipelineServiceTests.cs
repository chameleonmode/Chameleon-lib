using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;
using Chameleon.lib.Playwright.HtmlProcessingPipeline;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.SelectorExtraction;

namespace Tests.HtmlProcessingPipeline;
public class DummyHtmlExtractor : IHtmlExtractor {
	public Task<string> ExtractHtmlAsync(string url, ExtractionOptions? options = null, CancellationToken cancellationToken = default) {
		return Task.FromResult(string.Empty);
	}
}
public class HtmlProcessingPipelineServiceTests {
	private readonly HtmlProcessingPipelineService pipelineService;

	public HtmlProcessingPipelineServiceTests() {
		IHtmlExtractor dummyExtractor = new DummyHtmlExtractor();
		IHtmlChunker chunker = new HtmlChunkingService();
		ISelectorExtractor selectorExtractor = new SelectorExtractionService();

		pipelineService = new HtmlProcessingPipelineService(dummyExtractor, chunker, selectorExtractor);
	}

	[Fact]
	public async Task TestProcessHtmlAsync_ReturnsExpectedSelectors() {
		var html = @"
                <html>
                  <body>
                    <div id='main' class='content'>Hello World</div>
                    <p>Paragraph content</p>
                    <section class='info'>Additional section</section>
                  </body>
                </html>";

		var chunkingOptions = new HtmlChunkingOptions {
			MaxChunkSize = 1000,
			MinChunkSize = 200,
			BreakAtTagBoundaries = true,
			UseDetailedSelector = true
		};

		var selectorOptions = new SelectorExtractionOptions {
			IncludeTagName = true,
			IncludeId = true,
			IncludeClasses = true,
			IncludeAttributes = false,
			IncludeInnerText = true
		};

		var resultSelectors = await pipelineService.ProcessHtmlAsync(html, chunkingOptions, selectorOptions);

		Assert.NotEmpty(resultSelectors);

		var mainDivSelector = resultSelectors.FirstOrDefault(s => s.Selector.Contains("div#main"));
		Assert.NotNull(mainDivSelector);
		Assert.Equal("Hello World", mainDivSelector.InnerText?.Trim());

		var paragraph = resultSelectors.FirstOrDefault(s => s.TagName.Equals("p", StringComparison.OrdinalIgnoreCase));
		Assert.NotNull(paragraph);

		var section = resultSelectors.FirstOrDefault(s => s.Selector.Contains("section"));
		Assert.NotNull(section);
	}
}
