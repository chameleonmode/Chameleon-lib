using Chameleon.lib.Playwright.HtmlProcessingPipeline.SelectorExtraction;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;
public interface IHtmlChunker {
	Task<IList<string>> ChunkHtmlAsync(string html, HtmlChunkingOptions options, CancellationToken cancellationToken = default);
	IList<IList<SelectorInfo>> ChunkSelectors(IList<SelectorInfo> selectors, int maxSelectorsPerChunk);
}
