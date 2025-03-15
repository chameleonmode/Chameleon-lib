namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;
public class HtmlChunk(string chunk, string selector) {
	public string Chunk { get; set; } = chunk;
	public string Selector { get; set; } = selector;
}