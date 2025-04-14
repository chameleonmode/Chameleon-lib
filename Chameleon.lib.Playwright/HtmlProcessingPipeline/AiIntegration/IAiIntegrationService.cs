using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
public interface IAiIntegrationService {
	Task<string> QueryLLMAsync(string prompt, AiIntegrationOptions options, CancellationToken cancellationToken = default);

	Task<string> GenerateAutomationScriptAsync(List<HtmlChildSummary> relevantNodes, string automationRequest);
}
