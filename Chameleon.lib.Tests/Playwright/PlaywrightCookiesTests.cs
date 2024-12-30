using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Common.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Microsoft.Playwright;
using Chameleon.lib.Api;
using Chameleon.lib.Abs;
using Chameleon.lib.Common.Constants;

namespace Chameleon.lib.Tests.Playwright;
public class PlaywrightCookiesTests : PlaywrightTestsBase, IDisposable {
	private readonly PlaywrightCookiesRepo _playwrightCookiesRepo = PlaywrightCookiesRepo.Instance;

	public PlaywrightCookiesTests() : base()
	{
		async void setup(bool init)
		{
			// Setup code
			Port = Netil.NextFreePort(Port);
			await Auther.LoginAsync(lib.Tests.Api.Environment.email, lib.Tests.Api.Environment.lkey);

			_ = Assert.NotNull(Auther.AuthSession?.UserId);
			Assert.NotNull(Auther.AuthSession?.UserName);
			Assert.NotNull(Auther.AuthSession?.LicenseKey);
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
	public async Task Test_CookiesRepo_PutCookies()
	{
		_ = await _tcs.Task;

		await _playwrightCookiesRepo.PutChromiumCookies(
			Auther.AuthSession!.UserId!.ToString(),
			"25541",
			Enums.SystemBrowserType.Chrome
		);
		await _playwrightCookiesRepo.PutChromiumCookies(
			Auther.AuthSession!.UserId!.ToString(),
			"25542",
			Enums.SystemBrowserType.Chrome
		);
	}

	[Fact]
	public async Task Test_CookiesRepo_GetCookies_intoBrave()
	{
		_ = await _tcs.Task;

		await _playwrightCookiesRepo.SyncCookies(Enums.SystemBrowserType.Brave);
	}

	[Fact]
	public async Task Test_CookiesRepo_GetCookies_intoChrome()
	{
		_ = await _tcs.Task;

		await _playwrightCookiesRepo.SyncCookies(Enums.SystemBrowserType.Chrome);
	}

	[Fact]
	public async Task Test_CookiesRepo_GetCookies_intoFirefox()
	{
		_ = await _tcs.Task;
	//C:\repos\Chameleon\Chameleon.Avalonia\src\Chameleon.Avalonia.Desktop\obj\outwin\.playwright\node\win32_x64\node.exe C:\repos\Chameleon\Chameleon.Avalonia\src\Chameleon.Avalonia.Desktop\obj\outwin\.playwright\package\cli.js install firefox
		await _playwrightCookiesRepo.SyncCookies(Enums.SystemBrowserType.Firefox);
	}

	[Fact]
	public async Task Test_CookiesRepo_Clear()
	{
		_ = await _tcs.Task;
		await _playwrightCookiesRepo.SyncCookiesClear();
	}
	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}