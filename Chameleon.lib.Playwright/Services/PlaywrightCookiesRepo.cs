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
	private readonly List<ApiObject<ObjectsCookies<BrowserContextCookiesResult>>> _cookiesCache = [];

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

	// Uploads Chromium cookies to server
	public async Task PutChromiumCookies(string userId, string profileId, Enums.SystemBrowserType browserType)
	{
		Toaster.ShowInf("Sending cookies...");

		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var playwrightBrowser = browserType == Enums.SystemBrowserType.Firefox
		? playwright.Firefox
		: playwright.Chromium;
		await using var context = await playwrightBrowser.LaunchPersistentContextAsync(
				Path.Combine(Consts.AppDataLocalDir, browserType.ToString(), profileId),
				new() {
					Headless = true,
					ExecutablePath = await GetExecutable(browserType),
					Args = ["--allow-downgrade"]
				}
		);

		var cookies = await context.CookiesAsync();
		await context.CloseAsync();

		if (cookies.Any()) {
			_ = await _abService.AddCookiesAsync(userId, new { profileId, cookies });
		}

		Toaster.ShowSuccess("Cookies sent successfully");
	}

	// Retrieves cookies from server
	public async Task<bool> GetCookies()
	{
		_cookiesCache.Clear();

		try {
			var result = (await _abService.GetCookiesAsync<BrowserContextCookiesResult>())
					?? throw new InvalidOperationException("Response is unreadable");
			_cookiesCache.AddRange(result.Objects);
		} catch {
			Console.WriteLine("Failed to get cookies");
		}

		return _cookiesCache.Count != 0;
	}

	// Syncs cookies to browser
	public async Task SyncCookies(Enums.SystemBrowserType browserType)
	{
		if (!await GetCookies()) return;

		// Retrieve path to the browser executable
		var exePath = await GetExecutable(browserType);

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

			// Add the cookies to the context
			await using var context = await playwrightBrowser.LaunchPersistentContextAsync(
					IOtil.EnsureDirectoryExists(Path.Combine(Consts.AppDataLocalDir, browserType.ToString(), cookieData.Data.ProfileId!)),
					new() {
						Headless = true,
						ExecutablePath = exePath,
						Args = ["--allow-downgrade"]
					}
			);
			await context.AddCookiesAsync(
				cookieData.Data.Cookies!.Select(c => 
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

				await _abService.DeleteCookieAsync(cookie.Id);
				_cookiesCache.RemoveAt(i);
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

	private static async Task<string> GetExecutable(Enums.SystemBrowserType browserType)
	{
		return browserType == Enums.SystemBrowserType.Firefox
				? await InstallPlaywrightsFirefoxIfNecessary() ?? throw new InvalidOperationException("Failed to install Playwright's Firefox")
				: SysBrowserInfoUtil.FindByType(browserType).Path;
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