namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.SelectorExtraction;
public class SelectorInfo {

	public string Selector { get; set; } = null!;

	public string TagName { get; set; } = null!;

	public string? Id { get; set; }

	public IList<string> Classes { get; set; } = [];

	public IDictionary<string, string>? Attributes { get; set; }

	public string? InnerText { get; set; }
}
