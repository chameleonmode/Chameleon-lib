using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Scripts;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common;
using Chameleon.lib.Playwright.Models;
using Microsoft.Extensions.DependencyInjection;
using Chameleon.lib.Playwright.Services;
using Microsoft.Extensions.Configuration;
using Chameleon.lib.Common.Types;

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
			BundledScript = repo!.BundledScripts[nameof(GoogleCTRClickThrough)],
			Parameters = new Dictionary<string, string>() {
				{ "keyword", "you" },
				{ "targetUrl", "abcd.com" },
				{ "pagescount", "3" },
				{ "timeout", "2" }
			}
		}, CancellationToken.None);

		playBrowserService.Dispose();

		await DisposeBrowser();
		await LaunchBrowser();

		await playBrowserService!.RunScript(new PlaywriteRunScriptOptions {
			Port = Port,
			BundledScript = repo!.BundledScripts[nameof(URLsexplorer)],
			Parameters = new Dictionary<string, string>() {
				{ "urls", "youtube.com,google.com,x.com" },
				{ "timeout", "2" }
			}
		}, CancellationToken.None);


		playBrowserService.Dispose();
		await DisposeBrowser();
	}

	[Fact]
	public async Task TestScriptFromFile()
	{
		_ = await _tcs.Task;

		var playBrowserService = IoC.GetService<IPlaywriteService>();
		await playBrowserService!.RunScript(new PlaywriteRunScriptOptions {
			Port = Port,
			FilePath = @"C:\repos\chameleon-lib\Chameleon.lib.Playwright\Scripts\PlaywrightCSTemplate.cs",
		}, CancellationToken.None);

		playBrowserService.Dispose();
	}

	[Fact]
	public async Task TestRecord()
	{
		_ = await _tcs.Task;

		var playBrowserService = IoC.GetService<IPlaywriteService>();
		await playBrowserService!.RunScript(new PlaywriteRunScriptOptions {
			Port = Port,
			Record = true,
		}, CancellationToken.None);

		playBrowserService.Dispose();
	}

	public async void Dispose()
	{
		if (BrowserProcess != null && !BrowserProcess.HasExited)
			await BrowserProcess.WaitForExitAsync();
		await DisposeBrowser();
		GC.SuppressFinalize(this);
	}
}
