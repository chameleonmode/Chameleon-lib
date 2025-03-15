namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.SelectorExtraction;
public interface ISelectorExtractor {
	Task<IList<SelectorInfo>> ExtractSelectorsAsync(string html, SelectorExtractionOptions options, CancellationToken cancellationToken = default);
}
