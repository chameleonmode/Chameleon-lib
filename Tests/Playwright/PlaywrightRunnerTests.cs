using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts.CS;
using Chameleon.lib.Playwright.Scripts.JS;
using Chameleon.lib.Playwright.Scripts.JS.Reddit.Post;
using Chameleon.lib.Playwright.Scripts.JS.Reddit.Subreddit;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Playwright.Utils;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Models;
using Chameleon.lib.WebBrowser.Services;
using static Chameleon.lib.Common.Constants.Enums;

namespace Tests.Playwright;

public class PlaywrightRunnerTests : TestSetup {
	readonly int port = 9613;
	readonly BundledScriptsService repo;
	readonly SystemBrowserService browserService;

	public PlaywrightRunnerTests() {
		repo = BundledScriptsService.Instance;
		browserService = SystemBrowserService.Instance;
	}

	async Task<int> OpenBrowser(SystemBrowserType bt = SystemBrowserType.Chrome, int id = 28296) {
		var port = TcpUtil.NextFreePort(9613);
		var browser = await browserService.OpenWithSettings(new SysBrowserSettings(
				new(bt, new() { Id = id, Port = port, })
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
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			BundledScript = repo.BundledJSScripts[nameof(CommentOnTitle)],
			Description = new(
				Parameters: new() {
					{"search", "christopher walken"}
				}
			)
		});
	}

	[Fact]
	public async Task TestRedditCommentOnCommentScript() {
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			BundledScript = repo.BundledJSScripts[nameof(ReplyToComment)],
			Description = new(
				Parameters: new() {
					{"search", "pringles"}
				}
			)
		});
	}

	[Fact]
	public async Task Reddit_Subreddit_Join() {
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			BundledScript = repo.BundledJSScripts[nameof(Join)],
			Description = new(
				Parameters: new() {
					{"search", "joe rogan"}
				}
			)
		});
	}

	[Fact]
	public async Task Reddit_Subreddit_Vote() {
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			BundledScript = repo.BundledJSScripts[nameof(Vote)],
			Description = new(
				Parameters: new() {
					{"search", "elon musk"}
				}
			)
		});
	}

	[Fact]
	public async Task Reddit_Subreddit_Post() {
		await PlaywriteRunner.RunScript(new() {
			Port = port,
			BundledScript = repo.BundledJSScripts[nameof(Post)],
			Description = new(
				Parameters: new() {
					{"search", "tom segura"}
				}
			)
		});
	}

	[Fact]
	public async Task TestBundledGsiteJsScriptScript() {
		var port = 9613;
		//var port = await OpenBrowser();
		await PlaywriteRunner.RunScript(new RunScriptOptions {
			Port = port,
			BundledScript = repo!.BundledJSScripts[nameof(Gsites)],
			Description = new PlaywrightScriptDescription(
				Parameters: new Dictionary<string, string>
				{
					{ "name", "Site Name" },
					{ "title", "Title" },
					{ "content", "Content" },
					{ "youtube", "aii" },
					//{ "textContent", "Post Content" },
				  //{ "link", "http://example.com" },
					//{ "linkText", "Link Text" },
					//{ "locationSearch", "Hawaii" }
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
