using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
public interface IAiIntegrationService {
	AiIntegrationOptions Options { get; }
	Task<string> QueryLLMAsync(string prompt, AiIntegrationOptions options, CancellationToken cancellationToken = default);

	Task<string> GenerateAutomationScriptAsync(IEnumerable<HtmlChildSummary> relevantNodes, string automationRequest);
}
