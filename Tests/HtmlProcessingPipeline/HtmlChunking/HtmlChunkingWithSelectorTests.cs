using Chameleon.lib.Playwright.HtmlChunking;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;

namespace Tests.HtmlProcessingPipeline.HtmlChunking;
public class HtmlChunkingTests {
	private readonly IHtmlChunker chunker;

	public HtmlChunkingTests() {
		chunker = new HtmlChunkingService();
	}

	[Fact]
	public async Task TestEmptyHtmlReturnsEmptyList() {

		var emptyHtml = "";
		var options = new HtmlChunkingOptions();

		var chunks = await chunker.ChunkHtmlAsync(emptyHtml, options);

		Assert.Empty(chunks);
	}

	[Fact]
	public async Task TestBasicChunkingReturnsSingleChunkForShortHtml() {

		var html = "<html><body><p>Short content</p></body></html>";
		var options = new HtmlChunkingOptions {
			MaxChunkSize = 1000,
			MinChunkSize = 50,
			BreakAtTagBoundaries = false
		};

		var chunks = await chunker.ChunkHtmlAsync(html, options);

		_ = Assert.Single(chunks);
		Assert.Equal(html.Trim(), chunks[0]);
	}

	[Fact]
	public async Task TestChunkingBreaksIntoMultipleChunks() {

		var repeatedSection = "<div>Repeated content</div>";
		var html = "<html><body>" + string.Concat(Enumerable.Repeat(repeatedSection, 50)) + "</body></html>";
		var options = new HtmlChunkingOptions {
			MaxChunkSize = 200,
			MinChunkSize = 50,
			BreakAtTagBoundaries = true
		};

		var chunks = await chunker.ChunkHtmlAsync(html, options);

		Assert.True(chunks.Count > 1);
		foreach (var chunk in chunks) {
			// Allow some tolerance for tag boundary adjustments.
			Assert.True(chunk.Length <= options.MaxChunkSize + 50);
		}
	}

	[Fact]
	public async Task TestChunkHtmlWithSelectors_BasicSelector() {

		var html = "<html><body><div>Content A</div><section>Content B</section></body></html>";
		var options = new HtmlChunkingOptions {
			MaxChunkSize = 100,
			MinChunkSize = 10,
			BreakAtTagBoundaries = true,
			UseDetailedSelector = false
		};

		var chunksWithSelectors = await chunker.ChunkHtmlWithSelectorsAsync(html, options);

		Assert.NotEmpty(chunksWithSelectors);
		foreach (var chunk in chunksWithSelectors) {
			Assert.False(string.IsNullOrWhiteSpace(chunk.Selector));
		}
	}

	[Fact]
	public async Task TestChunkHtmlWithSelectors_DetailedIdSelector() {
		var html = "<html><body><div id='testId'>Content A</div><p>Content B</p></body></html>";
		var options = new HtmlChunkingOptions {
			MaxChunkSize = 100,
			MinChunkSize = 10,
			BreakAtTagBoundaries = true,
			UseDetailedSelector = true
		};

		var chunksWithSelectors = await chunker.ChunkHtmlWithSelectorsAsync(html, options);

		Assert.NotEmpty(chunksWithSelectors);

		var firstChunk = chunksWithSelectors[0];
		Assert.Contains("#testId", firstChunk.Selector, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task TestChunkHtmlWithSelectors_DetailedClassSelector() {

		var html = "<html><body><section class='testClass extra'>Content A</section><div>Content B</div></body></html>";
		var options = new HtmlChunkingOptions {
			MaxChunkSize = 100,
			MinChunkSize = 10,
			BreakAtTagBoundaries = true,
			UseDetailedSelector = true
		};

		var chunksWithSelectors = await chunker.ChunkHtmlWithSelectorsAsync(html, options);

		Assert.NotEmpty(chunksWithSelectors);
		var firstChunk = chunksWithSelectors[0];
		Assert.Contains(".testClass", firstChunk.Selector, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task TestChunkHtmlWithSelectors_FallbackToTag() {

		var html = "<html><body><article>Content A</article><div>Content B</div></body></html>";
		var options = new HtmlChunkingOptions {
			MaxChunkSize = 100,
			MinChunkSize = 10,
			BreakAtTagBoundaries = true,
			UseDetailedSelector = true
		};

		var chunksWithSelectors = await chunker.ChunkHtmlWithSelectorsAsync(html, options);

		Assert.NotEmpty(chunksWithSelectors);
		foreach (var chunk in chunksWithSelectors) {
			Assert.False(string.IsNullOrWhiteSpace(chunk.Selector));
		}
	}
}
