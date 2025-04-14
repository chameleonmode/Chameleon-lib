using Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.Extensions;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.Models;
using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline;
public class HtmlProcessingPipelineService(
		IHtmlExtractor htmlExtractor,
		IAiIntegrationService aiIntegrationService) {
	private readonly IHtmlExtractor htmlExtractor = htmlExtractor ?? throw new ArgumentNullException(nameof(htmlExtractor));
	private readonly IAiIntegrationService aiIntegrationService = aiIntegrationService ?? throw new ArgumentNullException(nameof(aiIntegrationService));

	public async Task<string> ProcessPageAsync(IPage page, string automationRequest, AiIntegrationOptions aiOptions, ExtractionOptions extractionOptions, CancellationToken cancellationToken = default) {

		var rootId = await htmlExtractor.InitializeCrawlerContextAsync(page);

		var relevantNodes = await htmlExtractor.GetRelevantNodesAsync(page, rootId, automationRequest, aiOptions,
				extractionOptions,
				aiIntegrationService.QueryLLMAsync,
				cancellationToken
		);

		var finalScript = await aiIntegrationService.GenerateAutomationScriptAsync(relevantNodes, automationRequest);

		return finalScript;
	}

	public async Task<string> ProcessingPageAsync(IPage page, string automationRequest, AiIntegrationOptions aiOptions, ExtractionOptions extractionOptions, CancellationToken cancellationToken = default) {

		var nodes = await htmlExtractor.GetAllNodesAsync(page, extractionOptions.MaxChildDepth, extractionOptions.SnippetTextLength);

		var relevantNodes = await htmlExtractor.GetRelevantNodesAsync(nodes, automationRequest, aiOptions,
				extractionOptions, aiIntegrationService.QueryLLMAsync, cancellationToken);

		var finalScript = await aiIntegrationService.GenerateAutomationScriptAsync(relevantNodes, automationRequest);

		return finalScript;
	}

	public async Task<string> ProcessMultiStepAsync(IPage page, List<StepDefinition> steps, AiIntegrationOptions aiOptions,
		ExtractionOptions extractionOptions, CancellationToken cancellationToken = default) {

		var allRelevantNodes = new List<HtmlChildSummary>();

		for (var i = 0; i < steps.Count; i++) {
			var step = steps[i];

			var currentNodes = await htmlExtractor.GetAllNodesAsync(page,extractionOptions.MaxChildDepth,extractionOptions.SnippetTextLength);

			var relevantNodes = await htmlExtractor.GetRelevantNodesAsync(
					currentNodes,
					step.Description,
					aiOptions,
					extractionOptions,
					aiIntegrationService.QueryLLMAsync,
					cancellationToken
			);

			allRelevantNodes.AddRange(relevantNodes);

			if (step.AutoPerformAction) {
				var partialScript = await aiIntegrationService.GenerateAutomationScriptAsync(
						relevantNodes,
						step.Description
				);

				await page.PerformPartialScriptActions(partialScript);
			} else {
				// Possibly do a known or manual action, e.g.:
				// await page.ClickAsync("#signInButton");
				// await page.WaitForLoadStateAsync();
				//Or thinking about reusing an existing known script
			}
		}

		var finalMultiStepDescription = steps.BuildMultiStepDescription();

		var finalScript = await aiIntegrationService.GenerateAutomationScriptAsync(
				allRelevantNodes,
				finalMultiStepDescription
		);

		return finalScript;
	}

}
