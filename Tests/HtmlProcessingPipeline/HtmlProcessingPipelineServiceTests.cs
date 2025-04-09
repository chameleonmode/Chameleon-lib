using Chameleon.lib.Playwright.HtmlProcessingPipeline;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Microsoft.Playwright;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
using Xunit.Abstractions;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Services;
using Chameleon.lib.Playwright.Services;
using static Chameleon.lib.Common.Constants.Enums;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Utils;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using System.Text.RegularExpressions;

namespace Tests.HtmlProcessingPipeline;

public partial class HtmlProcessingPipelineServiceTests : TestSetup, IAsyncLifetime {
	private IBrowser? browser;
	private IPlaywright? playwright;
	private ITestOutputHelper testOutput;
	readonly BundledScriptsService repo;
	readonly SystemBrowserService browserService;

	public HtmlProcessingPipelineServiceTests(ITestOutputHelper testOutput) {
		repo = BundledScriptsService.Instance;
		browserService = SystemBrowserService.Instance;
		this.testOutput = testOutput;
	}

	public async Task InitializeAsync() {
		playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
	}

	public async Task DisposeAsync() {
		if (browser != null)
			await browser.DisposeAsync();
		playwright?.Dispose();
	}

	[Theory]
	[InlineData("testHtmlText.txt",
@"Automation Requirements:
1. Fill in the username and password fields then click Login.
2. Locate the Comments Section, read the first comment text.
3. Click 'Reply' and type 'Thanks for the info!', then submit if there's a publish option.
4. If there is any cookie banner, accept or close it.
5. Ignore any advertisement or analytics scripts.
6. Final script must be in JavaScript using Playwright, with error handling and structured logging.")]
	[InlineData("testHtmlText.txt",
@"Automation Requirements:
1. Accept disclaimers or cookies if they appear.
2. Login with 'user' / 'pass', wait for confirmation.
3. Upvote the top post, add a new comment 'Nice build!' if relevant.
4. Skip all sponsor ads.
5. Must be in JavaScript, with robust logging and error handling.")]
	public async Task BFS_IntegrationTest_GeneratesNonEmptyScript(string fileName, string automationDescription) {

		var page = await browser!.NewPageAsync();

#pragma warning disable CS8604 // Possible null reference argument.
		var fileHtmlPath = Path.Combine(
				Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
				"Files",
				fileName
		);
#pragma warning restore CS8604 // Possible null reference argument.

		var fakeHtml = await File.ReadAllTextAsync(fileHtmlPath);
		await page.SetContentAsync(fakeHtml);

		var extractorService = new HtmlExtractorService(browser);

		var aiOptions = new AiIntegrationOptions {
			ApiKey = "AIzaSyD7THGyxSb5qE60bKmFqdgGr8JTN0xY904",
			ModelName = "gemini-2.0-flash",
			MaxTokens = 500
		};
		var aiService = new AiExtensionsIntegrationService(aiOptions);

		var extractOptions = new ExtractionOptions() { MaxChildDepth = 3, SnippetTextLength = 200 };
		var pipelineService = new HtmlProcessingPipelineService(extractorService, aiService);

		var generatedScript = await pipelineService.ProcessPageAsync(
				page,
				automationDescription,
				aiOptions,
				extractOptions,
				CancellationToken.None
		);

		testOutput.WriteLine("=======================================");
		testOutput.WriteLine($"Testing File: {fileName}");
		testOutput.WriteLine($"Automation Description:\n{automationDescription}");
		testOutput.WriteLine("----- Generated Script -----");
		testOutput.WriteLine(generatedScript);
		testOutput.WriteLine("=======================================\n");

		Assert.False(string.IsNullOrWhiteSpace(generatedScript), "The generated script should not be empty.");
	}

	[Fact]
	public async Task BFS_IntegrationTest_GenerateLoginScriptForFacebook() {
		var page = await browser!.NewPageAsync();
		_ = await page.GotoAsync("https://www.facebook.com", new PageGotoOptions {
			WaitUntil = WaitUntilState.NetworkIdle
		});

		var aiOptions = new AiIntegrationOptions {
			ApiKey = "AIzaSyD7THGyxSb5qE60bKmFqdgGr8JTN0xY904",
			ModelName = "gemini-2.0-flash",
			MaxTokens = 1000
		};
		var aiService = new AiExtensionsIntegrationService(aiOptions);



		var extractOptions = new ExtractionOptions() { MaxChildDepth = 20, SnippetTextLength = 400 };
		var extractorService = new HtmlExtractorService(browser);
		var pipelineService = new HtmlProcessingPipelineService(extractorService, aiService);
		var automationDescription = @"Facebook Signin Automation objectives:
				Fill in the username and password fields, then click the login button to sign in.
				If any disclaimers or cookie banners appear, accept or dismiss them(make sure to use a selector that is dynamic and independent of language or use a more generic selector like div[aria-label*=""all cookies""] that can match both english by default and make sure the selector selects div with role=button)
				For the login button use a different css selectors than id or class
				Produce a JavaScript Playwright script, with error handling and structured logging at each step.
				Only use import instead of require";
		var generatedScript = await pipelineService.ProcessingPageAsync(
				page,
				automationDescription,
				aiOptions,
				extractOptions,
				CancellationToken.None
		);

		generatedScript = ExtractCodeBlock(generatedScript);

		testOutput.WriteLine("=======================================");
		testOutput.WriteLine($"Facebook - Sign in using username password");
		testOutput.WriteLine("----- Generated Script -----");
		testOutput.WriteLine(generatedScript);
		testOutput.WriteLine("=======================================\n");

		Assert.False(string.IsNullOrWhiteSpace(generatedScript), "The generated script should not be empty.");

		var tempFile = Path.Combine("C:\\repos\\chameleon-playwright\\src\\scripts","facebook.js");
		testOutput.WriteLine($"Tempfile: {tempFile}");
		await File.WriteAllTextAsync(tempFile, generatedScript);

		Assert.True(File.Exists(tempFile));
		var port = await OpenBrowser();

		Exception? executionError = null;
		try {
			await PlaywriteRunner.RunScript(new RunScriptOptions {
				Port = port,
				Description = new(FilePath: tempFile,
				Parameters: new Dictionary<string, string> {
								{ "FACEBOOK_USERNAME", "jmutobu" },
								{ "FACEBOOK_PASSWORD", "test@243" },
								{ "FB_USERNAME", "jmutobu" },
								{ "FB_PASSWORD", "test@243" },
				}
		)
			});
		} catch (Exception ex) {
			testOutput.WriteLine($"PlaywriteRunner.RunScript threw an exception: {ex}");
			executionError = ex;
		} finally {
		
		}
		Assert.Null(executionError);
	}

	string ExtractCodeBlock(string input) {
		var match = EextractCodeBlock().Match(input);
		return match.Success ? match.Groups[1].Value : input;
	}

	async Task<int> OpenBrowser(SystemBrowserType bt = SystemBrowserType.Chrome, int id = 28296) {
		var port = TcpUtil.NextFreePort(9613);
		var browser = await browserService.OpenWithSettings(new(
				new(bt, new() { Id = id }),
				new(),
				"http://example.com",
				port
			)
		);
		Assert.NotNull(browser);
		_ = await browser.LoadedTCS.Task;
		return port;
	}

	[Fact]
	public async Task TestOpenBrowser() {
		var port = await OpenBrowser();
		Assert.True(port > 0);
	}

	[GeneratedRegex(@"```[a-zA-Z0-9]*\r?\n([\s\S]*?)\r?\n```", RegexOptions.Singleline)]
	private static partial Regex EextractCodeBlock();
}
