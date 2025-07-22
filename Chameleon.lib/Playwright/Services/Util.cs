using Chameleon.lib.Helpers;
using Chameleon.lib.Util;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Chameleon.lib.Playwright.Services;

/// <summary>
/// Helper/Util class for static Playwright operations
/// </summary>
public static class Util {
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
				await IO.CopyDirectory(chromiumDefaultDirOriginal, tempChromiumDefaultDir);
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

	public static async Task<string> GetBrowseExecutablePath(Browzio.BrowserType browserType) {
		return browserType == Browzio.BrowserType.Firefox
				? await InstallPlaywrightsFirefoxIfNecessary() ?? throw new InvalidOperationException("Failed to install Firefox")
				: lib.Browzio.Browzio.Utilities.GetBrowser(browserType)?.ExecutablePath ??
				  throw new InvalidOperationException("Browser executable path not found.");
	}

	// Installs Playwright's Firefox if not already installed
	public static async Task<string?> InstallPlaywrightsFirefoxIfNecessary() {
		// 1) Check if it is already installed
		var existingPath = FindPlaywrightFirefox();
		if (existingPath != null) return existingPath;

		try {
			Toaster.Info("Installing Firefox Sync Update...");
			using var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = Project.Plugins.Node,
					Arguments = $"{Project.Plugins.CLI} install firefox",
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
		var cacheDir = OperatingSystem.IsWindows()
			? Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "ms-playwright")
			: Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "~", "Library", "Caches", "ms-playwright");
		if (
			!Directory.Exists(cacheDir) ||
			Directory.GetDirectories(cacheDir, "firefox-*", SearchOption.TopDirectoryOnly)
				.OrderByDescending(d => d)
				.FirstOrDefault() is not { } firefoxDir) return null;
		var file = Path.Combine(firefoxDir, "firefox", OperatingSystem.IsWindows() ? "firefox.exe" : "Nightly.app/Contents/MacOS/firefox");
		return File.Exists(file) ? file : null;
	}
}
