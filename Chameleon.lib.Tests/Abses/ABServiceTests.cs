using System.Text.Json;

using Chameleon.lib.Abs;
using Chameleon.lib.Api;
using Chameleon.lib.Common;
using Chameleon.lib.Common.Types;
using Chameleon.lib.Playwright.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace Chameleon.Tests;

/// <summary>
/// Tests for ABService using xUnit.
/// Adjust namespaces to match your solution.
/// 
/// 1@1 1 KEYF-QSKF-H2W5-LPE2
/// 
/// {
//  "Login": { "LoginName": "elimdadia@gmail.com", "LicenseKey": "HHTQ-QJYS-ZMWX-CO5U" },
//  "Settings": {
//	"CurrentAppTheme": "Dark",
//    "CustomAccentColor": null,
//    "UseCustomAccentColor": false,
//    "AutoLogin": true,
//    "CodesverifyApiKey": "11025f84122066b887645",
//    "UserScriptsDirectory": "C:/repos/scripts",
//    "SMSPoolApiKey": "Rbv5Lt9KTERxuQREjvU8i4ugcwXwNZOT"

//	}
//}
/// </summary>
public class ABServiceTests {
		// Typically, you would inject a mock or real HttpClient here, but for simplicity,
		// we'll just use ABService.Instance directly. 
		private readonly ABService _abService = ABService.Instance;
		private readonly TaskCompletionSource _tcs = new();

		private long userId;
		private string email = string.Empty;
		private string license_key = string.Empty;
		private long? creatorId;

	public ABServiceTests()
	{
		IoC.Instance.Configure(() => {
			return new WritableConfiguration(new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build(), Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
		}, (_) => {
			//_ = services;
		});
		// Setup IoC
		IoC.Instance.Init(async (on) => {
			await Auther.LoginAsync(lib.Tests.Api.Environment.email, lib.Tests.Api.Environment.lkey);

			_ = Assert.NotNull(Auther.AuthSession?.UserId);
			Assert.NotNull(Auther.AuthSession?.UserName);
			Assert.NotNull(Auther.AuthSession?.LicenseKey);

			userId = Auther.AuthSession.UserId;
			email = Auther.AuthSession.UserName;
			license_key = Auther.AuthSession.LicenseKey;
			creatorId = Auther.AuthSession.CreatorUserId;


			_abService.SetLoaders(
					() => Tuple.Create(
							Auther.AuthSession!.UserId,
							Auther.AuthSession!.UserName!,
							Auther.AuthSession!.LicenseKey!,
							Auther.AuthSession!.CreatorUserId
					)
			);

			_ = _tcs.TrySetResult();
		});
	}

	[Fact]
	public async Task ActivateLicenseAsync_Returns_Success()
	{
		await _tcs.Task;

		var result = await _abService.GetTokenAsync();
		Assert.NotNull(result);
	}

	[Fact]
	public async Task LoginAsync_Returns_Success()
	{
		await _tcs.Task;
		//
		var result = await _abService.LoginAsync();
		Assert.NotNull(result);
		// ...
	}

	[Fact]
	public async Task AddCookiesAsync_AddsCookies_Successfully()
	{
		await _tcs.Task;
		// JSON string must match your API’s expected structure
		var data = new { 
			profileId = "12345", 
			cookies = new[] { 
				new {
					name = "AEC",
					value = "someTestCookieValue",
					domain = ".example.com" 
				}
			}
		};

		// Act
		Exception? exception = null;
		try {
			var result = await _abService.AddCookiesAsync("551", new {
				type = "COOKIE", 
				data 
			});
			Assert.NotNull(result);
			//Assert.NotEmpty(result?.Doc?.Objects!);
		} catch (Exception ex) {
			exception = ex;
		}

		// Assert
		Assert.Null(exception);
	}

	[Fact]
	public async Task GetCookiesAsync_Returns_Cookies()
	{
		await _tcs.Task;

		// We expect the cookie type to be "COOKIE". The method automatically appends `...&type=COOKIE`
		// in the querystring.

		// Act
		Exception? exception = null;
		try {
			// We'll pass in <TestCookieResult> as a stand-in for your real BrowserContextCookiesResult 
			// or another type you expect from the server.
			var result = await _abService.GetCookiesAsync<BrowserContextCookiesResult>();
			Assert.NotNull(result);
			//Assert.NotNull(result.Doc);
			//Assert.NotEmpty(result.Doc.Objects);
		} catch (Exception ex) {
			exception = ex;
		}

		// Assert
		Assert.Null(exception);
	}

	[Fact]
	public async Task DeleteCookieAsync_Deletes_Successfully()
	{
		await _tcs.Task;

		// Arrange
		var result = await _abService.GetCookiesAsync<BrowserContextCookiesResult>();
		//var cookieId = result!.Doc!.Objects.First().Id; // The ID of the cookie to delete

		// Act
		var succeeded = false;
		Exception? exception = null;
		try {
			//succeeded = await _abService.DeleteCookieAsync(cookieId);
		} catch (Exception ex) {
			exception = ex;
		}

		// Assert
		Assert.Null(exception);
		Assert.True(succeeded);
	}
}
