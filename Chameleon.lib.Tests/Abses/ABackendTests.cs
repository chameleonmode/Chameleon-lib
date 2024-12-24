using System.Text;
using System.Text.Json;

using Chameleon.lib.Api;
using Chameleon.lib.Common;
using Chameleon.lib.Common.Types;

using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

using Abs = Chameleon.lib.Abs;

namespace Chameleon.Tests;
public class ABackendTests {
	private readonly string clientBase = "http://localhost:3001";
	private readonly TaskCompletionSource _tcs = new();

	private long userId;
	private string email = string.Empty;
	private string license_key = string.Empty;

	public ABackendTests()
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

			_ = _tcs.TrySetResult();
		});
	}
	[Fact]
	public async Task LicenseEnpoint_Succeeds()
	{
		await _tcs.Task;
		//
		using var httpClient = new HttpClient();
		var authContent = new StringContent(JsonSerializer.Serialize(new { userId, email, license_key }), Encoding.UTF8, "application/json");
		var authResponse = await httpClient.PostAsync($"{clientBase}/auth/license", authContent);
		var authResponseString = await authResponse.Content.ReadAsStringAsync();
		var succes = authResponse.EnsureSuccessStatusCode();
		Assert.True(succes.IsSuccessStatusCode);

		var authResponseContent = JsonSerializer.Deserialize<Abs.ApiSuccessResponse<Abs.Doc<Abs.TokenObject>>>(authResponseString, new JsonSerializerOptions() {
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});

		var token = authResponseContent?.Data?.Objects.Last(o => o.Type == Abs.ObjectTypes.USER.GetUserType(Abs.UserType.TOKEN)).Data.Token;
		Assert.NotNull(token);
		IoC.SetValue(token, Abs.IoCKeys.TokenObject);
		var savedToken = IoC.GetValue(Abs.IoCKeys.TokenObject);
		Assert.Equal(token, savedToken);
	}

	[Fact]
	public async Task LoginEnpoint_Succeeds()
	{
		await _tcs.Task;
		//
		var token = IoC.GetValue(Abs.IoCKeys.TokenObject);
		Assert.NotNull(token);
		//
		using var httpClient = new HttpClient();
		var authContent = new StringContent(JsonSerializer.Serialize(new { token }), Encoding.UTF8, "application/json");
		var authResponse = await httpClient.PostAsync($"{clientBase}/auth/login", authContent);
		var authResponseString = await authResponse.Content.ReadAsStringAsync();
		var succes = authResponse.EnsureSuccessStatusCode();
		Assert.True(succes.IsSuccessStatusCode);

		var authResponseContent = JsonSerializer.Deserialize<Abs.ApiSuccessResponse<Abs.Doc<Abs.TokenObject>>>(authResponseString, new JsonSerializerOptions() {
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});

		var newtoken = authResponseContent?.Data?.Objects.Last(o => o.Type == Abs.ObjectTypes.USER.GetUserType(Abs.UserType.TOKEN)).Data.Token;
		Assert.NotNull(newtoken);
		IoC.SetValue(newtoken, Abs.IoCKeys.TokenObject);
		var savedToken = IoC.GetValue(Abs.IoCKeys.TokenObject);
		Assert.Equal(newtoken, savedToken);
	}

	[Fact]
	public async Task Enpoint_ApiErrorResponse()
	{
		await _tcs.Task;
		//
		var token = "wrong token";
		//
		using var httpClient = new HttpClient();
		var authContent = new StringContent(JsonSerializer.Serialize(new { token }), Encoding.UTF8, "application/json");
		var authResponse = await httpClient.PostAsync($"{clientBase}/auth/login", authContent);
		var authResponseString = await authResponse.Content.ReadAsStringAsync();
		try {
			var succes = authResponse.EnsureSuccessStatusCode();
			if (succes.IsSuccessStatusCode) throw new Exception("Expected a failure");
		} catch {
			var authResponseContent = JsonSerializer.Deserialize<Abs.ApiErrorResponse>(authResponseString, new JsonSerializerOptions() {
				PropertyNameCaseInsensitive = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			});
			Assert.NotNull(authResponseContent);
			Assert.NotNull(authResponseContent.Error);
			Assert.NotNull(authResponseContent.Code);
		}
	}

	[Fact]
	public async Task AddObjects_Succeeds()
	{
		await _tcs.Task;
		//
		var token = IoC.GetValue(Abs.IoCKeys.TokenObject);
		Assert.NotNull(token);
		//
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		// Add request parameters
		var requestUri = $"{clientBase}/api/objects/{userId}";
		var data = new
		{
			type = "CUSTOM",
			data = new
			{
				foo = "bar",
				baz = "qux",
				quux = "corge"
			}
		};
		var requestContent = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
		var response = await httpClient.PutAsync(requestUri, requestContent);
		var responseString = await response.Content.ReadAsStringAsync();
		_ = response.EnsureSuccessStatusCode();

		var authResponseContent = JsonSerializer.Deserialize<Abs.ApiSuccessResponse<Abs.Doc<object>>>(responseString, new JsonSerializerOptions() {
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});
		Assert.Equal(JsonSerializer.Serialize(data.data), JsonSerializer.Serialize(authResponseContent?.Data?.Objects.Last().Data));
	}

	[Fact]
	public async Task GetObjects_Succeeds()
	{
		await _tcs.Task;
		//
		var token = IoC.GetValue(Abs.IoCKeys.TokenObject);
		Assert.NotNull(token);
		//
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		// Add request parameters
		var requestUri = $"{clientBase}/api/objects/{userId}?type=CUSTOM";
		var requestContent = new StringContent(JsonSerializer.Serialize(new { type = "CUSTOM" }), Encoding.UTF8, "application/json");
		var response = await httpClient.GetAsync(requestUri);
		var responseString = await response.Content.ReadAsStringAsync();
		_ = response.EnsureSuccessStatusCode();

		var authResponseContent = JsonSerializer.Deserialize<Abs.ApiSuccessResponse<List<Abs.BaseObject<object>>>>(responseString, new JsonSerializerOptions() {
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});
		Assert.NotNull(authResponseContent?.Data);
	}

	[Fact]
	public async Task AddCookiesSucceeds()
	{
		await _tcs.Task;
		//
		var token = IoC.GetValue(Abs.IoCKeys.TokenObject);
		Assert.NotNull(token);
		//
		var cookiesJson = """
			{
				"type":"COOKIE",
				"data":{
					"profileId":"25541",
					"cookies": [
					{
						"name":"AEC",
						"value":"AZ6Zc-W5_FI_3j5WV2tP_CZSoj4fFBducKDnHTZtX9ZG1Goeh7JRHx_w8g",
						"domain":".google.com",
						"path":"/",
						"expires":1.7487887E+09,
						"httpOnly":true,
						"secure":true,
						"sameSite":1
					},
					{
						"name":"NID","value":"519=Z04BHLYc2hfg6z8t-v7KZv6R9qQZ7Ws8OMsLgFD9zPTNYoHg2aghqwJz8OWsVPjFiRprrNCGq9avlNIK4bge3PwJKSI1AIQbH5ZG6M2mC3BBmTkPcUlVwle_BHqCmR67DKLd_7ocN9XXKsBxpyIw7I8QrMNKehuiWxLnCtOL1nF_aMO2KYHmv0ENpZASl0bTKMCFYYe0UT542PDYbTcHZG0FFLug","domain":".google.com","path":"/","expires":1.7487887E+09,"httpOnly":true,"secure":true,"sameSite":2
					}]
				}
			}
			""";
		// 
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var requestUri = $"{clientBase}/api/objects/{userId}";
		var requestContent = new StringContent(cookiesJson, Encoding.UTF8, "application/json");
		var response = await httpClient.PutAsync(requestUri, requestContent);
		var responseString = await response.Content.ReadAsStringAsync();
		_ = response.EnsureSuccessStatusCode();

		var authResponseContent = JsonSerializer.Deserialize<Abs.ApiSuccessResponse<Abs.Doc<object>>>(responseString, new JsonSerializerOptions() {
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});
		Assert.Equal(
			"{\"profileId\":\"25541\",\"cookies\":[{\"name\":\"AEC\",\"value\":\"AZ6Zc-W5_FI_3j5WV2tP_CZSoj4fFBducKDnHTZtX9ZG1Goeh7JRHx_w8g\",\"domain\":\".google.com\",\"path\":\"/\",\"expires\":1748788700,\"httpOnly\":true,\"secure\":true,\"sameSite\":1},{\"name\":\"NID\",\"value\":\"519=Z04BHLYc2hfg6z8t-v7KZv6R9qQZ7Ws8OMsLgFD9zPTNYoHg2aghqwJz8OWsVPjFiRprrNCGq9avlNIK4bge3PwJKSI1AIQbH5ZG6M2mC3BBmTkPcUlVwle_BHqCmR67DKLd_7ocN9XXKsBxpyIw7I8QrMNKehuiWxLnCtOL1nF_aMO2KYHmv0ENpZASl0bTKMCFYYe0UT542PDYbTcHZG0FFLug\",\"domain\":\".google.com\",\"path\":\"/\",\"expires\":1748788700,\"httpOnly\":true,\"secure\":true,\"sameSite\":2}]}"
			, JsonSerializer.Serialize(authResponseContent?.Data?.Objects.Last().Data)
		);
	}

	[Fact]
	public async Task GetCookiesSucceeds()
	{
		await _tcs.Task;
		//
		var token = IoC.GetValue(Abs.IoCKeys.TokenObject);
		Assert.NotNull(token);
		// 
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var requestUri = $"{clientBase}/api/objects/{userId}?type={Abs.ObjectTypes.OBJECT.GetObjectType(Abs.ObjectType.COOKIE)}";
		var response = await httpClient.GetAsync(requestUri);
		var responseString = await response.Content.ReadAsStringAsync();
		_ = response.EnsureSuccessStatusCode();

		var authResponseContent = JsonSerializer.Deserialize<Abs.ApiSuccessResponse<List<Abs.BaseObject<Abs.CookieObject<BrowserContextCookiesResult>>>>>(responseString, new JsonSerializerOptions() {
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});
		Assert.NotNull(authResponseContent?.Data);
		Assert.NotEmpty(authResponseContent.Data);

	}

	[Fact]
	public async Task DeleteCookiesSucceeds()
	{
		await _tcs.Task;
		//
		var token = IoC.GetValue(Abs.IoCKeys.TokenObject);
		Assert.NotNull(token);
		// 
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var requestUri = $"{clientBase}/api/objects/{userId}?type={Abs.ObjectTypes.OBJECT.GetObjectType(Abs.ObjectType.COOKIE)}&_id=676a9422c8434c0ca0ed6403";
		var response = await httpClient.DeleteAsync(requestUri);
		var responseString = await response.Content.ReadAsStringAsync();
		_ = response.EnsureSuccessStatusCode();
	}
}