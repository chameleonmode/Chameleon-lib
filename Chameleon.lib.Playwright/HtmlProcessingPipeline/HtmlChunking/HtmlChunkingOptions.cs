namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;
public class HtmlChunkingOptions {
	public int MaxChunkSize { get; set; } = 1000;

	public int MinChunkSize { get; set; } = 200;

	public bool BreakAtTagBoundaries { get; set; } = true;

	public bool UseDetailedSelector { get; set; } = false;
}
