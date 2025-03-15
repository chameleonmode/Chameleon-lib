namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;
public interface IHtmlChunker {
	Task<IList<string>> ChunkHtmlAsync(string html, HtmlChunkingOptions options, CancellationToken cancellationToken = default);
	Task<IList<HtmlChunk>> ChunkHtmlWithSelectorsAsync(string html, HtmlChunkingOptions options, CancellationToken cancellationToken = default);
}
