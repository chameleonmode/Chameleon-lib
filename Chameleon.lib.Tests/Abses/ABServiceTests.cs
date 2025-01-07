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
//}
/// </summary>
public class ABServiceTests {
		// Typically, you would inject a mock or real HttpClient here, but for simplicity,
		// we'll just use ABService.Instance directly. 
		private readonly ABService _abService = ABService.Instance;
		private readonly TaskCompletionSource _tcs = new();

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
			await Auther.LoginAsync(lib.Tests.Api.Environment.Directory[1].email, lib.Tests.Api.Environment.Directory[1].license);

			_ = Assert.NotNull(Auther.AuthSession?.UserId);

			_ = _tcs.TrySetResult();
		});
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
			var result = await _abService.GetCookies<BrowserContextCookiesResult>();
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
		var result = await _abService.GetCookies<BrowserContextCookiesResult>();
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
