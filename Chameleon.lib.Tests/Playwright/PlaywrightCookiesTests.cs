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
namespace Chameleon.lib.Tests.Playwright;
public class PlaywrightCookiesTests : PlaywrightTestsBase, IDisposable {
	public class CookieRequest {
		public string? userId { get; set; }
		public IReadOnlyList<BrowserContextCookiesResult> cookies { get; set; } = [];
	}
	public PlaywrightCookiesTests() : base()
	{
		void setup(bool init)
		{
			// Setup code
			Port = Netil.NextFreePort(Port);
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
		try {
			_ = await _tcs.Task;
			await Task.Delay(2000);
			var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
			var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(CachePath, new() { Headless = true });
			var page = await browserContext!.NewPageAsync();
			_ = await page.GotoAsync("https://example.com");

			var cookies = await browserContext.CookiesAsync();

			// Convert cookies to JSON
			//var cookiesJson = JsonSerializer.Serialize(cookies);

			// Assume user information is already retrieved and authenticated via Amplify
			var userId = "authenticated-user-id"; // Replace with actual user ID retrieval logic

			// Prepare the HTTP client
			// Prepare the HTTP client
			using var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjU1MSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJlbGltZGFkaWFAZ21haWwuY29tIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvZW1haWxhZGRyZXNzIjoiZWxpbWRhZGlhQGdtYWlsLmNvbSIsIkFzcE5ldC5JZGVudGl0eS5TZWN1cml0eVN0YW1wIjoiU1g1MkpQUU9MUEpINTJWQ0Q3TkdETTJLNTRNWUxJN0siLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJBZG1pbiIsImh0dHA6Ly93d3cuYXNwbmV0Ym9pbGVycGxhdGUuY29tL2lkZW50aXR5L2NsYWltcy90ZW5hbnRJZCI6IjIwMSIsInN1YiI6IjU1MSIsImp0aSI6IjFmZGU1YjIzLWZjMzAtNGFkNC04MDMyLTVlZDNkNzA0OTBhMSIsImlhdCI6MTcyNDA1Nzc1OSwibmJmIjoxNzI0MDU3NzU5LCJleHAiOjE3MjQxNDQxNTksImlzcyI6IkNoYW1lbGVvbiIsImF1ZCI6IkNoYW1lbGVvbiJ9.bcpzTCpInBEsmjEyWzLfFaAB5dh7_HSYx9bLMabru1I");
			var requestContent = new StringContent(JsonSerializer.Serialize(new { userId, cookies }), Encoding.UTF8, "application/json");

		// Replace with your actual ASP.NET endpoint
			var response = await httpClient.PutAsync("https://localhost:56332/api/s3", requestContent);

			_ = response.EnsureSuccessStatusCode();

			await browserContext.CloseAsync();
		} catch (Exception ex) {
			Debug.WriteLine(ex.Message);
		} finally {
		}
	}

	[Fact]
	public async Task TestGetCookies()
	{
		try {
			// context.Token = SimpleStringCipher.Instance.Decrypt(qsAuthToken, Environment.DefaultPassPhrase);
			_ = await _tcs.Task;
			await Task.Delay(2000);
			var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
			var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(CachePath, new() { Headless = true });

			// Assume user information is already retrieved and authenticated via Amplify
			//var userId = "authenticated-user-id"; // Replace with actual user ID retrieval logic

			// Prepare the HTTP client
			using var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjU1MSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJlbGltZGFkaWFAZ21haWwuY29tIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvZW1haWxhZGRyZXNzIjoiZWxpbWRhZGlhQGdtYWlsLmNvbSIsIkFzcE5ldC5JZGVudGl0eS5TZWN1cml0eVN0YW1wIjoiU1g1MkpQUU9MUEpINTJWQ0Q3TkdETTJLNTRNWUxJN0siLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJBZG1pbiIsImh0dHA6Ly93d3cuYXNwbmV0Ym9pbGVycGxhdGUuY29tL2lkZW50aXR5L2NsYWltcy90ZW5hbnRJZCI6IjIwMSIsInN1YiI6IjU1MSIsImp0aSI6IjFmZGU1YjIzLWZjMzAtNGFkNC04MDMyLTVlZDNkNzA0OTBhMSIsImlhdCI6MTcyNDA1Nzc1OSwibmJmIjoxNzI0MDU3NzU5LCJleHAiOjE3MjQxNDQxNTksImlzcyI6IkNoYW1lbGVvbiIsImF1ZCI6IkNoYW1lbGVvbiJ9.bcpzTCpInBEsmjEyWzLfFaAB5dh7_HSYx9bLMabru1I");
			//var requestContent = new StringContent(JsonSerializer.Serialize(new CookieRequestBody() { UserId = userId }), System.Text.Encoding.UTF8, "application/json");

			// Replace with your actual Lambda endpoint
			var response = await httpClient.GetAsync($"https://localhost:56332/api/s3/cookies");
			response.EnsureSuccessStatusCode();

			var cookiesJson = await response.Content.ReadAsStringAsync();
      var cookies = JsonSerializer.Deserialize<CookieRequest>(cookiesJson);

			//add loop to add cookies to playwright context
			var pcookies = new List<Microsoft.Playwright.Cookie>();
			foreach (var cookie in cookies?.cookies!) {
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
			await browserContext.AddCookiesAsync(pcookies);
			await browserContext.CloseAsync();
		} catch (Exception ex) {
			Debug.WriteLine(ex.Message);
		} finally {
		}
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}
