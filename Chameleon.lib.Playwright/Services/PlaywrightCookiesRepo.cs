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

public sealed class PlaywrightCookiesRepo {
	private readonly ABService _abService = ABService.Instance;
	private readonly List<BaseObject<CookieObject<BrowserContextCookiesResult>>> _cookiesCache = [];

	private PlaywrightCookiesRepo()
	{
		// Set authentication loaders
		_abService.SetLoaders(() => new (
				Auther.AuthSession!.UserId,
				Auther.AuthSession!.UserName!,
				Auther.AuthSession!.LicenseKey!,
				Auther.AuthSession!.CreatorUserId
		));
	}

	// Checks and ensures authentication
	private async Task EnsureAuthenticated()
	{
		if (!_abService.IsAuthenticated) {
			var token = await _abService.GetTokenAsync()
					?? throw new InvalidOperationException("Failed to activate permissions for cookies sync");
		}
	}

	// Uploads Chromium cookies to server
	public async Task PutChromiumCookies(string userId, string profileId, Enums.SystemBrowserType browserType)
	{
		await EnsureAuthenticated();

		var browserPath = SysBrowserInfoUtil.FindByType(browserType).Path;
		var profilePath = Path.Combine(Consts.AppDataLocalDir, browserType.ToString(), profileId);

		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		await using var context = await playwright.Chromium.LaunchPersistentContextAsync(
				profilePath,
				new() { Headless = true, ExecutablePath = browserPath }
		);

		var cookies = await context.CookiesAsync();
		await context.CloseAsync();

		if (cookies.Any()) {
			_ = await _abService.AddCookiesAsync(userId, new { profileId, cookies });
		}
	}

	// Retrieves cookies from server
	public async Task<bool> GetCookies()
	{
		await EnsureAuthenticated();

		var results = (await _abService.GetCookiesAsync<BrowserContextCookiesResult>())?.Data
				?? throw new InvalidOperationException("Response is unreadable");

		_cookiesCache.Clear();
		_cookiesCache.AddRange(results);
		return _cookiesCache.Count > 0;
	}

	// Syncs cookies to browser
	public async Task SyncCookies(Enums.SystemBrowserType browserType)
	{
		if (!await GetCookies()) return;

		// Retrieve path to the browser executable
		var exePath = browserType == Enums.SystemBrowserType.Firefox
				? await InstallPlaywrightsFirefoxIfNecessary()
				: SysBrowserInfoUtil.FindByType(browserType).Path;

		if (string.IsNullOrEmpty(exePath))
			throw new InvalidOperationException("Browser executable path not found");

		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var playwrightBrowser = browserType == Enums.SystemBrowserType.Firefox
				? playwright.Firefox
				: playwright.Chromium;

		// We only want cookie entries that have a non-empty ProfileId
		var cookiesToSync = _cookiesCache.Where(c => !string.IsNullOrEmpty(c.Data.ProfileId));

		var cookieSyncIndex = 0;
		var cookieSyncTotal = cookiesToSync.Count();
		foreach (var cookieData in cookiesToSync) {
			// Log: starting sync for this profile
			Toaster.ShowInf($"[Cookies/Sync] Starting cookie sync: {++cookieSyncIndex} out of {cookieSyncTotal}");

			var profilePath = Path.Combine(
					Consts.AppDataLocalDir,
					browserType.ToString(),
					cookieData.Data.ProfileId!
			);

			await using var context = await playwrightBrowser.LaunchPersistentContextAsync(
					profilePath,
					new() {
						Headless = true,
						ExecutablePath = exePath
					}
			);

			// Prepare cookies to add
			var cookies = cookieData.Data.Cookies!.Select(c => new Cookie {
				Domain = c.Domain,
				Expires = c.Expires,
				HttpOnly = c.HttpOnly,
				Name = c.Name,
				Path = c.Path,
				SameSite = c.SameSite,
				Secure = c.Secure,
				Value = c.Value
			}).ToList();

			// Log: how many cookies we are adding
			Console.WriteLine($"[Cookies/Sync] Adding {cookies.Count} cookies for Profile: {cookieData.Data.ProfileId}");

			// Add the cookies to the context
			await context.AddCookiesAsync(cookies);

			// Close the context
			await context.CloseAsync();

			// Log: done syncing
			Console.WriteLine($"[Cookies/Sync] Finished cookie sync for Profile: {cookieData.Data.ProfileId}\n");
		}
	}

