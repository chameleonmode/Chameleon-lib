namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.SelectorExtraction;
public class SelectorExtractionOptions {

	public bool IncludeTagName { get; set; } = true;

	public bool IncludeId { get; set; } = true;

	public bool IncludeClasses { get; set; } = true;

	public bool IncludeAttributes { get; set; } = false;

	public bool IncludeInnerText { get; set; } = false;
}
