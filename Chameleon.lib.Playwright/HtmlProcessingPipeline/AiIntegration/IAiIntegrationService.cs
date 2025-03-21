using Microsoft.Extensions.AI;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
public interface IAiIntegrationService {
	Task<string> GenerateScriptAsync(string prompt, AiIntegrationOptions options, CancellationToken cancellationToken = default);
	Task<ChatResponse> GenerateScriptChatResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default);
	Task<IDictionary<string, Embedding<float>>> GenerateEmbeddingsForChunksAsync(IEnumerable<string> chunks, CancellationToken cancellationToken = default);
	Task<IList<string>> RetrieveRelevantChunksAsync(string queryText, IDictionary<string, Embedding<float>> chunkEmbeddings, int topN = 5, CancellationToken cancellationToken = default);
}
