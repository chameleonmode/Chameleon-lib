using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Const;
using Chameleon.lib.Helpers;
using Chameleon.lib.Playwright.Utils;

using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Services;

public sealed class PlaywrightCookiesSyncService {
	readonly List<CookyPayload<BrowserContextCookiesResult>> cookyPayloads = [];
	#region Constructor
	private PlaywrightCookiesSyncService() { }
	// Thread-safe singleton implementation
	private static readonly Lazy<PlaywrightCookiesSyncService> _instance = new(() => new PlaywrightCookiesSyncService(), LazyThreadSafetyMode.ExecutionAndPublication);
	public static PlaywrightCookiesSyncService Instance => _instance.Value;
	// ----------------------------
	#endregion

	public async Task<bool> HasCookies() {
		cookyPayloads.Clear();
		var cookiesSearch = await DB.Instance.GetCookyDataInteractions<BrowserContextCookiesResult>();
		if (cookiesSearch != null) {
			cookyPayloads.AddRange(cookiesSearch);
		}
		return cookyPayloads.Count != 0;
	}

	// Syncs cookies to browser
	public async Task SyncCookies(Enums.SystemBrowserType browserType)
	{
		// Check latest cookies on server
		if(!await HasCookies()) {
			Toaster.Info("No cookies to sync");
			return;
		}

		// Retrieve path to the browser executable
		var exePath = await PlaywrightUtil.GetBrowseExecutablePath(browserType);

		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var playwrightBrowser = browserType == Enums.SystemBrowserType.Firefox
				? playwright.Firefox
				: playwright.Chromium;

		//// We only want cookie entries that have a non-empty ProfileId
		//var cookiesToSync = absApiCookiesRepo.CookiesCache;

		var cookieSyncIndex = 0;
		var cookieSyncTotal = cookyPayloads.Count;
		foreach (var cookieData in cookyPayloads) {
			// Log: starting sync for this profile
			Toaster.Info($"[Cookies/Sync] Starting cookie sync: {++cookieSyncIndex} out of {cookieSyncTotal}");

			// Add the cookies to the context
			await using var context = await playwrightBrowser.LaunchPersistentContextAsync(
					Path.Combine(FilePaths.AppDataLocalDir, browserType.ToString(), cookieData.profileId),
					new() {
						Headless = true,
						ExecutablePath = await PlaywrightUtil.GetBrowseExecutablePath(browserType),
						Args = ["--allow-downgrade"]
					}
			);
			await context.AddCookiesAsync(
				cookieData.cookiesJs!.Select(c =>
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
		}
	}
}