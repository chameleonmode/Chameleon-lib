using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Scripts;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Common.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using Microsoft.Playwright;
using static Chameleon.lib.Common.Constants.Consts;
using Chameleon.lib.Api;
using Microsoft.Extensions.Options;
using static Chameleon.lib.Tests.Playwright.PlaywrightCookiesTests;
namespace Chameleon.lib.Tests.Playwright;
public class PlaywrightCookiesTests : PlaywrightTestsBase, IDisposable {
	private readonly string clientBase = "http://localhost:3001";

	public class Rootobject<T> {
		public Datum<T>[] data { get; set; }
	}

	public class Datum<T> {
		public string? type { get; set; }
		public T? data { get; set; }
		public string? _id { get; set; }
	}

	public class CookiesData {
		public BrowserContextCookiesResult[] cookies { get; set; }
		public int project { get; set; }
	}

	public class Root<T> {
		public Data<T>? data { get; set; }
	}

	public class Data<T> {
		public string? userId { get; set; }
		public Object<T>[]? objects { get; set; }
		public DateTime createdAt { get; set; }
		public DateTime updatedAt { get; set; }
		public string? id { get; set; }
	}

	public class Object<T> {
		public string? type { get; set; }
		public T? data { get; set; }
		public string? _id { get; set; }
	}

	public class TokenData {
		public string? token { get; set; }
	}

	public PlaywrightCookiesTests() : base()
	{
		async void setup(bool init)
		{
			// Setup code
			Port = Netil.NextFreePort(Port);
			await Auther.LoginAsync(Chameleon.lib.Tests.Api.Environment.email, Chameleon.lib.Tests.Api.Environment.lkey);
			//_ = new Process {
			//	StartInfo = new ProcessStartInfo {
			//		FileName = "chrome.exe",
			//		Arguments = string.Join(" ", new List<string> {
			//				"--disable-session-crashed-bubble",
			//				"--hide-crash-restore-bubble",
			//				"--restore-last-session",
			//				"--profile-directory=Default",
			//				"--ash-no-nudges",
			//				"--disable-domain-reliability",
			//				"--no-default-browser-check",
			//				"--no-first-run",
			//				"--disable-field-trial-config",
			//				$"--remote-debugging-port={Port}",
			//				"--disable-hyperlink-auditing",
			//				$"--user-data-dir=\"{CachePath}\""
			//		}),
			//		UseShellExecute = true,
			//		ErrorDialog = true,
			//		CreateNoWindow = true,
			//	},
			//	EnableRaisingEvents = true,
			//}.Start();
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
	public async Task TestPostCookies()
	{
		_ = await _tcs.Task;

		// 
		var userId = Auther.AuthSession?.UserId;
		var email = Auther.AuthSession?.UserName;
		var license_key = Auther.AuthSession?.LicenseKey;

		// Prepare the HTTP client
		using var httpClient = new HttpClient();
		var authContent = new StringContent(JsonSerializer.Serialize(new { userId, email, license_key }), Encoding.UTF8, "application/json");
		var authResponse = await httpClient.PostAsync($"{clientBase}/auth/license", authContent);
		var authResponseString = await authResponse.Content.ReadAsStringAsync();
		var authResponseContent = JsonSerializer.Deserialize<Root<TokenData>>(authResponseString);
		Assert.NotNull(authResponseContent?.data?.objects?[0]?.data?.token);

		var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(CachePath, new() { Headless = true, ExecutablePath = IoC.GetValue<string>("BrowserPath") });
		var cookies = await browserContext.CookiesAsync();
		var data = new { cookies, project = 25541 };

		httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponseContent?.data?.objects?[0]?.data?.token);

		// Add request parameters
		var requestUri = $"{clientBase}/api/objects/{userId}";
		var requestContent = new StringContent(JsonSerializer.Serialize(new { type = "CUSTOM", data }), Encoding.UTF8, "application/json");
		var response = await httpClient.PutAsync(requestUri, requestContent);
		_ = response.EnsureSuccessStatusCode();
		var responseString = await response.Content.ReadAsStringAsync();

		await browserContext.CloseAsync();
	}

	[Fact]
	public async Task TestGetCookies()
	{
		_ = await _tcs.Task;

		// 
		var userId = Auther.AuthSession?.UserId;
		var email = Auther.AuthSession?.UserName;
		var license_key = Auther.AuthSession?.LicenseKey;

		// Prepare the HTTP client
		using var httpClient = new HttpClient();
		var authContent = new StringContent(JsonSerializer.Serialize(new { userId, email, license_key }), Encoding.UTF8, "application/json");
		var authResponse = await httpClient.PostAsync($"{clientBase}/auth/license", authContent);
		var authResponseString = await authResponse.Content.ReadAsStringAsync();
		var authResponseContent = JsonSerializer.Deserialize<Root<TokenData>>(authResponseString);
		Assert.NotNull(authResponseContent?.data?.objects?[0]?.data?.token);

		// 
		httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponseContent?.data?.objects?[0]?.data?.token);
		var requestUri = $"{clientBase}/api/objects/{userId}?type=CUSTOM";
		var requestContent = new StringContent(JsonSerializer.Serialize(new { type = "CUSTOM" }), Encoding.UTF8, "application/json");
		var response = await httpClient.GetAsync(requestUri);
		_ = response.EnsureSuccessStatusCode();

		var cookiesJson = await response.Content.ReadAsStringAsync();
		var cookies = JsonSerializer.Deserialize<Rootobject<CookiesData>>(cookiesJson);

		//add loop to add cookies to playwright context
		var pcookies = new List<Microsoft.Playwright.Cookie>();
		foreach (var item in cookies?.data!) {
			foreach (var cookie in item.data!.cookies) {
				pcookies.Add(new Microsoft.Playwright.Cookie {
					Domain = cookie.Domain,
					Expires = cookie.Expires,
					HttpOnly = cookie.HttpOnly,
					Name = cookie.Name,
					Path = cookie.Path,
					SameSite = cookie.SameSite,
					Secure = cookie.Secure,
					Value = cookie.Value // Fix: Ensure that 'Value' is a property, not a method
				});
			}
		}

		var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(CachePath, new() { Headless = true, ExecutablePath = IoC.GetValue<string>("BrowserPath") });
		await browserContext.AddCookiesAsync(pcookies);
		await browserContext.CloseAsync();
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}
