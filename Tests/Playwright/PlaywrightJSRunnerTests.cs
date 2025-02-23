using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Services;
using static Chameleon.lib.Common.Constants.Enums;

namespace Tests.Playwright;
public class PlaywrightJSRunnerTests : TestSetup
{
	readonly PlaywrightScriptRepository repo;
	readonly SystemBrowserService browserService;

	public PlaywrightJSRunnerTests()
	{
		repo = PlaywrightScriptRepository.Instance;
		browserService = SystemBrowserService.Instance;
	}

	async Task<int> OpenBrowser(SystemBrowserType bt = SystemBrowserType.Chrome, int id = 28296)
	{
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
	public async Task TestOpenBrowser()
	{
		var port = await OpenBrowser();
		Assert.True(port > 0);
	}

	[Fact]
	public async Task TestReddit1CommentScripts()
	{
		var port = await OpenBrowser();
		var cancellationToken = new CancellationToken();
		await PlaywriteRunner.RunScript(new PlaywriteRunScriptOptions
		{
			Port = port,
			BundledJSScript = repo.BundledJSScripts[nameof(Reddit1Comment)],
			Description = new PlaywrightScriptDescription
			{
				Parameters = [
					new() { Key = "search", Value = "tangy" },
					new() { Key = "comment", Value = "rabba luba dub dub" }
			]
			}
		}, cancellationToken);
	}
}
