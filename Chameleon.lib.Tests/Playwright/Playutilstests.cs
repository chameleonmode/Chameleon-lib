using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Common;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Common.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Playwright;
namespace Chameleon.lib.Tests.Playwright;
public class Playutilstests : PlaywrightTestsBase, IDisposable {
	static readonly string pid = "wawa";
	readonly string profile = Path.Combine(Consts.AppDataLocalDir, Enums.SystemBrowserType.Chrome.ToString(), pid);
	readonly string profile_brv = Path.Combine(Consts.AppDataLocalDir, Enums.SystemBrowserType.Brave.ToString(), pid);
	public Playutilstests() : base()
	{
		async void setup(bool init)
		{
			// Setup code
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
	public async Task TestBundledScripts_chrm()
	{
		_ = await _tcs.Task;

		//
		await PlaywrightUtil.CreateDevmodePrefs(Enums.SystemBrowserType.Chrome, pid);
		await LaunchBrowser(profile);
	}


	[Fact]
	public async Task TestBundledScripts_brv()
	{
		_ = await _tcs.Task;

		//
		await PlaywrightUtil.CreateDevmodePrefs(Enums.SystemBrowserType.Brave, pid);
		await LaunchBrowser(profile_brv);
	}

	public async void Dispose()
	{
		if (BrowserProcess != null && !BrowserProcess.HasExited)
			await BrowserProcess.WaitForExitAsync();
		await DisposeBrowser();
		GC.SuppressFinalize(this);
	}
}
