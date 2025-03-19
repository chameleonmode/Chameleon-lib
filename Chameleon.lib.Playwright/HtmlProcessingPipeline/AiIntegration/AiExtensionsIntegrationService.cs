using Microsoft.Extensions.AI;
using Mscc.GenerativeAI.Microsoft;
using OpenAI;

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

	public async Task<string> GenerateScriptAsync(string prompt, AiIntegrationOptions options, CancellationToken cancellationToken = default) {
		var messages = new List<ChatMessage>
						{
								new ChatMessage(ChatRole.System, "You are a helpful assistant generating automation scripts."),
								new ChatMessage(ChatRole.User, prompt)
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

	public async Task<ChatResponse> GenerateScriptChatResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(messages);
		return await chatClient.GetResponseAsync(messages, options, cancellationToken);
	}
}
