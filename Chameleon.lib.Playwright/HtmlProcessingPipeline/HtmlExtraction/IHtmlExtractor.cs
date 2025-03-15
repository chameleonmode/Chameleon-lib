namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
public interface IHtmlExtractor {
	Task<string> ExtractHtmlAsync(string url, ExtractionOptions? options = null, CancellationToken cancellationToken = default);
}
