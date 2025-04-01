using Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline;
public class HtmlProcessingPipelineService(
		IHtmlExtractor htmlExtractor,
		IAiIntegrationService aiIntegrationService) {
	private readonly IHtmlExtractor htmlExtractor = htmlExtractor ?? throw new ArgumentNullException(nameof(htmlExtractor));
	private readonly IAiIntegrationService aiIntegrationService = aiIntegrationService ?? throw new ArgumentNullException(nameof(aiIntegrationService));

	public async Task<string> ProcessPageAsync(IPage page, string automationRequest, AiIntegrationOptions aiOptions, ExtractionOptions extractionOptions, CancellationToken cancellationToken = default) {

		var rootId = await htmlExtractor.InitializeCrawlerContextAsync(page);

		var relevantNodes = await htmlExtractor.GetRelevantNodesAsync(page,rootId,automationRequest,aiOptions,
				extractionOptions,
				aiIntegrationService.QueryLLMAsync,
				cancellationToken
		);

		var finalScript = await aiIntegrationService.GenerateAutomationScriptAsync(relevantNodes,automationRequest);

		return finalScript;
	}

	public async Task<string> ProcessingPageAsync(IPage page, string automationRequest, AiIntegrationOptions aiOptions, ExtractionOptions extractionOptions, CancellationToken cancellationToken = default) {

		var nodes = await htmlExtractor.GetAllNodesAsync(page, extractionOptions.MaxChildDepth, extractionOptions.SnippetTextLength);

		var relevantNodes = await htmlExtractor.GetRelevantNodesAsync(nodes, automationRequest, aiOptions,
				extractionOptions, aiIntegrationService.QueryLLMAsync,cancellationToken);

		var finalScript = await aiIntegrationService.GenerateAutomationScriptAsync(relevantNodes, automationRequest);

		return finalScript;
	}
}
