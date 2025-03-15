using Chameleon.lib.Playwright.HtmlProcessingPipeline.SelectorExtraction;

namespace Tests.HtmlProcessingPipeline;
public class SelectorExtractionTests {
	private readonly ISelectorExtractor extractor;

	public SelectorExtractionTests() {
		extractor = new SelectorExtractionService();
	}

	[Fact]
	public async Task TestExtractSelectors_IncludesTagIdAndClasses() {

		var html = @"
                <html>
                  <body>
                    <div id='mainDiv' class='container content'>Hello World</div>
                    <section class='info'>Information here</section>
                    <p>Paragraph text</p>
                  </body>
                </html>";
		var options = new SelectorExtractionOptions {
			IncludeTagName = true,
			IncludeId = true,
			IncludeClasses = true,
			IncludeAttributes = false,
			IncludeInnerText = true
		};

		var selectors = await extractor.ExtractSelectorsAsync(html, options);

		Assert.NotEmpty(selectors);

		var divSelector = selectors.FirstOrDefault(s => s.TagName == "div" && s.Id == "mainDiv");
		Assert.NotNull(divSelector);
		Assert.Contains("#mainDiv", divSelector.Selector);
		Assert.Contains("container", divSelector.Selector);
		Assert.Contains("content", divSelector.Selector);

		Assert.Equal("Hello World", divSelector.InnerText);

		var sectionSelector = selectors.FirstOrDefault(s => s.TagName == "section");
		Assert.NotNull(sectionSelector);
		Assert.Contains(".info", sectionSelector.Selector);
	}

	[Fact]
	public async Task TestExtractSelectors_WithoutIdAndClass() {
		var html = @"
                <html>
                  <body>
                    <article>Article content</article>
                    <footer>Footer content</footer>
                  </body>
                </html>";
		var options = new SelectorExtractionOptions {
			IncludeId = true,
			IncludeClasses = true,
			IncludeAttributes = false,
			IncludeInnerText = false
		};

		var selectors = await extractor.ExtractSelectorsAsync(html, options);

		foreach (var info in selectors) {
			Assert.False(string.IsNullOrWhiteSpace(info.Selector));
			if (info.TagName is "article" or "footer") 				Assert.Equal(info.TagName, info.Selector);
		}
	}
}
