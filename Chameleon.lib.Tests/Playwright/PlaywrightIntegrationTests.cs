using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Scripts;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Common.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
namespace Chameleon.lib.Tests.Playwright;
public class PlaywrightIntegrationTests : PlaywrightTestsBase, IDisposable {
	public PlaywrightIntegrationTests() : base()
	{
		async void setup(bool init)
		{
			// Setup code
			Port = Netil.NextFreePort(Port);
			CachePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			BrowserProcess = GrowserProcess(CachePath, [$"--remote-debugging-port={Port}"]);
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
	public async Task TestBundledScripts()
	{
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
	public async Task TestBundledJSScript()
	{
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
						Key = "url",
						Value = "https://sites.google.com/"
					},
					new PlaywrightDescriptionParam {
						Id = 2,
						Key = "email",
						Value = "testjosh11011900@gmail.com"
					},
					new PlaywrightDescriptionParam {
						Id = 3,
						Key = "password",
						Value = "testjosh11011900@123"
					},
					new PlaywrightDescriptionParam {
						Id = 4,
						Key = "textContent",
						Value = "blaa blaa laddy dAAAA doo"
					},
					new PlaywrightDescriptionParam {
						Id = 5,
						Key = "textSearch",
						Value = "What is da title"
					},
					new PlaywrightDescriptionParam {
						Id = 6,
						Key = "location",
						Value = "new york"
					},
					new PlaywrightDescriptionParam {
						Id = 7,
						Key = "publishTitle",
						Value = "datitleexplained"
					},
					new PlaywrightDescriptionParam {
						Id = 8,
						Key = "gsiteTitle",
						Value = "Da Title"
					},
					new PlaywrightDescriptionParam {
						Id = 9,
						Key = "postTitle",
						Value = "zIpErry doo daa"
					}
				]
			}
		}, CancellationToken.None);
	}

	[Fact]
	public async Task TestScriptFromFile()
	{
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
	public async Task TestRecord()
	{
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

	public async void Dispose()
	{
		if (BrowserProcess != null && !BrowserProcess.HasExited)
			await BrowserProcess.WaitForExitAsync();
		await DisposeBrowser();
		GC.SuppressFinalize(this);
	}
}
