using Chameleon.lib.Playwright.HtmlProcessingPipeline.SelectorExtraction;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;
public partial class HtmlChunkingService : IHtmlChunker {

	public async Task<IList<string>> ChunkHtmlAsync(string html, HtmlChunkingOptions options, CancellationToken cancellationToken = default) {
		return await Task.Run(() => {
			var chunks = new List<string>();
			if (string.IsNullOrWhiteSpace(html))
				return chunks;

			var maxChunkSize = options.MaxChunkSize;
			var currentIndex = 0;
			var length = html.Length;

			while (currentIndex < length) {
				var remaining = length - currentIndex;
				var chunkSize = Math.Min(maxChunkSize, remaining);
				var chunk = html.Substring(currentIndex, chunkSize);

				if (options.BreakAtTagBoundaries && remaining > maxChunkSize) {
					var breakIndex = chunk.LastIndexOf("</", StringComparison.OrdinalIgnoreCase);
					if (breakIndex > options.MinChunkSize) {
						chunk = html.Substring(currentIndex, breakIndex + 1);
						currentIndex += breakIndex + 1;
					} else {
						currentIndex += chunkSize;
					}
				} else {
					currentIndex += chunkSize;
				}

				chunk = chunk.Trim();
				if (!string.IsNullOrWhiteSpace(chunk))
					chunks.Add(chunk);
			}
			return chunks;
		}, cancellationToken);
	}

	public IList<IList<SelectorInfo>> ChunkSelectors(IList<SelectorInfo> selectors, int maxSelectorsPerChunk) {
		var result = new List<IList<SelectorInfo>>();
		for (var i = 0; i < selectors.Count; i += maxSelectorsPerChunk) {
			var chunk = selectors.Skip(i).Take(maxSelectorsPerChunk).ToList();
			result.Add(chunk);
		}
		return result;
	}
}
