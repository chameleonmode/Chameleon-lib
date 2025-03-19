using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;

namespace Tests.HtmlProcessingPipeline;
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
}
