using Chameleon.lib.Browzio;
using Chameleon.lib.Helpers;
using Chameleon.lib.Util;
using Microsoft.Playwright;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Chameleon.lib.Playwright.Services;

/// <summary>
/// Helper/Util class for static Playwright operations
/// </summary>
public static class Util {
	public static Task<IReadOnlyList<BrowserContextCookiesResult>> GetCookies(Options options)
		=> ExecuteWithRetryPolicyAsync((_) => ExecuteCookieAction(options));

	public static Task<IReadOnlyList<BrowserContextCookiesResult>> SetCookies(Options options, IEnumerable<Cookie> cookies)
		=> ExecuteWithRetryPolicyAsync((_) => ExecuteCookieAction(options, [.. cookies]));

	private static async Task<IReadOnlyList<BrowserContextCookiesResult>> ExecuteCookieAction(Options options, List<Cookie>? cookiesToSet = null) {
		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var playwrightBrowser = options.Browser.BrowserType == Browzio.BrowserType.Firefox ? playwright.Firefox : playwright.Chromium;

		if (options.Port != null) {
			try {
				await using var browser = await playwrightBrowser.ConnectOverCDPAsync($"http://localhost:{options.Port}");
				var context = browser.Contexts.Count > 0 ? browser.Contexts[0] : await browser.NewContextAsync();
				if (cookiesToSet == null) {
					return await context.CookiesAsync();
				} else {
					await context.AddCookiesAsync(cookiesToSet);
					return [];
				}
			} catch (Exception ex) {
				throw new InvalidOperationException($"Failed to connect to browser on port {options.Port}. " +
						$"Ensure the browser is running with remote debugging enabled. Error: {ex.Message}", ex);
			}
		} else {
			var userProfileActualDir = options.Dir;
			if (string.IsNullOrEmpty(userProfileActualDir) || !Directory.Exists(userProfileActualDir)) {
				Debug.WriteLine($"Error: User profile directory 'options.Dir' is not set or does not exist: {userProfileActualDir}");
				return new List<BrowserContextCookiesResult>();
			}

			var tempDir = Path.Combine(Path.GetTempPath(), "chameleon-cookie-temp", Guid.NewGuid().ToString());
			try {
				_ = Directory.CreateDirectory(tempDir);
				await CopyProfileDataToTempDir(options, userProfileActualDir, tempDir);

				await using var context = await playwrightBrowser.LaunchPersistentContextAsync(
						tempDir,
						new() {
							Headless = true,
							Args = ["--allow-downgrade"],
							Proxy = options.Proxy,
							ExecutablePath = await GetBrowseExecutablePath(options.Browser.BrowserType),
						}
				);

				if (cookiesToSet == null) {
					var cookies = await context.CookiesAsync();
					Debug.WriteLine($"Found {cookies.Count} cookies in Util.cs for profile {options.Dir}");
					return cookies;
				} else {
					await context.AddCookiesAsync(cookiesToSet);
					await context.CloseAsync();
					return Array.Empty<BrowserContextCookiesResult>();
				}
			} finally {
				try {
					if (Directory.Exists(tempDir)) {
						Directory.Delete(tempDir, true);
					}
				} catch (Exception ex) {
					Debug.WriteLine($"Error cleaning up temp directory {tempDir}: {ex.Message}");
				}
			}
		}
	}

	private static async Task CopyProfileDataToTempDir(Options options, string userProfileActualDir, string tempDir) {
		if (options.Browser.BrowserType == Browzio.BrowserType.Firefox) {
			var originalCookieFile = Path.Combine(userProfileActualDir, "cookies.sqlite");
			if (File.Exists(originalCookieFile)) {
        File.Copy(originalCookieFile, Path.Combine(tempDir, "cookies.sqlite"), true);
			}
		} else {
			var chromiumDefaultDirOriginal = Path.Combine(userProfileActualDir, "Default");
			var tempChromiumDefaultDir = Path.Combine(tempDir, "Default");
			if (Directory.Exists(chromiumDefaultDirOriginal)) {
				await IOU.CopyDirectory(chromiumDefaultDirOriginal, tempChromiumDefaultDir);
			} else {
				var tempNetworkDir = Path.Combine(tempDir, "Default", "Network");
				_ = Directory.CreateDirectory(tempNetworkDir);
				var originalCookieFile = Path.Combine(userProfileActualDir, "Default", "Network", "Cookies");
				if (File.Exists(originalCookieFile)) {
          File.Copy(originalCookieFile, Path.Combine(tempNetworkDir, "Cookies"), true);
				}
			}
		}
	}

	private static bool IsPlaywrightException(Exception ex) =>
		ex.Message.Contains("Target page, context or browser has been closed") ||
		ex.Message.Contains("Connection closed") ||
		ex.Message.Contains("Browser has been closed") ||
		ex.Message.Contains("Protocol error") ||
		ex.Message.Contains("WebSocket") ||
		ex.Message.Contains("net::ERR_CONNECTION_REFUSED");

	private static async Task<T> ExecuteWithRetryPolicyAsync<T>(Func<int, Task<T>> action, int tries = 0) {
		try {
			return await action(tries);
		} catch (Exception ex) when (IsPlaywrightException(ex) && tries < 3) {
			await Task.Delay(500 * (tries + 1));
			return await ExecuteWithRetryPolicyAsync(action, ++tries);
		}
	}

	public static async Task<string> GetBrowseExecutablePath(Browzio.BrowserType browserType) {
		return browserType == Browzio.BrowserType.Firefox
				? await InstallPlaywrightsFirefoxIfNecessary() ?? throw new InvalidOperationException("Failed to install Playwright's Firefox")
				: BrowserInfo.Find(browserType).Path;
	}

	// Installs Playwright's Firefox if not already installed
	public static async Task<string?> InstallPlaywrightsFirefoxIfNecessary() {
		// 1) Check if it is already installed
		var existingPath = FindPlaywrightFirefox();
		if (existingPath != null) return existingPath;

		try {
			Toaster.Info("Installing Firefox Sync Update...");

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
					Toaster.Info($"[Installing Firefox Sync Update...]: " +
						$"{Regex.Replace(e.Data.Replace("â– ", ""), @"\s+", " ").Trim()}");
				}
			};
			process.ErrorDataReceived += (sender, e) => {
				if (!string.IsNullOrEmpty(e.Data)) Toaster.Error($"[Firefox Sync Update Install/Error]: {e.Data}");
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
	public static string? FindPlaywrightFirefox() {
		var cacheDir = GetPlaywrightCacheDir();
		if (!Directory.Exists(cacheDir)) return null;

		var firefoxDir = Directory
				.GetDirectories(cacheDir, "firefox-*", SearchOption.TopDirectoryOnly)
				.OrderByDescending(d => d)
				.FirstOrDefault();

		return firefoxDir == null ? null : Path.Combine(firefoxDir, "firefox", "firefox.exe");
	}

	// Gets Playwright paths based on OS
	public static (string NodePath, string CliPath) GetPlaywrightPaths() {
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
	public static string GetPlaywrightCacheDir() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "ms-playwright")
			: Path.Combine(
					Environment.GetEnvironmentVariable("HOME") ?? "~",
					RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Library/Caches" : ".cache",
					"ms-playwright"
			);
}
