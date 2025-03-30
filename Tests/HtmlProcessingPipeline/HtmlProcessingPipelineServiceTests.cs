using Chameleon.lib.Playwright.HtmlProcessingPipeline;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Microsoft.Playwright;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;

namespace Tests.HtmlProcessingPipeline;

public class HtmlProcessingPipelineServiceTests : IAsyncLifetime {
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
	public async Task BFS_IntegrationTest_GeneratesNonEmptyScript() {

		var page = await browser!.NewPageAsync();

		var fileHtmlPath = Path.Combine(
				Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
				"Files",
				"testHtmlText.txt"
		);

		var fakeHtml = await File.ReadAllTextAsync(fileHtmlPath);
		await page.SetContentAsync(fakeHtml);

		var extractorService = new HtmlExtractorService(browser);

		var aiOptions = new AiIntegrationOptions {
			ApiKey = "",
			ModelName = "",
			MaxTokens = 500
		};
		var aiService = new AiExtensionsIntegrationService(aiOptions);

		var extractOptions = new ExtractionOptions() { MaxChildDepth = 3, SnippetTextLength = 200 };
		var pipelineService = new HtmlProcessingPipelineService(extractorService, aiService);

		var automationDescription = $@"
Automation Requirements:
1. Fill in the username and password fields then click the Login.
2. Locate the Comments Section, read the first comment text.
3. Click Replyand type ""Thanks for the info!"", then submit if there's a publish option.
4. If there is any cookie banner or disclaimers, accept or close them.
5. Ignore any advertisement or analytics scripts.
6. The final script must be in JavaScript using Playwright, with error handling and structured logging where possible.
";


		var generatedScript = await pipelineService.ProcessPageAsync(
				page,
				automationDescription,
				aiOptions,
				extractOptions,
				CancellationToken.None
		);

		Console.WriteLine("Generated Script:");
		Console.WriteLine(generatedScript);

		Assert.False(string.IsNullOrWhiteSpace(generatedScript),"The generated script should not be empty.");
	}

}
