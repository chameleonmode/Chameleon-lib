using System.Net;
using System.Text.Json;

using Chameleon.lib.Abs;
using Chameleon.lib.Api;
using Chameleon.lib.Common;
using Chameleon.lib.Common.Types;
using Chameleon.lib.Playwright.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

using Newtonsoft.Json.Linq;

using static System.Runtime.InteropServices.JavaScript.JSType;
using static Chameleon.lib.Common.Constants.Consts;

namespace Chameleon.Tests;

/// <summary>
/// Tests for ABService using xUnit.
/// Adjust namespaces to match your solution.
/// 
/// 1@1 1 KEYF-QSKF-H2W5-LPE2
/// 
//}
/// </summary>
public class AbsClientTests {
	// Typically, you would inject a mock or real HttpClient here, but for simplicity,
	// we'll just use ABService.Instance directly. 
	private readonly AbsClient _absClient = AbsClient.Instance;
	private readonly TaskCompletionSource _tcs = new();

	public AbsClientTests()
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

			_ = _tcs.TrySetResult();
		});
	}

	[Fact]
	public async Task ActivateLicenseAsync_Returns_Success()
	{
		await _tcs.Task;
		Assert.NotNull(Auther.AuthSession);

		// Arrange
		var userId = Auther.AuthSession.UserId;
		var email = Auther.AuthSession.UserName;
		var license_key = Auther.AuthSession.LicenseKey;
		var creatorId = Auther.AuthSession.CreatorUserId;
		var data = new { userId, email, license_key, creatorId };

		// Act
		var response = await _absClient.PostAsync<AuthRecord>(
				"/auth/license",
				data,
				false
		);
		var token = await _absClient.TokenProvider();

		// Assert
		Assert.NotNull(response);
		Assert.NotNull(token);
	}

	[Fact]
	public async Task LoginAsync_Returns_Success()
	{
		await _tcs.Task;
		Assert.NotNull(Auther.AuthSession);

		// Arrange
		var userId = Auther.AuthSession.UserId;
		var email = Auther.AuthSession.UserName;
		var license_key = Auther.AuthSession.LicenseKey;
		var creatorId = Auther.AuthSession.CreatorUserId;
		var data = new { userId, email, license_key, creatorId };
		var response = await _absClient.PostAsync<string>(
				"/auth/license",
				data,
				false
		);

		// Act
		var token = await _absClient.TokenProvider();
		var body = new { token };	
		var result = await _absClient.PostAsync<string>("/auth/login", body);

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Data);
	}

	[Fact]
	public async Task AddCookiesAsync_AddsCookies_Successfully()
	{
		await _tcs.Task;
		Assert.NotNull(Auther.AuthSession);

		// Arrange
		await ActivateLicenseAsync_Returns_Success();
		var data = new
		{
			profileId = "12345",
			cookies = new[] {
					new {
						name = "AEC",
						value = "AZ6Zc-UT2svRnoe-FZ0wB9GgnOPVbMsTMOiT0soWHQsTREZrAmt0G94fDg",
						domain = ".google.com",
						path = "/",
						expires = 1.7511136e9,
						httpOnly = true,
						secure = true,
						sameSite = 1,
					},
					new {
						name = "OGPC",
						value = "19037049-1:",
						domain = ".google.com",
						path = "/",
						expires = 1.7381537e9,
						httpOnly = false,
						secure = false,
						sameSite = 1,
					},
					new {
						name = "NID",
						value = "520=Kzq2qcaWT7cvGWG_-iaz2TUPZFIEfkHSyN1Q_0c83vFEBqYfVmEtParmoim00KCu3kmFjSV7KvQa6cLbXZBhsRQaCwTW2ZQ-2JZxL5v2n6oQZZiSkMTlDxXnpBLRqGNPdLAVvjhK3vXq9gWK6ZB9ymbAAc37-vpCNnxis9wPTkqrCf3aW0GnDmB1wO58MIgLC5_LFwy9y0RmkV_aPWJAXOTKOC4",
						domain = ".google.com",
						path = "/",
						expires = 1.7513729e9,
						httpOnly = true,
						secure = true,
						sameSite = 2,
					},
					new {
						name = "OTZ",
						value = "7887628_76_76_104100_72_446760",
						domain = "ogs.google.com",
						path = "/",
						expires = 1.7514592e9,
						httpOnly = false,
						secure = true,
						sameSite = 1,
					},
				}
		};
		var userId = Auther.AuthSession.UserId;

		// Act
		Exception? exception = null;
		try {
			var endpoint = $"/api/objects/{userId}";
			var body = new { type = ObjectType.COOKIE.ToString(), data };
			var result = await _absClient.PutAsync<Doc<object>>(endpoint, body);
			Assert.NotNull(result);
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
		Assert.NotNull(Auther.AuthSession);

		// Arrange
		await ActivateLicenseAsync_Returns_Success();
		var userId = Auther.AuthSession.UserId;

		// Act
		Exception? exception = null;
		try {
			var endpoint = $"/api/objects/{userId}?type={ObjectType.COOKIE}";
			var result = await _absClient.GetAsync<Doc<ObjectsCookies<object>>>(endpoint);
			Assert.NotNull(result);
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
		Assert.NotNull(Auther.AuthSession);

		// Arrange
		await ActivateLicenseAsync_Returns_Success();
		var userId = Auther.AuthSession.UserId;
		var route = $"/api/objects/{userId}?type={ObjectType.COOKIE}";
		var result = await _absClient.GetAsync<Doc<ObjectsCookies<object>>>(route);

		// Act
		Exception? exception = null;
		try {
			foreach (var cookie in result.Data!.Objects) {
				var endpoint = $"/api/objects/{userId}?type={ObjectType.COOKIE}&_id={cookie.Id}";
				_ = await _absClient.DeleteAsync(endpoint);
			}
		} catch (Exception ex) {
			exception = ex;
		}

		// Assert
		Assert.Null(exception);
	}
}
