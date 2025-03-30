using Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
using Microsoft.Playwright;
using Newtonsoft.Json;
using System.Text.Json;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
public class HtmlExtractorService(IBrowser browser) : IHtmlExtractor, IDisposable {

	private readonly IBrowser browser = browser ?? throw new ArgumentNullException(nameof(browser));

	public async Task<string> ExtractHtmlAsync(string url, ExtractionOptions? options = null, CancellationToken cancellationToken = default) {
		options ??= new ExtractionOptions();

		var page = await browser.NewPageAsync();

		page.SetDefaultNavigationTimeout(options.NavigationTimeout);
		page.SetDefaultTimeout(options.WaitTimeout);

		_ = await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

		if (!string.IsNullOrEmpty(options.WaitForSelector)) _ = await page.WaitForSelectorAsync(options.WaitForSelector, new PageWaitForSelectorOptions { Timeout = options.WaitTimeout });

		var html = await page.EvaluateAsync<string>(@"() => {
        function getShadowHTML(el) {
            let shadow = el.shadowRoot;
            if (!shadow) return '';
            let html = '';
            shadow.childNodes.forEach(child => {
                html += getFullHTML(child);
            });
            return html;
        }
        function getFullHTML(node) {
            if (node.nodeType === Node.TEXT_NODE)
                return node.textContent;
            let html = node.outerHTML || '';
            if (node.shadowRoot) {
                // Insert shadow content before closing tag.
                let shadowHTML = getShadowHTML(node);
                html = html.replace(/(<\/[^>]+>)$/, shadowHTML + '$1');
            }
            return html;
        }
        return getFullHTML(document.documentElement);
    }");
		return html;
	}

	public async Task<string> InitializeCrawlerContextAsync(IPage page) {

		const string crawlerScript = @"
(() => {
    window.__crawler = {
        nodeCounter: 0,
        nodeMap: new Map(),

        registerNode(node) {
            const id = 'node-' + (this.nodeCounter++);
            this.nodeMap.set(id, node);
            return id;
        },

        getNode(nodeId) {
            return this.nodeMap.get(nodeId);
        },

        buildCssSelector(node) {
            if (!node || !node.tagName) {
                return '';
            }

            if (node.id) {
                return `#${node.id}`;
            }

            let selector = node.tagName.toLowerCase();

            if (node.classList && node.classList.length > 0) {
                selector += '.' + Array.from(node.classList).join('.');
            }

            const parent = node.parentElement;
            if (parent) {
                const sameTagSiblings = Array.from(parent.children)
                    .filter(el => el.tagName === node.tagName);
                const index = sameTagSiblings.indexOf(node);

                if (sameTagSiblings.length > 1) {
                    selector += `:nth-of-type(${index + 1})`;
                }

                const parentSelector = this.buildCssSelector(parent);
                if (parentSelector && parentSelector !== 'html' && parentSelector !== 'body') {
                    return parentSelector + ' > ' + selector;
                }
            }

            return selector;
        },

       getImmediateChildren(nodeId, snippetTextLength = 200) {
  const node = this.getNode(nodeId);
  if (!node) return [];

  const results = [];
  const childElems = Array.from(node.children) || [];

  if (node.shadowRoot) {
    childElems.push(...Array.from(node.shadowRoot.children));
  }

  for (const child of childElems) {
    const newId = window.__crawler.registerNode(child);
    const text = child.innerText || '';
    const shortText = text.length > snippetTextLength
      ? text.substring(0, snippetTextLength) + '...'
      : text;

    results.push({
      nodeId: newId,
      tagName: child.tagName.toLowerCase(),
      id: child.id || null,
      className: child.className || null,
      shortText,
      cssSelector: window.__crawler.buildCssSelector(child)
    });
  }

  return results;
}
    };

    const rootNode = document.body;
    window.__crawler.rootId = window.__crawler.registerNode(rootNode);
})();
";

		_ = await page.EvaluateAsync(crawlerScript);

		var rootId = await page.EvaluateAsync<string>("() => window.__crawler.rootId");

		return rootId;
	}

