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
		var cancellationToken = new CancellationToken();
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			BundledJSScript = repo.BundledJSScripts[nameof(Reddit1Comment)],
			Description = new PlaywrightScriptDescription {
				Parameters = [
					new() { Key = "search", Value = "tangy" },
					new() { Key = "comment", Value = "rabba luba dub dub" }
			]
			}
		}, cancellationToken);
	}

	[Fact]
	public async Task TestBundledGsiteJsScriptScript()
	{
		var port = await OpenBrowser();

		await PlaywriteRunner.RunScript(new PlaywriteRunScriptOptions
		{
			Port = port,
			BundledJSScript = repo!.BundledJSScripts[nameof(GsiteJsScript)],
			Description = new PlaywrightScriptDescription
			{
				Parameters = [
				new PlaywrightDescriptionParam {
						Key = "gsiteTitle",
						Value = "Google Site Title"
					},
					new PlaywrightDescriptionParam {
						Key = "publishTitle",
						Value = "Publish Title"
					},
					new PlaywrightDescriptionParam {
						Key = "postTitle",
						Value = "Post Title"
					},
					new PlaywrightDescriptionParam {
						Key = "textContent",
						Value = "Post Content"
					},
					new PlaywrightDescriptionParam {
						Key = "link",
						Value = "HyperLink Link"
					},
					new PlaywrightDescriptionParam {
						Key = "textWithLink",
						Value = "HyperLink Text"
					},
					new PlaywrightDescriptionParam {
						Key = "textSearch",
						Value = "Youtube KW Search"
					},
					new PlaywrightDescriptionParam {
						Key = "location",
						Value = "Post Location Pin"
					},
					new PlaywrightDescriptionParam {
						Key = "email",
						Value = "Email"
					},
					new PlaywrightDescriptionParam {
						Key = "password",
						Value = "Password"
					}
				]
			}
		}, CancellationToken.None);
	}

	// [Fact]
	// public async Task TestBundledScripts() {
	// 	_ = await _tcs.Task;

	// 	await PlaywriteRunner.RunScript(new PlaywriteRunScriptOptions {
	// 		Port = Port,
	// 		BundledCSScript = repo!.BundledCSScripts[nameof(GoogleCTRClickThrough)],
	// 		Description = new PlaywrightScriptDescription {
	// 			Parameters = [
	// 				new PlaywrightDescriptionParam {
	// 					Id = 1,
	// 					Key = "keyword",
	// 					Value = "you"
	// 				},
	// 				new PlaywrightDescriptionParam {
	// 					Id = 2,
	// 					Key = "targetUrl",
	// 					Value = "abcd.com"
	// 				},
	// 				new PlaywrightDescriptionParam {
	// 					Id = 3,
	// 					Key = "pagescount",
	// 					Value = "you"
	// 				},
	// 				new PlaywrightDescriptionParam {
	// 					Id = 4,
	// 					Key = "timeout",
	// 					Value = "2"
	// 				}
	// 			]
	// 		}
	// 	}, CancellationToken.None);

	// 	PlaywriteRunner.Dispose();

	// 	await DisposeBrowser();
	// 	await LaunchBrowser();

	// 	await PlaywriteRunner.RunScript(new PlaywriteRunScriptOptions {
	// 		Port = Port,
	// 		BundledCSScript = repo!.BundledCSScripts[nameof(URLsexplorer)],
	// 		Description = new PlaywrightScriptDescription {
	// 			Parameters = [
	// 				new PlaywrightDescriptionParam {
	// 					Id = 1,
	// 					Key = "urls",
	// 					Value = "google.com,x.com"
	// 				},
	// 				new PlaywrightDescriptionParam {
	// 					Id = 2,
	// 					Key = "timeout",
	// 					Value = "2"
	// 				},
	// 			]
	// 		},

	// 	}, CancellationToken.None);

	// 	PlaywriteRunner.Dispose();
	// 	await DisposeBrowser();
	// }


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
			Description = new PlaywrightScriptDescription {
				FilePath = "/Users/dev/Documents/jscripts/test.js",
				Parameters = [
					new() { Key = "url", Value = "https://www.google.com" },
					new() { Key = "search", Value = "tangy" },
				]
			}
		});
	}
}
