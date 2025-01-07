using Chameleon.lib.Abs;
using Chameleon.lib.Api;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.ServiceManagers;
using Chameleon.lib.Common.Util;

using Microsoft.Playwright;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Chameleon.lib.Playwright.Services;

public sealed class PlaywrightCookiesSyncService {
	// Repos
	private readonly AbsApiCookiesRepo<BrowserContextCookiesResult> absApiCookiesRepo = new();

	// Properties
	public Task<bool> HasCookies => absApiCookiesRepo.HasCookies;

	#region Constructor
	private PlaywrightCookiesSyncService() { }
	// Thread-safe singleton implementation
	private static readonly Lazy<PlaywrightCookiesSyncService> _instance = new(() => new PlaywrightCookiesSyncService(), LazyThreadSafetyMode.ExecutionAndPublication);
	public static PlaywrightCookiesSyncService Instance => _instance.Value;
	// ----------------------------
	#endregion

	// Uploads Chromium cookies to server
	public async Task PutCookies(string userId, string? email, string profileId, Enums.SystemBrowserType browserType)
	{
		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var playwrightBrowser = browserType == Enums.SystemBrowserType.Firefox
		? playwright.Firefox
		: playwright.Chromium;
		await using var context = await playwrightBrowser.LaunchPersistentContextAsync(
				Path.Combine(Consts.AppDataLocalDir, browserType.ToString(), profileId),
				new() {
					Headless = true,
					ExecutablePath = await PlaywrightUtil.GetExecutable(browserType),
					Args = ["--allow-downgrade"]
				}
		);

		var cookies = await context.CookiesAsync();
		await context.CloseAsync();

		if (cookies.Any()) {
			await absApiCookiesRepo.AddCookies(userId, email,  profileId, cookies);
		}
	}

	// Syncs cookies to browser
	public async Task SyncCookies(Enums.SystemBrowserType browserType)
	{
		if (!await HasCookies) return;

		// Retrieve path to the browser executable
		var exePath = await PlaywrightUtil.GetExecutable(browserType);

		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var playwrightBrowser = browserType == Enums.SystemBrowserType.Firefox
				? playwright.Firefox
				: playwright.Chromium;

		// We only want cookie entries that have a non-empty ProfileId
		var cookiesToSync = absApiCookiesRepo.CookiesCache;

		var cookieSyncIndex = 0;
		var cookieSyncTotal = cookiesToSync.Count;
		foreach (var cookieData in cookiesToSync) {
			// Log: starting sync for this profile
			Toaster.Info($"[Cookies/Sync] Starting cookie sync: {++cookieSyncIndex} out of {cookieSyncTotal}");

			// Add the cookies to the context
			await using var context = await playwrightBrowser.LaunchPersistentContextAsync(
					IOtil.EnsureDirectoryExists(Path.Combine(Consts.AppDataLocalDir, browserType.ToString(), cookieData.ProfileId!)),
					new() {
						Headless = true,
						ExecutablePath = exePath,
						Args = ["--allow-downgrade"]
					}
			);
			await context.AddCookiesAsync(
				cookieData.Cookies!.Select(c => 
					new Cookie {
						Domain = c.Domain,
						Expires = c.Expires,
						HttpOnly = c.HttpOnly,
						Name = c.Name,
						Path = c.Path,
						SameSite = c.SameSite,
						Secure = c.Secure,
						Value = c.Value
					}
				)
			);
			// Close the context
			await context.CloseAsync();

			// Log: done syncing
			Console.WriteLine($"[Cookies/Sync] Finished cookie sync for Profile: {cookieData.ProfileId}\n");
		}
	}

	//Clears synchronized cookies from both the cache and server
	public async Task ClearCookies()
	{
		await absApiCookiesRepo.DeleteCookies();
	}
}