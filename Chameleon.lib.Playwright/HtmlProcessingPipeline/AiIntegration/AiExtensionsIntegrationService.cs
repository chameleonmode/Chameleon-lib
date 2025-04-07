using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Microsoft.Extensions.AI;
using Mscc.GenerativeAI.Microsoft;
using System.Text;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
public class AiExtensionsIntegrationService : IAiIntegrationService {
	private readonly AiIntegrationOptions options;
	private readonly IChatClient chatClient;
	public AiExtensionsIntegrationService(AiIntegrationOptions options) {
		this.options = options ?? throw new ArgumentNullException(nameof(options));

		if (string.IsNullOrWhiteSpace(options.ApiKey))
			throw new ArgumentException("API key must be provided in options.", nameof(options));

		if (string.IsNullOrWhiteSpace(options.ModelName))
			throw new ArgumentException("Model name must be provided in options.", nameof(options));


		//var openAiClient = new OpenAIClient(options.ApiKey);
		//chatClient = openAiClient.AsChatClient(options.ModelName);
		chatClient = new GeminiChatClient(options.ApiKey, options.ModelName);
	}

	public async Task<string> QueryLLMAsync(string prompt, AiIntegrationOptions options, CancellationToken cancellationToken = default) {
		var messages = new List<ChatMessage>
						{
								new(ChatRole.System, "You are a helpful assistant generating automation scripts."),
								new(ChatRole.User, prompt)
						};
		var chatOptions = new ChatOptions {
			MaxOutputTokens = options.MaxTokens
		};
		if (options.Temperature is not null) {
			chatOptions.Temperature = options.Temperature.Value;
		}

		var response = await chatClient.GetResponseAsync(messages, chatOptions, cancellationToken);

		return response.Text;
	}

	public async Task<string> GenerateAutomationScriptAsync(List<HtmlChildSummary> relevantNodes,string automationRequest) {
		var sb = new StringBuilder();
		sb.AppendLine("You are a helpful assistant that generates **valid JavaScript Playwright scripts** only.");
		sb.AppendLine("Any additional explanations or disclaimers must be wrapped in JavaScript comments so the resulting code is directly runnable with Node.js or any JavaScript environment.");
		sb.AppendLine();
		sb.AppendLine($"User wants to automate: {automationRequest}");
		sb.AppendLine();
		sb.AppendLine("Relevant DOM nodes:");
		foreach (var node in relevantNodes) {
			sb.AppendLine($"- Tag: {node.TagName}, ID: {node.Id}, Class: {node.ClassName}, Snippet: {node.ShortText}, Potential CssSelecotor:{node.CssSelector}");
		}

		sb.AppendLine();
		sb.AppendLine("Now, generate a JavaScript Playwright script that interacts with these elements.");
		sb.AppendLine("The script should be able to accept any parameters that might be relevant to this automation task,");
		sb.AppendLine("such as credentials, search terms, or configuration flags. Please handle them by using:");
		sb.AppendLine("Environment variables (e.g. `process.env.PARAM_NAME`)");
		sb.AppendLine();
		sb.AppendLine("Please ensure the script gracefully handles missing or invalid parameters.");
		sb.AppendLine("Produce a valid, runnable JavaScript Playwright code snippet that references these parameters and");
		sb.AppendLine("demonstrates as a comment how they can be used to complete the automation steps.");
		sb.AppendLine();
		sb.AppendLine("**Important**:");
		sb.AppendLine("- Put any disclaimers or usage notes in JavaScript comments at the top.");
		sb.AppendLine("- - The resulting script must be purely valid JavaScript.");
		sb.AppendLine();
		sb.AppendLine("Now produce the **full** JavaScript code.");

		var finalPrompt = sb.ToString();
		var script = await QueryLLMAsync(finalPrompt, options, CancellationToken.None);
		return script;
	}

}
