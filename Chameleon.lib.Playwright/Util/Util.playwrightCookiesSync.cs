using Chameleon.lib.Abs;

using Microsoft.Playwright;

using System.Text.Json;

namespace Chameleon.lib.Playwright.Util;
public class PlaywrightCookiesSync {
	public static async Task PostChromiumCookies(string profileId, string cachePath, string exePath)
	{
		var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(cachePath, new() { Headless = true, ExecutablePath = exePath });
		var cookies = await browserContext.CookiesAsync();
		await browserContext.CloseAsync();

		var data = new { profileId, cookies };
		var jsonRequestContent = JsonSerializer.Serialize(new { type = ObjectTypes.OBJECT.GetObjectType(ObjectType.COOKIE), data });
		_ = await ABService.Instance.AddCookiesAsync(jsonRequestContent);
	}

	public static async Task LoadChromiumCookies(string cacheDir, string exePath)
	{
		var response = await ABService.Instance.GetCookiesAsync<BrowserContextCookiesResult>();

		//add loop to add cookies to playwright context
		foreach (var cookies in response!.Data!) {
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
			var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(Path.Combine(cacheDir, cookies.Data.ProfileId!), new() { Headless = true, ExecutablePath = exePath });
			await browserContext.AddCookiesAsync(pcookies);
			await browserContext.CloseAsync();
		}
	}

	public static PlaywrightCookiesSync Instance { get; } = new();
}
