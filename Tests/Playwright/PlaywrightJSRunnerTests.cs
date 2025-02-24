using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Playwright.Utils;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Services;
using static Chameleon.lib.Common.Constants.Enums;

namespace Tests.Playwright;
public class PlaywrightJSRunnerTests : TestSetup {
	readonly PlaywrightScriptRepository repo;
	readonly SystemBrowserService browserService;

	public PlaywrightJSRunnerTests() {
		repo = PlaywrightScriptRepository.Instance;
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
	public async Task TestReddit1CommentScripts() {
		var port = await OpenBrowser();
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			BundledJSScript = repo.BundledJSScripts[nameof(Reddit1Comment)],
			Description = new (
				Parameters: new() {
					{"search", "tangy"},
					{"comment", "rabba luba dub dub"}
				}
			)
		});
	}

	[Fact]
	public async Task TestBundledGsiteJsScriptScript()
	{
		var port = await OpenBrowser();

		await PlaywriteRunner.RunScript(new RunScriptOptions
		{
			Port = port,
			BundledJSScript = repo!.BundledJSScripts[nameof(GsiteJsScript)],
			Description = new PlaywrightScriptDescription (
				Parameters: new Dictionary<string, string>
				{
					{"gsiteTitle", "Google Site Title"},
					{"publishTitle", "Publish Title"},
					{"postTitle", "Post Title"},
					{"textContent", "Post Content"},
					{"link", "HyperLink Link"},
					{"textWithLink", "HyperLink Text"},
					{"textSearch", "Youtube KW Search"},
					{"location", "Post Location Pin"},
					{"email", "Email"},
					{"password", "Password"}
				}
			)
		});
	}
	
	[Fact]
	public async Task TestRecord() {
		var port = await OpenBrowser();
		await PlaywriteRunner.RunScript(new () {
			Port = port,
			Record = true
		});
	}

	[Fact]
	public async Task TestUserScript() {
		var port = await OpenBrowser();
		await PlaywriteRunner.RunScript(new () {
			Port = port,
			Description = new (
				FilePath: "/Users/dev/Documents/jscripts/test.js",
				Parameters: new(){
					{"url", "https://www.google.com"},
					{"search", "tangy"}
				}
			)
		});
	}
}
