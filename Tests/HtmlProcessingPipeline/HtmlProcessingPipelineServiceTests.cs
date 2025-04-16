using Chameleon.lib.Playwright.HtmlProcessingPipeline;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Microsoft.Playwright;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
using Xunit.Abstractions;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Utils;
using System.Text.RegularExpressions;
using Chameleon.lib.Const;
using Chameleon.lib.Playwright.HtmlProcessingPipeline.Models;
using System.Diagnostics;

namespace Tests.HtmlProcessingPipeline;
public class HtmlProcessingPipelineServiceTests(ITestOutputHelper testOutput) : Base {

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

		var page = await headlessBrowser!.NewPageAsync();

		var fileHtmlPath = Path.Combine(
				Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
				"Files",
				fileName
		);

		var fakeHtml = await File.ReadAllTextAsync(fileHtmlPath);
		await page.SetContentAsync(fakeHtml);

		var extractorService = new HtmlExtractorService(headlessBrowser);

		var aiOptions = new AiIntegrationOptions {
			ApiKey = "AIzaSyD7THGyxSb5qE60bKmFqdgGr8JTN0xY904",
			ModelName = "gemini-2.0-flash",
			MaxTokens = 500
		};
		var aiService = new AiExtensionsIntegrationService(aiOptions);

		var extractOptions = new ExtractionOptions() { MaxChildDepth = 3, SnippetTextLength = 200 };
		var pipelineService = new HtmlProcessingPipelineService(extractorService, aiService);

		var generatedScript = await pipelineService.ProcessPageAsync(new(
				page,
				extractOptions,
				Steps: new StepDefinition {
					Description = automationDescription,
				}
		 	)
		 );

		testOutput.WriteLine("=======================================");
		testOutput.WriteLine($"Testing File: {fileName}");
		testOutput.WriteLine($"Automation Description:\n{automationDescription}");
		testOutput.WriteLine("----- Generated Script -----");
		testOutput.WriteLine(generatedScript[0]);
		testOutput.WriteLine("=======================================\n");

		Assert.False(string.IsNullOrWhiteSpace(generatedScript[0]), "The generated script should not be empty.");
	}

	[Fact]
	public async Task BFS_IntegrationTest_GenerateLoginScriptForFacebook() {
		await GenerateAndRunScriptAsync(
			url: "https://www.facebook.com",
			options: new() {
				{ "username", "jmutobu191803" },
				{ "password", "Test@243" }
			},
			steps: new StepDefinition {
				Description = @"Facebook Sign-in Automation objectives:
    			Fill in the username and password fields, then click the login button to sign in.
    			If any disclaimers or cookie banners appear, accept or dismiss them
    			(use a dynamic, language-independent selector).
    			For the login button, use a different CSS selector than id or class.
    			Produce a JavaScript Playwright script, with error handling and structured logging,
    			using only import instead of require."
			}
		);
	}

	[Fact]
	public async Task BFS_IntegrationTest_GenerateLoginScriptForXcom() {
		await GenerateAndRunScriptAsync(
			url: "https://x.com",
			options: new() {
				{ "username", "jmutobu191803" },
				{ "password", "Test@243" }
			},
			steps: new StepDefinition {
				Description = @"X.com (Twitter) Login Automation objectives:
        	1. Fill in the username/phone/email and password fields.
        	2. Click the login button.
        	3. Accept or dismiss any cookie banners with a dynamic selector.
        	4. Use error handling and structured logging.
        	5. Use import statements only (no require)."
			}
		);
	}

	[Fact]
	public async Task BFS_IntegrationTest_GenerateLoginScriptForXcom_MultiStep() {
		await GenerateAndRunScriptAsync(
			url: "https://x.com",
			options: new() {
				{ "username", "jmutobu191803" },
				{ "password", "Test@243" }
			},
			steps: [
				new StepDefinition {
					Description = "Accept or dismiss cookie banners or disclaimers if present.",
				 	AutoPerformAction = true
			 	},
				new StepDefinition{
					Description = "Click the 'Sign in' link or button on x.com to open the login overlay or page.",
					AutoPerformAction = true
				},
				new StepDefinition{
					Description = "Fill out the username/phone/email and password fields on the login form, then click 'Continue' or 'Log In'."
				}
			]
		);
	}

	private async Task GenerateAndRunScriptAsync(
		string url,
		Dictionary<string, string> options,
		[System.Runtime.CompilerServices.CallerMemberName] string testName = "",
		params StepDefinition[] steps
	) {
		var page = await headlessBrowser!.NewPageAsync();
		_ = await page.GotoAsync(url, new PageGotoOptions {
			WaitUntil = WaitUntilState.NetworkIdle
		});

		;
		foreach (var response in await new HtmlProcessingPipelineService(
			new HtmlExtractorService(headlessBrowser),
			new AiExtensionsIntegrationService(new AiIntegrationOptions {
				ApiKey = "AIzaSyD7THGyxSb5qE60bKmFqdgGr8JTN0xY904",
				ModelName = "gemini-2.0-flash",
				MaxTokens = 1000
			})
		).Process(
			new(page, new ExtractionOptions { MaxChildDepth = 20, SnippetTextLength = 400 }, Steps: steps)
		)) {
			var match = new Regex(@"```[a-zA-Z0-9]*\r?\n([\s\S]*?)\r?\n```", RegexOptions.Singleline).Match(response);
			var script = match.Success ? match.Groups[1].Value : response;

			Debug.WriteLine("=======================================");
			Debug.WriteLine($"{testName}");
			Debug.WriteLine("----- Generated Script -----");
			Debug.WriteLine(script);
			Debug.WriteLine("=======================================\n");
			Assert.False(string.IsNullOrWhiteSpace(script), "The generated script should not be empty.");

			await Play(script, options);
		}
	}

	async Task Play(string generatedScript, Dictionary<string, string> options) {
		var tempFile = Path.Combine(FilePaths.AppTempScripts, Guid.NewGuid() + ".js");
		Debug.WriteLine($"Tempfile: {tempFile}");
		await File.WriteAllTextAsync(tempFile, generatedScript);

		Assert.True(File.Exists(tempFile));

		Exception? executionError = null;
		try {
			// var browserInstance = await LaunchBrowserFromSettings(new (Chameleon.lib.Common.Constants.Enums.SystemBrowserType.Chrome,
			// 	new() {
			// 		Id = 28296,
			// 		// Proxy = new BrowserProxy() {
			// 		// 	Host = "proxy.chameleonmode.com",
			// 		// 	Port = 31112,
			// 		// 	UserName = "elimdadia_gmail_com",
			// 		// 	Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-vUp6cZAY"
			// 		// }
			// 	})
			// );
			//var port = browserInstance.Settings.Profile.Port;
			var port = 9613;
			await PlaywriteRunner.RunScript(new RunScriptOptions {
				Port = port,
				Description = new(
					FilePath: tempFile,
					Parameters: options
				)
			});
		} catch (Exception ex) {
			Debug.WriteLine($"PlaywriteRunner.RunScript threw an exception: {ex}");
			executionError = ex;
		}

		Assert.Null(executionError);
		Debug.WriteLine("Script executed successfully.");
	}
}
