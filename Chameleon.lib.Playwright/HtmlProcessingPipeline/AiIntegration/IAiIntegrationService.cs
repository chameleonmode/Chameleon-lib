using Microsoft.Extensions.AI;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
public interface IAiIntegrationService {
	Task<string> GenerateScriptAsync(string prompt, AiIntegrationOptions options, CancellationToken cancellationToken = default);
	Task<ChatResponse> GenerateScriptChatResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default);
}
