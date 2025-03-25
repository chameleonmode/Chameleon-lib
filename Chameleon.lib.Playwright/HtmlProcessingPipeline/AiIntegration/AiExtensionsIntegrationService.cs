using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Mscc.GenerativeAI.Microsoft;
using OpenAI;
using System.Text;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
public class AiExtensionsIntegrationService : IAiIntegrationService {
	private readonly AiIntegrationOptions options;
	private readonly IChatClient chatClient;
	private readonly IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator;
	public AiExtensionsIntegrationService(AiIntegrationOptions options) {
		this.options = options ?? throw new ArgumentNullException(nameof(options));

		if (string.IsNullOrWhiteSpace(options.ApiKey))
			throw new ArgumentException("API key must be provided in options.", nameof(options));

		if (string.IsNullOrWhiteSpace(options.ModelName))
			throw new ArgumentException("Model name must be provided in options.", nameof(options));


		//var openAiClient = new OpenAIClient(options.ApiKey);
		//chatClient = openAiClient.AsChatClient(options.ModelName);
		chatClient = new GeminiChatClient(options.ApiKey, options.ModelName);

		//var openAiClient = new OpenAIClient(this.options.ApiKey);
		//embeddingGenerator = openAiClient.AsEmbeddingGenerator("text-embedding-3-small");
		embeddingGenerator = new GeminiEmbeddingGenerator(options.ApiKey, "embedding-001");
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

	public async Task<IDictionary<string, Embedding<float>>> GenerateEmbeddingsForChunksAsync(IEnumerable<string> chunks, CancellationToken cancellationToken = default) {

		var cacheOptions = Options.Create(new MemoryDistributedCacheOptions());
		var cache = new MemoryDistributedCache(cacheOptions);

		var generator = embeddingGenerator
				.AsBuilder()
				.UseDistributedCache(cache)
				.Build();

		var embeddings = new Dictionary<string, Embedding<float>>();

		foreach (var chunk in chunks) {
			var embedding = await generator.GenerateEmbeddingAsync(chunk, null, cancellationToken);
			embeddings[chunk] = embedding;
		}

		return embeddings;
	}

	private static float CosineSimilarity(ReadOnlyMemory<float> vectorA, ReadOnlyMemory<float> vectorB) {
		if (vectorA.Length != vectorB.Length)
			throw new ArgumentException("Vectors must be the same length.");

		float dot = 0;
		float magA = 0;
		float magB = 0;
		for (var i = 0; i < vectorA.Length; i++) {
			dot += vectorA.Span[i] * vectorB.Span[i];
			magA += vectorA.Span[i] * vectorA.Span[i];
			magB += vectorB.Span[i] * vectorB.Span[i];
		}
		if (magA == 0 || magB == 0)
			return 0;
		return dot / (float)(Math.Sqrt(magA) * Math.Sqrt(magB));
	}

	public async Task<IList<string>> RetrieveRelevantChunksAsync(string queryText, IDictionary<string, Embedding<float>> chunkEmbeddings,int topN = 5, CancellationToken cancellationToken = default) {
		var cacheOptions = Options.Create(new MemoryDistributedCacheOptions());
		IDistributedCache cache = new MemoryDistributedCache(cacheOptions);

		var generator = embeddingGenerator
				.AsBuilder()
				.UseDistributedCache(cache)
				.Build();

		var queryEmbedding = await generator.GenerateEmbeddingAsync(queryText, null, cancellationToken);

		var scoredChunks = chunkEmbeddings.Select(kvp => new {
			Chunk = kvp.Key,
			Similarity = CosineSimilarity(queryEmbedding.Vector, kvp.Value.Vector)
		});

		var topChunks = scoredChunks.OrderByDescending(x => x.Similarity)
																	.Take(topN)
																	.Select(x => x.Chunk)
																	.ToList();

		return topChunks;
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
		sb.AppendLine($"User wants to automate: {automationRequest}");
		sb.AppendLine();
		sb.AppendLine("Relevant DOM nodes:");
		foreach (var node in relevantNodes) {
			sb.AppendLine($"- Tag: {node.TagName}, ID: {node.Id}, Class: {node.ClassName}, Snippet: {node.ShortText}, Potential CssSelecotor:{node.CssSelector}");
		}

		sb.AppendLine();
		sb.AppendLine("Now, generate a JavaScript Playwright script that interacts with these elements...");

		var finalPrompt = sb.ToString();
		var script = await QueryLLMAsync(finalPrompt, options, CancellationToken.None);
		return script;
	}

}