	public async Task<List<HtmlChildSummary>> GetRelevantNodesAsync(IPage page, string rootId, string automationRequest, AiIntegrationOptions options, ExtractionOptions extractionOptions, Func<string, AiIntegrationOptions, CancellationToken, Task<string>> queryLLMAsync, CancellationToken cancellation) {
		var visited = new HashSet<string>();
		var queue = new Queue<string>();
		var keptNodes = new List<HtmlChildSummary>();
		var relevantNodes = new List<HtmlChildSummary>();

		queue.Enqueue(rootId);

		while (queue.Count > 0) {
			var currentNodeId = queue.Dequeue();
			if (!visited.Add(currentNodeId)) {
				continue;
			}

			var childSummaries = await GetChildSummariesAsync(page, currentNodeId, extractionOptions.MaxChildDepth, extractionOptions.SnippetTextLength, cancellation);
			keptNodes.AddRange(childSummaries);

			foreach (var child in childSummaries) {
				if (child.NodeId is not null) {

					if (IsObviouslyIrrelevant(child)) continue;

					var isRelevant = await IsNodeRelevantAsync(page, child, automationRequest, options, extractionOptions, queryLLMAsync, cancellation);
					if (isRelevant) {
						relevantNodes.Add(child);
						queue.Enqueue(child.NodeId);
					}
				}
			}
		}

		return keptNodes;
	}

	private static bool IsObviouslyIrrelevant(HtmlChildSummary c) {
		return c.TagName is "script" or "style" || (string.IsNullOrWhiteSpace(c.ShortText) && string.IsNullOrWhiteSpace(c.Id));
	}


	private static async Task<List<HtmlChildSummary>> GetChildSummariesAsync(IPage page, string nodeId, int maxDepth, int snippetTextLength, CancellationToken cancellationToken = default) {
		var jsResult = await page.EvaluateAsync<JsonElement>($@"() => window.__crawler.getImmediateChildren('{nodeId}',{snippetTextLength})", cancellationToken);
		var rawText = jsResult.GetRawText();
		var childSummaries = JsonConvert.DeserializeObject<List<HtmlChildSummary>>(rawText);
		return childSummaries ?? [];
	}

	private static async Task<bool> IsNodeRelevantAsync(
		IPage page, HtmlChildSummary summary, string automationRequest, AiIntegrationOptions options, ExtractionOptions extractionOptions,
		Func<string, AiIntegrationOptions, CancellationToken, Task<string>> queryLLMAsync, CancellationToken cancellationToken = default) {

		var nodeInfo =
			$@"Tag: {summary.TagName}
				 ID: {summary.Id}
				 Class: {summary.ClassName}
				 TextSnippet: {summary.ShortText}";

		if (summary.NodeId is null)
			return false;

		var childSummaries = await GetChildSummariesAsync(page, summary.NodeId, extractionOptions.MaxChildDepth, extractionOptions.SnippetTextLength, cancellationToken);

		var sampleChildren = childSummaries.Take(2).Select(c => $"  - {c.TagName}, text: {c.ShortText}").ToList();

		var childInfoString = string.Join("\n", sampleChildren);
		if (!string.IsNullOrWhiteSpace(childInfoString)) {
			nodeInfo += $"\nChild sample:\n{childInfoString}";
		}

		var prompt = $@"
We are building an automation script in **JavaScript** using **Playwright**. 
Our overall tasks are:
{automationRequest}

Right now, we want to decide if the following DOM node (plus up to 2 of its child elements)
is relevant for these automation tasks.

Node info:
{nodeInfo}

If you think this node or its children might be needed for the tasks in our JavaScript Playwright script, 
please answer 'YES'. If you are fairly certain the node has no relation, answer 'NO'. 
If you are unsure, answer 'YES' so we can explore more deeply.

Answer ONLY with 'YES' or 'NO'.
";


		var response = await queryLLMAsync(prompt, options, cancellationToken);

		var trimmed = response.Trim().ToUpperInvariant();
		return !trimmed.Contains("NO");
	}


	public void Dispose() {
		browser?.DisposeAsync().GetAwaiter().GetResult();
	}
}
