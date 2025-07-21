using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Browzio;
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

public sealed class Sync {
	readonly List<DB.Routes.Cooky.Replies.CookyPayload<BrowserContextCookiesResult>> cookyPayloads = [];
	#region Constructor
	private Sync() { }
	// Thread-safe singleton implementation
	private static readonly Lazy<Sync> _instance = new(() => new Sync(), LazyThreadSafetyMode.ExecutionAndPublication);
	public static Sync Instance => _instance.Value;
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
	public async Task SyncCookies(Browzio.BrowserType browserType) {
		// Check latest cookies on server
		if (!await HasCookies()) {
			Toaster.Info("No cookies to sync");
			return;
		}

		// Retrieve path to the browser executable
		var exePath = await Util.GetBrowseExecutablePath(browserType);

		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var playwrightBrowser = browserType == Browzio.BrowserType.Firefox ? playwright.Firefox : playwright.Chromium;

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
						ExecutablePath = exePath,
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

	public static async Task<IReadOnlyList<BrowserContextCookiesResult>> GetCookies(Options options)
		=> await EX.Poly(async () => await ExecuteCookieAction(options)) ?? 
			throw new InvalidOperationException("Failed to retrieve cookies");

	public static async Task<IReadOnlyList<BrowserContextCookiesResult>> SetCookies(Options options, IEnumerable<Cookie> cookies)
		=> await EX.Poly(async () => await ExecuteCookieAction(options, [.. cookies])) ??
			throw new InvalidOperationException("Failed to set cookies");

	private static async Task<IReadOnlyList<BrowserContextCookiesResult>> ExecuteCookieAction(Options options, List<Cookie>? cookiesToSet = null) {
		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var playwrightBrowser = options.Browser.BrowserType == Browzio.BrowserType.Firefox ? playwright.Firefox : playwright.Chromium;
		try {
			if (options.Port != null) {
				await using var browser = await playwrightBrowser.ConnectOverCDPAsync($"http://localhost:{options.Port}");
				var context = browser.Contexts.Count > 0 ? browser.Contexts[0] : await browser.NewContextAsync();
				if (cookiesToSet == null) {
					return await context.CookiesAsync();
				} else {
					await context.AddCookiesAsync(cookiesToSet);
					return [];
				}
			} else {
				await using var context = await playwrightBrowser.LaunchPersistentContextAsync(
						options.Dir,
						new() {
							Headless = true,
							Args = ["--allow-downgrade"],
							Proxy = options.Proxy,
							ExecutablePath = await Util.GetBrowseExecutablePath(options.Browser.BrowserType),
							// ExecutablePath = Browzio.Browzio.Utilities.GetBrowser(options.Browser.BrowserType)?.ExecutablePath ??
							// 	throw new InvalidOperationException("Browser executable path not found."),
						}
				);
				if (cookiesToSet == null) {
					var cookies = await context.CookiesAsync();
					return cookies;
				} else {
					await context.AddCookiesAsync(cookiesToSet);
					await context.CloseAsync();
					return Array.Empty<BrowserContextCookiesResult>();
				}
			}
		} finally {
			playwright.Dispose();
		}
	}
}