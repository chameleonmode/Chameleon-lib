using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.SelectorExtraction;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline;
public class HtmlProcessingPipelineService(
		IHtmlExtractor htmlExtractor,
		IHtmlChunker htmlChunker,
		ISelectorExtractor selectorExtractor) {
	private readonly IHtmlExtractor htmlExtractor = htmlExtractor ?? throw new ArgumentNullException(nameof(htmlExtractor));
	private readonly IHtmlChunker htmlChunker = htmlChunker ?? throw new ArgumentNullException(nameof(htmlChunker));
	private readonly ISelectorExtractor selectorExtractor = selectorExtractor ?? throw new ArgumentNullException(nameof(selectorExtractor));

	public async Task<IList<SelectorInfo>> ProcessUrlAsync(
			string url,
			ExtractionOptions extractionOptions,
			HtmlChunkingOptions chunkingOptions,
			SelectorExtractionOptions selectorOptions,
			CancellationToken cancellationToken = default) {

		var html = await htmlExtractor.ExtractHtmlAsync(url, extractionOptions, cancellationToken);

		var chunkedHtmlWithSelectors = await htmlChunker.ChunkHtmlWithSelectorsAsync(html, chunkingOptions, cancellationToken);

		var finalSelectors = new List<SelectorInfo>();
		foreach (var chunk in chunkedHtmlWithSelectors) {
			var selectors = await selectorExtractor.ExtractSelectorsAsync(chunk.Chunk, selectorOptions, cancellationToken);
			finalSelectors.AddRange(selectors);
		}

		finalSelectors = finalSelectors
				.GroupBy(s => s.Selector)
				.Select(g => g.First())
				.ToList();

		return finalSelectors;
	}

	public async Task<IList<SelectorInfo>> ProcessHtmlAsync(
			string html,
			HtmlChunkingOptions chunkingOptions,
			SelectorExtractionOptions selectorOptions,
			CancellationToken cancellationToken = default) {
		var finalSelectors = new List<SelectorInfo>();
		var chunkedHtmlWithSelectors = await htmlChunker.ChunkHtmlWithSelectorsAsync(html, chunkingOptions, cancellationToken);

		foreach (var chunk in chunkedHtmlWithSelectors) {
			var selectors = await selectorExtractor.ExtractSelectorsAsync(chunk.Chunk, selectorOptions, cancellationToken);
			finalSelectors.AddRange(selectors);
		}

		finalSelectors = finalSelectors
				.GroupBy(s => s.Selector)
				.Select(g => g.First())
				.ToList();

		return finalSelectors;
	}
}
