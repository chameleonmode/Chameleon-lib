using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Scripts;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Chameleon.lib;

namespace Tests.Playwright;
public class PlaywrightIntegrationTests : PlaywrightTestsBase, IDisposable {
	public PlaywrightIntegrationTests() : base() {
		async void setup(bool init) {
			// Setup code
			await LaunchBrowser();
			_tcs.SetResult(true);
		}
		IoC.Instance.Configure(() => {
			return new WritableConfiguration(new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build(), Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
		}, (services) => {
			_ = services
			//app.Playwright
			.AddSingleton<ICompileScriptService, CompileScriptService>()
			.AddSingleton<IPlaywriteService, PlaywriteService>()
			.AddSingleton<IPlaywrightScriptRepository, PlaywrightScriptRepository>()
			.AddSingleton<IChromeiumPlaywrightBrowser, ChromeiumPlaywrightBrowser>();
		});
		// Setup IoC
		IoC.Instance.Init(action: setup);
	}

	[Fact]
	public async Task TestBundledScripts() {
		_ = await _tcs.Task;

		var repo = IoC.GetService<IPlaywrightScriptRepository>();
		var playBrowserService = IoC.GetService<IPlaywriteService>();

		await playBrowserService!.RunScript(new PlaywriteRunScriptOptions {
			Port = Port,
			BundledCSScript = repo!.BundledCSScripts[nameof(GoogleCTRClickThrough)],
			Description = new PlaywrightScriptDescription {
				Parameters = [
					new PlaywrightDescriptionParam {
						Id = 1,
						Key = "keyword",
						Value = "you"
					},
					new PlaywrightDescriptionParam {
						Id = 2,
						Key = "targetUrl",
						Value = "abcd.com"
					},
					new PlaywrightDescriptionParam {
						Id = 3,
						Key = "pagescount",
						Value = "you"
					},
					new PlaywrightDescriptionParam {
						Id = 4,
						Key = "timeout",
						Value = "2"
					}
				]
			}
		}, CancellationToken.None);

		playBrowserService.Dispose();

		await DisposeBrowser();
		await LaunchBrowser();

		await playBrowserService!.RunScript(new PlaywriteRunScriptOptions {
			Port = Port,
			BundledCSScript = repo!.BundledCSScripts[nameof(URLsexplorer)],
			Description = new PlaywrightScriptDescription {
				Parameters = [
					new PlaywrightDescriptionParam {
						Id = 1,
						Key = "urls",
						Value = "google.com,x.com"
					},
					new PlaywrightDescriptionParam {
						Id = 2,
						Key = "timeout",
						Value = "2"
					},
				]
			},

		}, CancellationToken.None);

		playBrowserService.Dispose();
		await DisposeBrowser();
	}

	[Fact]
	public async Task TestBundledGsiteJsScriptScript() {
		_ = await _tcs.Task;

		var repo = IoC.GetService<IPlaywrightScriptRepository>();
		var playBrowserService = IoC.GetService<IPlaywriteService>();

		await playBrowserService!.RunScript(new PlaywriteRunScriptOptions {
			Port = Port,
			BundledJSScript = repo!.BundledJSScripts[nameof(GsiteJsScript)],
			Description = new PlaywrightScriptDescription {
				Parameters = [
				new PlaywrightDescriptionParam {
						Id = 1,
						Key = "gsiteTitle",
						Value = "Google Site Title"
					},
					new PlaywrightDescriptionParam {
						Id = 2,
						Key = "publishTitle",
						Value = "Publish Title"
					},
					new PlaywrightDescriptionParam {
						Id = 3,
						Key = "postTitle",
						Value = "Post Title"
					},
					new PlaywrightDescriptionParam {
						Id = 4,
						Key = "textContent",
						Value = "Post Content"
					},
					new PlaywrightDescriptionParam {
						Id = 5,
						Key = "link",
						Value = "HyperLink Link"
					},
					new PlaywrightDescriptionParam {
						Id = 6,
						Key = "textWithLink",
						Value = "HyperLink Text"
					},
					new PlaywrightDescriptionParam {
						Id = 7,
						Key = "textSearch",
						Value = "Youtube KW Search"
					},
					new PlaywrightDescriptionParam {
						Id = 8,
						Key = "location",
						Value = "Post Location Pin"
					},
					new PlaywrightDescriptionParam {
						Id = 9,
						Key = "email",
						Value = "Email"
					},
					new PlaywrightDescriptionParam {
						Id = 10,
						Key = "password",
						Value = "Password"
					}
				]
			}
		}, CancellationToken.None);
	}

	[Fact]
	public async Task TestBundledRedditCommentVoteJsScript() {
		_ = await _tcs.Task;

		var repo = IoC.GetService<IPlaywrightScriptRepository>();
		var playBrowserService = IoC.GetService<IPlaywriteService>();

		await playBrowserService!.RunScript(new PlaywriteRunScriptOptions {
			Port = Port,
			BundledJSScript = repo!.BundledJSScripts[nameof(RedditCommentVoteJsScript)],
			Description = new PlaywrightScriptDescription {
				Parameters = [
		new PlaywrightDescriptionParam {
						Id = 1,
						Key = "textToSearch",
						Value = "Search Key Word"
					},
					new PlaywrightDescriptionParam {
						Id = 2,
						Key = "commenttoMainthread",
						Value = "First Comment"
					},
					new PlaywrightDescriptionParam {
						Id = 3,
						Key = "commenttoMainthread2",
						Value = "Second Comment"
					},
					new PlaywrightDescriptionParam {
						Id = 4,
						Key = "replToComment",
						Value = "Post Content"
					},
					new PlaywrightDescriptionParam {
						Id = 5,
						Key = "reddit_username",
						Value = "chamelionTest1"
					},
					new PlaywrightDescriptionParam {
						Id = 6,
						Key = "test_password",
						Value = "testjosh11011900@123"
					}
				]
			}
		}, CancellationToken.None);

		//await BrowserProcess!.WaitForExitAsync();
	}

	[Fact]
	public async Task TestScriptFromFile() {
		_ = await _tcs.Task;

		var playBrowserService = IoC.GetService<IPlaywriteService>();
		await playBrowserService!.RunScript(new PlaywriteRunScriptOptions {
			Port = Port,
			Description = new PlaywrightScriptDescription {
				FilePath = @"C:\repos\chameleon-lib\Chameleon.lib.Playwright\Scripts\PlaywrightCSTemplate.cs",
			},
		}, CancellationToken.None);

		playBrowserService.Dispose();
	}

	[Fact]
	public async Task TestRecord() {
		try {
			_ = await _tcs.Task;

			var playBrowserService = IoC.GetService<IPlaywriteService>();
			await playBrowserService!.RunScript(new PlaywriteRunScriptOptions {
				Port = Port,
				Record = true
			}, CancellationToken.None);
		} catch (Exception ex) {
			Debug.WriteLine(ex.Message);
		} finally {
			var playBrowserService = IoC.GetService<IPlaywriteService>();
			playBrowserService!.Dispose();
		}
	}

	public async void Dispose() {
		if (BrowserProcess != null && !BrowserProcess.HasExited)
			await BrowserProcess.WaitForExitAsync();
		await DisposeBrowser();
		GC.SuppressFinalize(this);
	}
}
