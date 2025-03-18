using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlChunking;
using Chameleon.lib.Playwright.HtmlProcessingPipeline;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.SelectorExtraction;
using Microsoft.Playwright;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;

namespace Tests.HtmlProcessingPipeline;

public class HtmlProcessingPipelineServiceTests: IAsyncLifetime {
	private HtmlProcessingPipelineService? pipelineService;
	private IBrowser? browser;
	private IPlaywright? playwright;

	public async Task InitializeAsync() {
		playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
	}

	public async Task DisposeAsync() {
		if (browser != null)
			await browser.DisposeAsync();
		playwright?.Dispose();
	}

	[Fact]
	public async Task IntegrationTest_ProcessUrlAndGenerateScriptAsync_GeneratesNonEmptyScript() {
		var fileHtmlPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Files/redditPostHtmlPage.txt");
		var fakeHtml = await File.ReadAllTextAsync(fileHtmlPath);
		IHtmlExtractor htmlExtractor = new FakeHtmlExtractor(fakeHtml);
		IHtmlChunker htmlChunker = new HtmlChunkingService();
		ISelectorExtractor selectorExtractor = new SelectorExtractionService();

		var aiOptions = new AiIntegrationOptions {
			ApiKey = "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",
			ModelName = "o3-mini",
			MaxTokens = 150
		};
		IAiIntegrationService aiIntegrationService = new AiExtensionsIntegrationService(aiOptions);

		pipelineService = new HtmlProcessingPipelineService(
				htmlExtractor,
				htmlChunker,
				selectorExtractor,
				aiIntegrationService
		);

		var extractionOptions = new ExtractionOptions { NavigationTimeout = 60000, WaitTimeout = 30000, WaitForSelector = "body" };
		var chunkingOptions = new HtmlChunkingOptions { MaxChunkSize = 1000, MinChunkSize = 200, BreakAtTagBoundaries = true, UseDetailedSelector = true };
		var selectorOptions = new SelectorExtractionOptions { IncludeTagName = true, IncludeId = true, IncludeClasses = true, IncludeAttributes = false, IncludeInnerText = true };

		var automationDescription =
				"Reddit Interaction Plugin script:\n" +
				"- Subreddit Membership Check: Joins subreddit if not already a member.\n" +
				"- Comment Extraction: Finds and logs the first comment.\n" +
				"- Reply Functionality: Clicks 'Reply,' types the provided message, and submits if publish is true.\n" +
				"- Error Handling: Uses Playwright's waiting mechanisms and structured logging.";

		var generatedScript = await pipelineService.ProcessUrlAndGenerateScriptAsync(
				"https://www.reddit.com/r/SatisfactoryGame/comments/1ihoih5/what_use_if_any_is_any_undefined/",
				automationDescription,
				extractionOptions,
				chunkingOptions,
				selectorOptions,
				aiOptions,
				CancellationToken.None
		);

		Console.WriteLine("Generated Script:");
		Console.WriteLine(generatedScript);

		Assert.False(string.IsNullOrWhiteSpace(generatedScript), "The generated script should not be empty.");
	}

	public class FakeHtmlExtractor(string fakeHtml) : IHtmlExtractor {
		public Task<string> ExtractHtmlAsync(string url, ExtractionOptions? options = null, CancellationToken cancellationToken = default) {
			return Task.FromResult(fakeHtml);
		}
	}

}
