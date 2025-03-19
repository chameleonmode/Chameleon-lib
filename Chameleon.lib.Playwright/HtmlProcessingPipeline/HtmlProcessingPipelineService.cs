using Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.SelectorExtraction;
using Microsoft.Extensions.AI;
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
		var chunkedHtml = await htmlChunker.ChunkHtmlAsync(html, chunkingOptions, cancellationToken);

		foreach (var chunk in chunkedHtml) {
			var selectors = await selectorExtractor.ExtractSelectorsAsync(chunk, selectorOptions, cancellationToken);
			finalSelectors.AddRange(selectors);
		}

		finalSelectors = finalSelectors
				.GroupBy(s => s.Selector)
				.Select(g => g.First())
				.ToList();

		return finalSelectors;
	}

	public async Task<string> ProcessUrlAndGenerateScriptInChunksChatAsync(
						string url,
						string automationDescription,
						ExtractionOptions extractionOptions,
						HtmlChunkingOptions chunkingOptions,
						SelectorExtractionOptions selectorOptions,
						AiIntegrationOptions aiOptions,
						int maxSelectorsPerChunk = 200,
						CancellationToken cancellationToken = default) {

		var fullHtml = await htmlExtractor.ExtractHtmlAsync(url, extractionOptions, cancellationToken);

		var chunkedHtml = await htmlChunker.ChunkHtmlAsync(fullHtml, chunkingOptions, cancellationToken);

		var conversation = new List<ChatMessage> {
			new(ChatRole.System, "You are a helpful assistant generating automation scripts using Playwright Javascript."),
			new(ChatRole.User, $"Automation Requirements:\n{automationDescription}")
		};

		var chatOptions = new ChatOptions { MaxOutputTokens = aiOptions.MaxTokens };

		foreach (var chunk in chunkedHtml) {

			var selectors = await selectorExtractor.ExtractSelectorsAsync(chunk, selectorOptions, cancellationToken);

			selectors = selectors.Where(s => !string.IsNullOrWhiteSpace(s.InnerText) && s.InnerText.Length > 3)
														.DistinctBy(d => d.InnerText)
														.DistinctBy(d => d.Selector)
													 .ToList();

			var selectorBatches = htmlChunker.ChunkSelectors(selectors, maxSelectorsPerChunk);

			foreach (var batch in selectorBatches) {
				var partialPrompt = BuildPartialPrompt(batch);
				conversation.Add(new ChatMessage(ChatRole.User, partialPrompt));
			}

			if(selectorBatches.Count > 0) {
				var partialResponse = await aiIntegrationService.GenerateScriptChatResponseAsync(conversation, chatOptions, cancellationToken);
				conversation.Add(new ChatMessage(ChatRole.Assistant, partialResponse.Text));
			}
		}

		conversation.Add(new ChatMessage(ChatRole.User,
				"Based on the conversation above, combine all the information into one complete JavaScript automation script using Playwright that fulfills the automation requirements. " +
				"Ensure the script follows best practices, including error handling and structured logging. Return only the final script code."));

		var finalResponse = await aiIntegrationService.GenerateScriptChatResponseAsync(conversation, chatOptions, cancellationToken);

		return finalResponse.Text;
	}


	private string BuildPartialPrompt(IList<SelectorInfo> selectors) {
		var sb = new StringBuilder();
		sb.AppendLine("Partial DOM Information:");
		foreach (var info in selectors) {
			sb.AppendLine($"- Selector: {info.Selector}, Tag: {info.TagName}" +
										(string.IsNullOrWhiteSpace(info.InnerText) ? "" : $", InnerText: \"{info.InnerText}\""));
		}
		sb.AppendLine("Based on this subset, provide a concise summary or a partial script section that addresses these elements in the context of the overall automation requirements. Ignore if the section is not relevant to the Automation Requirements");
		return sb.ToString();
	}

}
