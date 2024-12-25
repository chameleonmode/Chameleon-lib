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
using static Chameleon.lib.Abs.ObjectTypes;
using DynamicData;

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

		await _playwrightCookiesRepo.SyncCookies(Enums.SystemBrowserType.Brave, false);
	}

	[Fact]
	public async Task Test_CookiesRepo_GetCookies_intoChrome()
	{
		_ = await _tcs.Task;

		await _playwrightCookiesRepo.SyncCookies(Enums.SystemBrowserType.Chrome, false);
	}

	[Fact]
	public async Task Test_CookiesRepo_GetCookies_intoFirefox()
	{
		_ = await _tcs.Task;
	//C:\repos\Chameleon\Chameleon.Avalonia\src\Chameleon.Avalonia.Desktop\obj\outwin\.playwright\node\win32_x64\node.exe C:\repos\Chameleon\Chameleon.Avalonia\src\Chameleon.Avalonia.Desktop\obj\outwin\.playwright\package\cli.js install firefox
		await _playwrightCookiesRepo.SyncCookies(Enums.SystemBrowserType.Firefox, false);
	}
	[Fact]
	public async Task TestPostCookies()
	{
		_ = await _tcs.Task;

		var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(CachePath, new() { Headless = true, ExecutablePath = IoC.GetValue<string>("BrowserPath") });
		var cookies = await browserContext.CookiesAsync();
		await browserContext.CloseAsync();

		var data = new { profileId = "25541", cookies };
		var jsonRequestContent = JsonSerializer.Serialize(new { type = Abs.ObjectTypes.OBJECT.GetObjectType(ObjectType.COOKIE), data });

		Assert.NotEmpty(cookies);
		Assert.NotNull(jsonRequestContent);
	}

	[Fact]
	public async Task TestGetCookies()
	{
		_ = await _tcs.Task;

    var responseString = """
    {
      "data": [
        {
          "type": "COOKIE",
          "data": {
            "profileId": "25541",
            "cookies": [
              {
                "name": "AEC",
                "value": "AZ6Zc-W5_FI_3j5WV2tP_CZSoj4fFBducKDnHTZtX9ZG1Goeh7JRHx_w8g",
                "domain": ".google.com",
                "path": "/",
                "expires": 1748788700,
                "httpOnly": true,
                "secure": true,
                "sameSite": 1
              },
              {
                "name": "NID",
                "value": "519=Z04BHLYc2hfg6z8t-v7KZv6R9qQZ7Ws8OMsLgFD9zPTNYoHg2aghqwJz8OWsVPjFiRprrNCGq9avlNIK4bge3PwJKSI1AIQbH5ZG6M2mC3BBmTkPcUlVwle_BHqCmR67DKLd_7ocN9XXKsBxpyIw7I8QrMNKehuiWxLnCtOL1nF_aMO2KYHmv0ENpZASl0bTKMCFYYe0UT542PDYbTcHZG0FFLug",
                "domain": ".google.com",
                "path": "/",
                "expires": 1748788700,
                "httpOnly": true,
                "secure": true,
                "sameSite": 2
              }
            ]
          },
          "_id": "676a94f6c8434c0ca0ed640c"
        },
        {
          "type": "COOKIE",
          "data": {
            "profileId": "25541",
            "cookies": [
              {
                "name": "AEC",
                "value": "AZ6Zc-W5_FI_3j5WV2tP_CZSoj4fFBducKDnHTZtX9ZG1Goeh7JRHx_w8g",
                "domain": ".google.com",
                "path": "/",
                "expires": 1748788700,
                "httpOnly": true,
                "secure": true,
                "sameSite": 1
              },
              {
                "name": "NID",
                "value": "519=Z04BHLYc2hfg6z8t-v7KZv6R9qQZ7Ws8OMsLgFD9zPTNYoHg2aghqwJz8OWsVPjFiRprrNCGq9avlNIK4bge3PwJKSI1AIQbH5ZG6M2mC3BBmTkPcUlVwle_BHqCmR67DKLd_7ocN9XXKsBxpyIw7I8QrMNKehuiWxLnCtOL1nF_aMO2KYHmv0ENpZASl0bTKMCFYYe0UT542PDYbTcHZG0FFLug",
                "domain": ".google.com",
                "path": "/",
                "expires": 1748788700,
                "httpOnly": true,
                "secure": true,
                "sameSite": 2
              }
            ]
          },
          "_id": "676a9511c8434c0ca0ed6416"
        },
        {
          "type": "COOKIE",
          "data": {
            "profileId": "25541",
            "cookies": [
              {
                "name": "AEC",
                "value": "AZ6Zc-W5_FI_3j5WV2tP_CZSoj4fFBducKDnHTZtX9ZG1Goeh7JRHx_w8g",
                "domain": ".google.com",
                "path": "/",
                "expires": 1748788700,
                "httpOnly": true,
                "secure": true,
                "sameSite": 1
              },
              {
                "name": "NID",
                "value": "519=Z04BHLYc2hfg6z8t-v7KZv6R9qQZ7Ws8OMsLgFD9zPTNYoHg2aghqwJz8OWsVPjFiRprrNCGq9avlNIK4bge3PwJKSI1AIQbH5ZG6M2mC3BBmTkPcUlVwle_BHqCmR67DKLd_7ocN9XXKsBxpyIw7I8QrMNKehuiWxLnCtOL1nF_aMO2KYHmv0ENpZASl0bTKMCFYYe0UT542PDYbTcHZG0FFLug",
                "domain": ".google.com",
                "path": "/",
                "expires": 1748788700,
                "httpOnly": true,
                "secure": true,
                "sameSite": 2
              }
            ]
          },
          "_id": "676aa57d339c13619e72465a"
        },
        {
          "type": "COOKIE",
          "data": {
            "profileId": "25541",
            "cookies": [
              {
                "name": "AEC",
                "value": "AZ6Zc-W5_FI_3j5WV2tP_CZSoj4fFBducKDnHTZtX9ZG1Goeh7JRHx_w8g",
                "domain": ".google.com",
                "path": "/",
                "expires": 1748788700,
                "httpOnly": true,
                "secure": true,
                "sameSite": 1
              },
              {
                "name": "NID",
                "value": "519=Z04BHLYc2hfg6z8t-v7KZv6R9qQZ7Ws8OMsLgFD9zPTNYoHg2aghqwJz8OWsVPjFiRprrNCGq9avlNIK4bge3PwJKSI1AIQbH5ZG6M2mC3BBmTkPcUlVwle_BHqCmR67DKLd_7ocN9XXKsBxpyIw7I8QrMNKehuiWxLnCtOL1nF_aMO2KYHmv0ENpZASl0bTKMCFYYe0UT542PDYbTcHZG0FFLug",
                "domain": ".google.com",
                "path": "/",
                "expires": 1748788700,
                "httpOnly": true,
                "secure": true,
                "sameSite": 2
              }
            ]
          },
          "_id": "676aa585339c13619e72468f"
        },
        {
          "type": "COOKIE",
          "data": {
            "profileId": "25541",
            "cookies": [
              {
                "name": "AEC",
                "value": "AZ6Zc-W5_FI_3j5WV2tP_CZSoj4fFBducKDnHTZtX9ZG1Goeh7JRHx_w8g",
                "domain": ".google.com",
                "path": "/",
                "expires": 1748788700,
                "httpOnly": true,
                "secure": true,
                "sameSite": 1
              },
              {
                "name": "NID",
                "value": "519=Z04BHLYc2hfg6z8t-v7KZv6R9qQZ7Ws8OMsLgFD9zPTNYoHg2aghqwJz8OWsVPjFiRprrNCGq9avlNIK4bge3PwJKSI1AIQbH5ZG6M2mC3BBmTkPcUlVwle_BHqCmR67DKLd_7ocN9XXKsBxpyIw7I8QrMNKehuiWxLnCtOL1nF_aMO2KYHmv0ENpZASl0bTKMCFYYe0UT542PDYbTcHZG0FFLug",
                "domain": ".google.com",
                "path": "/",
                "expires": 1748788700,
                "httpOnly": true,
                "secure": true,
                "sameSite": 2
              }
            ]
          },
          "_id": "676aa6fb6d953f476a670dac"
        }
      ]
    }
    """;
		var cookiesResponse = JsonSerializer.Deserialize<Abs.ApiSuccessResponse<List<BaseObject<CookieObject<BrowserContextCookiesResult>>>>>(responseString, new JsonSerializerOptions() {
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});
		Assert.NotNull(cookiesResponse?.Data);

		//add loop to add cookies to playwright context
		foreach (var cookies in cookiesResponse!.Data!) {
			var pcookies = new List<Microsoft.Playwright.Cookie>();
			foreach (var cookie in cookies.Data.Cookies!) {
				pcookies.Add(new Microsoft.Playwright.Cookie {
					Domain = cookie.Domain,
					Expires = cookie.Expires,
					HttpOnly = cookie.HttpOnly,
					Name = cookie.Name,
					Path = cookie.Path,
					SameSite = cookie.SameSite,
					Secure = cookie.Secure,
					Value = cookie.Value
				});
			}
			var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
			var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(
				@"C:\Users\eli\AppData\Local\Chameleon\Brave\" + cookies.Data.ProfileId, 
				new() { Headless = true, ExecutablePath = IoC.GetValue<string>("BrowserPath") }
			);
			await browserContext.AddCookiesAsync(pcookies);
			await browserContext.CloseAsync();
		}
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}