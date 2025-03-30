using Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
public interface IHtmlExtractor {
	Task<string> ExtractHtmlAsync(string url, ExtractionOptions? options = null, CancellationToken cancellationToken = default);

	Task<string> InitializeCrawlerContextAsync(IPage page);

	Task<List<HtmlChildSummary>> GetRelevantNodesAsync(IPage page, string rootId, string automationRequest, AiIntegrationOptions options, ExtractionOptions extractionOptions, Func<string, AiIntegrationOptions, CancellationToken, Task<string>> queryLLMAsync, CancellationToken cancellation);
}
