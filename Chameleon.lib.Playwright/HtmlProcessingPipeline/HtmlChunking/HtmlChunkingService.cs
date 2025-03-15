using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;
using System.Text.RegularExpressions;

namespace Chameleon.lib.Playwright.HtmlChunking;
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

	public async Task<IList<HtmlChunk>> ChunkHtmlWithSelectorsAsync(string html, HtmlChunkingOptions options, CancellationToken cancellationToken = default) {

		var rawChunks = await ChunkHtmlAsync(html, options, cancellationToken);
	  var result = new List <HtmlChunk>();

		foreach (var chunk in rawChunks) {
			var selector = "body";
			if (options.UseDetailedSelector) {
				var detailedMatch = GetDetailedSelectorInChunckRegex().Match(chunk);
				if (detailedMatch.Success) {
					var tag = detailedMatch.Groups[1].Value;
					var id = detailedMatch.Groups[2].Value;
					var classValue = detailedMatch.Groups[3].Value;
					if (!string.IsNullOrWhiteSpace(id)) {
						selector = $"{tag}#{id}";
					} else if (!string.IsNullOrWhiteSpace(classValue)) {
						var firstClass = classValue.Split([' '], StringSplitOptions.RemoveEmptyEntries)[0];
						selector = $"{tag}.{firstClass}";
					} else {
						selector = tag;
					}
				} else {

					var match = GetSelectorInChunckRegex().Match(chunk);
					if (match.Success)
						selector = match.Groups[1].Value;
				}
			} else {

				var match = GetSelectorInChunckRegex().Match(chunk);
				if (match.Success)
					selector = match.Groups[1].Value;
			}

			result.Add(new HtmlChunk(chunk, selector));
		}

		return result;
	}

	[GeneratedRegex("<\\s*(\\w+)", RegexOptions.IgnoreCase, "en-US")]
	private static partial Regex GetSelectorInChunckRegex();
	[GeneratedRegex(@"<\s*(\w+)(?:\s+[^>]*?(?:id\s*=\s*[""']([^""']+)[""']|class\s*=\s*[""']([^""']+)[""']))", RegexOptions.IgnoreCase, "en-US")]
	private static partial Regex GetDetailedSelectorInChunckRegex();
}
