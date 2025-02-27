using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts.CS;
using Chameleon.lib.Playwright.Scripts.JS;
using Chameleon.lib.Playwright.Scripts.JS.Reddit;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Playwright.Utils;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Services;
using static Chameleon.lib.Common.Constants.Enums;

namespace Tests.Playwright;
public class PlaywrightRunnerTests : TestSetup {
	readonly BundledScriptsService repo;
	readonly SystemBrowserService browserService;

	public PlaywrightRunnerTests() {
		repo = BundledScriptsService.Instance;
		browserService = SystemBrowserService.Instance;
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

	[Fact]
	public async Task TestURLsexplorer() {
		var port = await OpenBrowser();
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			BundledScript = repo.BundledCSScripts[nameof(URLsexplorer)],
			Description = new(
				Parameters: new() {
					{"urls", "example.com, example.org"},
					{"delay", "3"}
				}
			)
		});
	}

	[Fact]
	public async Task TestKeepGmailAlive() {
		var port = await OpenBrowser();
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			BundledScript = repo.BundledCSScripts[nameof(KeepGmailAlive)]
		});
	}

	[Fact]
	public async Task TestGoogleCTR() {
		var port = await OpenBrowser();
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			BundledScript = repo.BundledCSScripts[nameof(GoogleCTR)],
			Description = new(
				Parameters: new() {
					{"search", "example.com"},
					{"target", "https://example.com"},
					{"maxPages", "1"}
				}
			)
		});
	}

	[Fact]
	public async Task TestRedditCommentScript() {
		var search = "tangy sauce";
		var res = await Plair.Instance.Ask(new(
				"reddit",
				new {
					keyword = search,
				}
			)
		);

		var port = await OpenBrowser();
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			BundledScript = repo.BundledJSScripts[nameof(Comment)],
			Description = new(
				Parameters: new() {
					{"search", search},
					{"comment", res!.Payload.Response}
				}
			)
		});
	}

	[Fact]
	public async Task TestBundledGsiteJsScriptScript() {
		var port = await OpenBrowser();

		await PlaywriteRunner.RunScript(new RunScriptOptions {
			Port = port,
			BundledScript = repo!.BundledJSScripts[nameof(Gsites)],
			Description = new PlaywrightScriptDescription(
				Parameters: new Dictionary<string, string>
				{
					{ "name", "Site Name" },
					{ "title", "Title" },
					{ "content", "Content" },
					{ "textContent", "Post Content" },
					{ "link", "http://example.com" },
					{ "linkText", "Link Text" },
					{ "youtubeSearch", "aii" },
					{ "locationSearch", "Hawaii" }
				}
			)
		});
	}

	[Fact]
	public async Task TestRecord() {
		var port = await OpenBrowser();
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			Record = true
		});
	}

	[Fact]
	public async Task TestUserScript() {
		var port = await OpenBrowser();
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			Description = new(
				FilePath: "/Users/dev/Documents/jscripts/test.js",
				Parameters: new(){
					{"url", "https://www.google.com"},
					{"search", "tangy"}
				}
			)
		});
	}
}