	//Clears synchronized cookies from both the cache and server
	public async Task SyncCookiesClear()
	{
		// Exit if no cookies are available
		if (!await GetCookies()) {
			return;
		}

		// Process deletion from end to start to avoid index shifting issues
		for (var i = _cookiesCache.Count - 1; i >= 0; i--) {
			try {
				var cookie = _cookiesCache[i];
				Toaster.ShowInf($"Clearing ... {i + 1} remaining");

				if (await _abService.DeleteCookieAsync(cookie.Id)) {
					_cookiesCache.RemoveAt(i);
				} else {
					// Log or handle failed deletion
					Debug.WriteLine($"Failed to delete cookie with ID: {cookie.Id}");
				}
			} catch (Exception ex) {
				// Log error but continue with remaining cookies
				Debug.WriteLine($"Error clearing cookie at index {i}: {ex.Message}");
				continue;
			}
		}

		// Optional: Notify when all cookies are cleared
		if (_cookiesCache.Count == 0) {
			Toaster.ShowSuccess("All cookies cleared successfully");
		}
	}

	// Installs Playwright's Firefox if not already installed
	private static async Task<string?> InstallPlaywrightsFirefoxIfNecessary()
	{
		// 1) Check if it is already installed
		var existingPath = FindPlaywrightFirefox();
		if (existingPath != null) {
			return existingPath;
		}

		try {
			Toaster.ShowInf("Installing Firefox Sync Update...");

			// 1.5) Install Firefox if not found
			var (nodePath, cliPath) = GetPlaywrightPaths();

			using var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = nodePath,
					Arguments = $"{cliPath} install firefox",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};

			// 2) Subscribe to output/error events
			process.OutputDataReceived += (sender, e) => {
				if (!string.IsNullOrEmpty(e.Data) && !e.Data.Contains("playwright")) {
					Toaster.ShowInf($"[Installing Firefox Sync Update...]: {Regex.Replace(e.Data.Replace("â–", ""), @"\s+", " ").Trim()}");
				}
			};
			process.ErrorDataReceived += (sender, e) => {
				if (!string.IsNullOrEmpty(e.Data)) {
					Toaster.ShowErr($"[Firefox Sync Update Install/Error]: {e.Data}");
				}
			};

			// 3) Start process, then begin reading from redirected streams
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			// 4) Wait for the process to exit
			await process.WaitForExitAsync();

			// 5) Check exit code
			if (process.ExitCode != 0) {
				throw new InvalidOperationException(
						$"Firefox installation failed with exit code: {process.ExitCode}"
				);
			}

			// If successful, re-check and return path
			return FindPlaywrightFirefox();
		} catch (Exception ex) {
			throw new InvalidOperationException("Failed to install Firefox Sync Update", ex);
		}
	}

	// Finds existing Playwright Firefox installation
	private static string? FindPlaywrightFirefox()
	{
		var cacheDir = GetPlaywrightCacheDir();
		if (!Directory.Exists(cacheDir)) {
			return null;
		}

		var firefoxDir = Directory
				.GetDirectories(cacheDir, "firefox-*", SearchOption.TopDirectoryOnly)
				.OrderByDescending(d => d)
				.FirstOrDefault();

		return firefoxDir == null ? null : Path.Combine(firefoxDir, "firefox", "firefox.exe");
	}

	// Gets Playwright paths based on OS
	private static (string NodePath, string CliPath) GetPlaywrightPaths()
	{
		var basePath = Path.Combine(
				AppDomain.CurrentDomain.BaseDirectory,
				OperatingSystem.IsMacOS() ? "../Resources/.playwright" : ".playwright"
		);

		var nodePath = Path.Combine(
				basePath,
				"node",
				OperatingSystem.IsMacOS() ? "darwin-x64/node" : "win32_x64/node.exe"
		);

		var cliPath = Path.Combine(basePath, "package", "cli.js");

		// Add quotes for Windows paths
		if (!OperatingSystem.IsMacOS()) {
			nodePath = $"\"{nodePath}\"";
			cliPath = $"\"{cliPath}\"";
		}

		return (nodePath, cliPath);
	}

	// Helper method to get Playwright cache directory
	private static string GetPlaywrightCacheDir() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "ms-playwright")
			: Path.Combine(
					Environment.GetEnvironmentVariable("HOME") ?? "~",
					RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Library/Caches" : ".cache",
					"ms-playwright"
			);

	// Thread-safe singleton implementation
	private static readonly Lazy<PlaywrightCookiesRepo> _instance = new(() => new PlaywrightCookiesRepo(), LazyThreadSafetyMode.ExecutionAndPublication);
	public static PlaywrightCookiesRepo Instance => _instance.Value;
	// ----------------------------
}