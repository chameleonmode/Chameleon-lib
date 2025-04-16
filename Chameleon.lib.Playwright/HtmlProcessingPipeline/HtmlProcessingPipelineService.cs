using Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.Extensions;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.Models;
using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline;
public class HtmlProcessingPipelineService(
		HtmlExtractorService htmlExtractor, AiExtensionsIntegrationService aiIntegrationService
) {
	// The CancellationToken parameter defaults to CancellationToken.None if not provided.
	public record struct HtmlProcessingParameters(
		IPage Page, ExtractionOptions ExtractionOptions, params StepDefinition[] Steps
	);

	/// <summary>
	/// Processes a web page asynchronously based on the provided parameters.
	/// </summary>
	/// <param name="parameters">The parameters containing the page, steps, extraction options, and cancellation token.</param>
	/// <returns>An array of strings representing the generated automation scripts for each step.</returns>
	public async Task<string[]> ProcessPageAsync(HtmlProcessingParameters parameters, CancellationToken cancellationToken = default) {
		var rootId = await htmlExtractor.InitializeCrawlerContextAsync(parameters.Page);

		// TODO: ? var semaphore = new SemaphoreSlim(5); // Limit to 5 concurrent tasks
		var tasks = parameters.Steps.Select(step => Task.Run(async () => {
			var relevantNodes = await htmlExtractor.GetRelevantNodesAsync(
				parameters.Page,
				rootId,
				step.Description,
				aiIntegrationService.Options,
				parameters.ExtractionOptions,
				aiIntegrationService.QueryLLMAsync,
				cancellationToken
			);

			return await aiIntegrationService.GenerateAutomationScriptAsync(relevantNodes, step.Description);
		}));

		return await Task.WhenAll(tasks);
	}

	public async Task<string> ProcessingPageAsync(HtmlProcessingParameters parameters, CancellationToken cancellationToken = default) {
		var nodes = await htmlExtractor.GetAllNodesAsync(parameters.Page, parameters.ExtractionOptions);

		if (parameters.Steps.Length == 1) {
			var relevantNodes = await htmlExtractor.GetRelevantNodesAsync(
				nodes,
				parameters.Steps[0].Description,
				aiIntegrationService.Options,
				parameters.ExtractionOptions,
				aiIntegrationService.QueryLLMAsync,
				cancellationToken
			);
			return await aiIntegrationService.GenerateAutomationScriptAsync(relevantNodes, parameters.Steps[0].Description);
		} else {
			var tasks = parameters.Steps.Select(async step => {
				var relevantNodes = await htmlExtractor.GetRelevantNodesAsync(
					nodes,
					step.Description,
					aiIntegrationService.Options,
					parameters.ExtractionOptions,
					aiIntegrationService.QueryLLMAsync,
					cancellationToken
				);
				if (step.AutoPerformAction) {
					var partialScript = await aiIntegrationService.GenerateAutomationScriptAsync(
							relevantNodes,
							step.Description
					);
					await parameters.Page.PerformPartialScriptActions(partialScript);
				} else {
					// Possibly do a known or manual action, e.g.:
					// await page.ClickAsync("#signInButton");
					// await page.WaitForLoadStateAsync();
					//Or thinking about reusing an existing known script
				}
				return relevantNodes;
			});

			var finalMultiStepDescription = parameters.Steps.BuildMultiStepDescription();
			var relevantNodes = await Task.WhenAll(tasks);

			return await aiIntegrationService.GenerateAutomationScriptAsync(relevantNodes.SelectMany(list => list), finalMultiStepDescription);
		}
	}
}
