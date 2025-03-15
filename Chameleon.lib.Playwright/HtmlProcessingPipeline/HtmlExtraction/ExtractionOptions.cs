namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
public class ExtractionOptions {
	public int NavigationTimeout { get; set; } = 60000;

	public int WaitTimeout { get; set; } = 30000;

	public string WaitForSelector { get; set; } = "body";
}
