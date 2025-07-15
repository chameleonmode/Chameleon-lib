using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Browzer;
using Chameleon.lib.Helpers;
using Chameleon.lib.Util;

using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Services;

public record Options(BrowserSetting Browser, int? Port) {
	public Proxy? Proxy => Browser.Profile.Proxy.Server == null ? null
	 : new() {
		 Server = Browser.Profile.Proxy.Server,
		 Username = Browser.Profile.Proxy.UserName,
		 Password = Browser.Profile.Proxy.Password,
	 };

	public string Dir => Path.Combine(FilePaths.AppDataLocalDir, Browser.BrowserType.ToString(), Browser.Profile.Id.ToString());
}

public sealed class PlaywrightCookiesSyncService {
	readonly List<DB.Routes.Cooky.Replies.CookyPayload<BrowserContextCookiesResult>> cookyPayloads = [];
	#region Constructor
	private PlaywrightCookiesSyncService() { }
	// Thread-safe singleton implementation
	private static readonly Lazy<PlaywrightCookiesSyncService> _instance = new(() => new PlaywrightCookiesSyncService(), LazyThreadSafetyMode.ExecutionAndPublication);
	public static PlaywrightCookiesSyncService Instance => _instance.Value;
	// ----------------------------
	#endregion

	public async Task<bool> HasCookies() {
		cookyPayloads.Clear();
		var cookiesSearch = await DB.I.Cooky.GetCookies<BrowserContextCookiesResult>();
		if (cookiesSearch != null) {
			cookyPayloads.AddRange(cookiesSearch);
		}
		return cookyPayloads.Count != 0;
	}

	// Syncs cookies to browser
	public async Task SyncCookies(Browzer.BrowserType browserType) {
		// Check latest cookies on server
		if (!await HasCookies()) {
			Toaster.Info("No cookies to sync");
			return;
		}

		// Retrieve path to the browser executable
		var exePath = await Util.GetBrowseExecutablePath(browserType);

		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var playwrightBrowser = browserType == Browzer.BrowserType.Firefox
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
					Path.Combine(FilePaths.AppDataLocalDir, browserType.ToString(), cookieData.ProfileId),
					new() {
						Headless = true,
						ExecutablePath = await Util.GetBrowseExecutablePath(browserType),
						Args = ["--allow-downgrade"]
					}
			);
			await context.AddCookiesAsync(
				cookieData.CookiesJs!.Select(c =>
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