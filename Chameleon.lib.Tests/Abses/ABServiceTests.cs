using Chameleon.lib.Abs;
using Chameleon.lib.Api;
using Chameleon.lib.Common;
using Chameleon.lib.Common.Types;

using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace Chameleon.Tests;

/// <summary>
/// Tests for ABService using xUnit.
/// Adjust namespaces to match your solution.
/// </summary>
public class ABServiceTests {
		// Typically, you would inject a mock or real HttpClient here, but for simplicity,
		// we'll just use ABService.Instance directly. 
		private readonly ABService _abService = ABService.Instance;
		private readonly TaskCompletionSource _tcs = new();

		private long userId;
		private string email = string.Empty;
		private string license_key = string.Empty;

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

			_abService.SetLoaders(() => {
				return IoC.GetValue(IoCKeys.TokenObject)!;
			}
			, () => Auther.AuthSession.UserId);

			_ = _tcs.TrySetResult();
		});
	}

	[Fact]
	public async Task ActivateLicenseAsync_Returns_Success()
	{
		await _tcs.Task;

		var result = await _abService.ActivateLicenseAsync(userId, email, license_key);
		Assert.NotNull(result);
		Assert.NotNull(result?.Data?.Objects);
		var token = result?.Data?.Objects
			.FindLast(o => o.Type == ObjectTypes.USER.GetUserType(UserType.TOKEN))?.Data?.Token;
		Assert.NotNull(token);

		IoC.SetValue(token, IoCKeys.TokenObject);
		var savedToken = IoC.GetValue(IoCKeys.TokenObject);
		Assert.NotNull(savedToken);
		Assert.Equal(token, savedToken);
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
	public async Task AddObjectAsync_Creates_An_Object_Successfully()
	{
		await _tcs.Task;

		// Arrange
		var objectType = ObjectTypes.OBJECT.GetObjectType(ObjectType.CUSTOM);
		var data = new
		{
			foo = "bar",
			baz = "qux"
		};

		// Act
		ApiSuccessResponse<Doc<object>>? result = null;
		Exception? exception = null;
		try {
			result = await _abService.AddObjectAsync(objectType, data);
		} catch (Exception ex) {
			exception = ex;
		}

		// Assert
		Assert.Null(exception);
		// For real integration:
		Assert.NotNull(result);
		Assert.Equal("CUSTOM", result?.Data?.Objects[0].Type);
	}

	[Fact]
	public async Task GetObjectsAsync_Returns_Objects()
	{
		await _tcs.Task;

		// Arrange
		var objectType = ObjectType.CUSTOM;

		// Act
		ApiSuccessResponse<List<BaseObject<object>>>? result = null;
		Exception? exception = null;
		try {
			result = await _abService.GetObjectsAsync(objectType);
		} catch (Exception ex) {
			exception = ex;
		}

		// Assert
		Assert.Null(exception);
		// For real integration:
		Assert.NotNull(result);
		Assert.NotNull(result?.Data);
	}

	[Fact]
	public async Task AddCookiesAsync_AddsCookies_Successfully()
	{
		await _tcs.Task;
		// JSON string must match your API’s expected structure
		var cookiesJson = """
        {
          "type": "COOKIE",
          "data": {
            "profileId": "12345",
            "cookies": [
              {
                "name": "AEC",
                "value": "someTestCookieValue",
                "domain": ".example.com"
              }
            ]
          }
        }
        """;

		// Act
		ApiSuccessResponse<Doc<object>>? result = null;
		Exception? exception = null;
		try {
			result = await _abService.AddCookiesAsync(cookiesJson);
		} catch (Exception ex) {
			exception = ex;
		}

		// Assert
		Assert.Null(exception);
		// For real integration:
		Assert.NotNull(result);
		Assert.NotEmpty(result?.Data?.Objects!);
	}

	[Fact]
	public async Task GetCookiesAsync_Returns_Cookies()
	{
		await _tcs.Task;

		// We expect the cookie type to be "COOKIE". The method automatically appends `...&type=COOKIE`
		// in the querystring.

		// Act
		ApiSuccessResponse<List<BaseObject<CookieObject<BrowserContextCookiesResult>>>>? result = null;
		Exception? exception = null;
		try {
			// We'll pass in <TestCookieResult> as a stand-in for your real BrowserContextCookiesResult 
			// or another type you expect from the server.
			result = await _abService.GetCookiesAsync<BrowserContextCookiesResult>();
		} catch (Exception ex) {
			exception = ex;
		}

		// Assert
		Assert.Null(exception);
		// For real integration:
		Assert.NotNull(result);
		Assert.NotEmpty(result?.Data!);
	}

	[Fact]
	public async Task DeleteCookieAsync_Deletes_Successfully()
	{
		await _tcs.Task;

		// Arrange
		var result = await _abService.GetCookiesAsync<BrowserContextCookiesResult>();
		var cookieId = result!.Data!.First().Id; // The ID of the cookie to delete

		// Act
		var succeeded = false;
		Exception? exception = null;
		try {
			succeeded = await _abService.DeleteCookieAsync(cookieId);
		} catch (Exception ex) {
			exception = ex;
		}

		// Assert
		Assert.Null(exception);
		Assert.True(succeeded);
	}
}
