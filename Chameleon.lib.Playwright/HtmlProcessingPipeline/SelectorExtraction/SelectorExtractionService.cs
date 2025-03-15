using HtmlAgilityPack;
using System.Xml;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.SelectorExtraction;
public class SelectorExtractionService: ISelectorExtractor {
	public async Task<IList<SelectorInfo>> ExtractSelectorsAsync(string html, SelectorExtractionOptions options, CancellationToken cancellationToken = default) {
		var doc = new HtmlDocument();
		doc.LoadHtml(html);

		var result = new List<SelectorInfo>();

		var nodes = doc.DocumentNode.SelectNodes("//*");
		if (nodes != null) {
			foreach (var node in nodes) {
				if (node.NodeType != HtmlNodeType.Element)
					continue;

				var selector = node.Name;
				string? id = null;
				var classes = new List<string>();

				if (options.IncludeId && node.Attributes["id"] != null) {
					id = node.Attributes["id"].Value;
					selector += $"#{id}";
				}

				if (options.IncludeClasses && node.Attributes["class"] != null) {
					classes = node.Attributes["class"].Value
							.Split(' ')
							.Where(c => !string.IsNullOrWhiteSpace(c))
							.ToList();
					if (classes.Count != 0) {
						selector += "." + string.Join(".", classes);
					}
				}

				var info = new SelectorInfo {
					Selector = selector,
					TagName = node.Name,
					Id = id,
					Classes = classes
				};

				if (options.IncludeAttributes) {
					info.Attributes = node.Attributes
							.Where(a => a.Name != "id" && a.Name != "class")
							.ToDictionary(a => a.Name, a => a.Value);
				}

				if (options.IncludeInnerText) {
					info.InnerText = node.InnerText.Trim();
				}

				result.Add(info);
			}
		}

		return await Task.FromResult(result);
	}
}
