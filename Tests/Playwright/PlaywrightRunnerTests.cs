using Chameleon.lib.AIR.Scripts.Reddit.Post;
using Chameleon.lib.AIR.Scripts.Reddit.Subreddit;
using Chameleon.lib.Playwright.Scripts.CS;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser;
using Chameleon.lib.WebBrowser.Services;


namespace Tests.Playwright;

public class PlaywrightRunnerTests : TestSetup {
	readonly int port = 9613;
	readonly BundledScriptsService repo;
	readonly SystemBrowser browserService;

	public PlaywrightRunnerTests() {
		repo = BundledScriptsService.Instance;
		browserService = SystemBrowser.I;
	}

	async Task<int> OpenBrowser(BrowserType bt = BrowserType.Chrome, int id = 28296) {
		var port = Processez.NextFreePort(9613);
		var browser = await browserService.Launch(
			new(bt, new() { Id = id})
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
		await Run.Script(new() {
			Port = port,
			Script = repo.BundledCSScripts[nameof(URLsexplorer)],
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
		await Run.Script(new() {
			Port = port,
			Script = repo.BundledCSScripts[nameof(KeepGmailAlive)]
		});
	}

	[Fact]
	public async Task TestGoogleCTR() {
		var port = await OpenBrowser();
		await Run.Script(new() {
			Port = port,
			Script = repo.BundledCSScripts[nameof(GoogleCTR)],
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
		await Run.Script(new() {
			Port = port,
			Script = repo.BundledJSScripts[nameof(Comment)],
			Description = new(
				Parameters: new() {
					{"search", "christopher walken"}
				}
			)
		});
	}

	[Fact]
	public async Task TestRedditCommentOnCommentScript() {
		await Run.Script(new() {
			Port = port,
			Script = repo.BundledJSScripts[nameof(Reply)],
			Description = new(
				Parameters: new() {
					{"search", "pringles"}
				}
			)
		});
	}

	[Fact]
	public async Task Reddit_Subreddit_Join() {
		await Run.Script(new() {
			Port = port,
			Script = repo.BundledJSScripts[nameof(Join)],
			Description = new(
				Parameters: new() {
					{"search", "joe rogan"}
				}
			)
		});
	}

	[Fact]
	public async Task Reddit_Subreddit_Vote() {
		await Run.Script(new() {
			Port = port,
			Script = repo.BundledJSScripts[nameof(Vote)],
			Description = new(
				Parameters: new() {
					{"search", "elon musk"}
				}
			)
		});
	}

	[Fact]
	public async Task Reddit_Subreddit_Post() {
		await Run.Script(new() {
			Port = port,
			Script = repo.BundledJSScripts[nameof(Post)],
			Description = new(
				Parameters: new() {
					{"search", "tom segura"}
				}
			)
		});
	}

	[Fact]
	public async Task TestRecord() {
		var port = await OpenBrowser();
		await Run.Script(new() {
			Port = port,
			Record = true
		});
	}

	[Fact]
	public async Task TestUserScript() {
		var port = await OpenBrowser();
		await Run.Script(new() {
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
