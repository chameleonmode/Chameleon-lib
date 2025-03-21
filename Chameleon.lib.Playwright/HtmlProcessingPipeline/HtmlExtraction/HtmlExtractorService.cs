using Microsoft.Playwright;

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

	public void Dispose() {
		browser?.DisposeAsync().GetAwaiter().GetResult();
	}
}
