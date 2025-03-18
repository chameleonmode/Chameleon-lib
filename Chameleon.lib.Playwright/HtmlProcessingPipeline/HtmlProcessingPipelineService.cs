using Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.SelectorExtraction;
using System.Text;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline;
public class HtmlProcessingPipelineService(
		IHtmlExtractor htmlExtractor,
		IHtmlChunker htmlChunker,
		ISelectorExtractor selectorExtractor,
		IAiIntegrationService aiIntegrationService) {
	private readonly IHtmlExtractor htmlExtractor = htmlExtractor ?? throw new ArgumentNullException(nameof(htmlExtractor));
	private readonly IHtmlChunker htmlChunker = htmlChunker ?? throw new ArgumentNullException(nameof(htmlChunker));
	private readonly ISelectorExtractor selectorExtractor = selectorExtractor ?? throw new ArgumentNullException(nameof(selectorExtractor));
	private readonly IAiIntegrationService aiIntegrationService = aiIntegrationService ?? throw new ArgumentNullException(nameof(aiIntegrationService));

	public async Task<IList<SelectorInfo>> ProcessUrlAsync(
			string url,
			ExtractionOptions extractionOptions,
			HtmlChunkingOptions chunkingOptions,
			SelectorExtractionOptions selectorOptions,
			CancellationToken cancellationToken = default) {
		var html = await htmlExtractor.ExtractHtmlAsync(url, extractionOptions, cancellationToken);
		return await ProcessHtmlAsync(html, chunkingOptions, selectorOptions, cancellationToken);
	}

	public async Task<IList<SelectorInfo>> ProcessHtmlAsync(
			string html,
			HtmlChunkingOptions chunkingOptions,
			SelectorExtractionOptions selectorOptions,
			CancellationToken cancellationToken = default) {
		var finalSelectors = new List<SelectorInfo>();
		var chunkedHtmlWithSelectors = await htmlChunker.ChunkHtmlWithSelectorsAsync(html, chunkingOptions, cancellationToken);

		foreach (var chunk in chunkedHtmlWithSelectors) {
			var selectors = await selectorExtractor.ExtractSelectorsAsync(chunk.Chunk, selectorOptions, cancellationToken);
			finalSelectors.AddRange(selectors);
		}

		finalSelectors = finalSelectors
				.GroupBy(s => s.Selector)
				.Select(g => g.First())
				.ToList();

		return finalSelectors;
	}

	public async Task<string> ProcessUrlAndGenerateScriptAsync(
			string url,
			string automationDescription,
			ExtractionOptions extractionOptions,
			HtmlChunkingOptions chunkingOptions,
			SelectorExtractionOptions selectorOptions,
			AiIntegrationOptions aiOptions,
			CancellationToken cancellationToken = default) {

		var selectors = await ProcessUrlAsync(url, extractionOptions, chunkingOptions, selectorOptions, cancellationToken);

		var promptBuilder = new StringBuilder();
		promptBuilder.AppendLine("Automation Script Requirements:");
		promptBuilder.AppendLine(automationDescription);
		promptBuilder.AppendLine();
		promptBuilder.AppendLine("Extracted DOM Information:");
		foreach (var info in selectors) {
			promptBuilder.AppendLine($"- Selector: {info.Selector} (Tag: {info.TagName})" +
					(string.IsNullOrWhiteSpace(info.InnerText) ? "" : $", InnerText: \"{info.InnerText}\""));
		}
		promptBuilder.AppendLine();
		promptBuilder.AppendLine("Based on the above information, generate a complete JavaScript automation script using Playwright. " +
															"The script should fulfill the specified requirements and follow best practices for automation, " +
															"including error handling and structured logging.");

		var prompt = promptBuilder.ToString();

		var generatedScript = await aiIntegrationService.GenerateScriptAsync(prompt, aiOptions, cancellationToken);
		return generatedScript;
	}
}
