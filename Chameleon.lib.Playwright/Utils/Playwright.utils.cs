using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Chameleon.lib.Common.Util;
using Chameleon.lib.Helpers;
using Chameleon.lib.Playwright.Models;
using Microsoft.Playwright;
using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.Playwright.Utils;

/// <summary>
/// Helper/Util class for static Playwright operations
/// </summary>
public static class PlaywrightUtil {
	public static Task<IReadOnlyList<BrowserContextCookiesResult>> GetCookies(GetCookiesOptions options) =>
		GetCookiesWithRetryPolicy(options);

	private static async Task<IReadOnlyList<BrowserContextCookiesResult>> GetCookiesWithRetryPolicy(GetCookiesOptions options, int tries = 0) {
		try {
			return tries switch {
				0 => await GetCookiesAsync(options),
				1 => await GetCookiesAsync(new(options.Browser, null)),
				_ => throw new InvalidOperationException("Failed to connect a browser context check your currently running browsers and try again"),//TODO: instead of warn ChromeProcessExtensions.CloseAllChrome();
			};
		} catch (Exception ex) when (ex.Message.Contains("Target page, context or browser has been closed") && tries < 3) {
			return await GetCookiesWithRetryPolicy(options, ++tries);
		}
	}

	private static async Task<IReadOnlyList<BrowserContextCookiesResult>> GetCookiesAsync(GetCookiesOptions options) {
		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var playwrightBrowser = options.Browser.BrowserType == SystemBrowserType.Firefox ? playwright.Firefox : playwright.Chromium;
		var context = options.Port != null ? (await playwrightBrowser.ConnectOverCDPAsync($"http://localhost:{options.Port}")).Contexts[0]
		 : await playwrightBrowser.LaunchPersistentContextAsync(
				options.Dir,
				new() {
					Headless = true,
					Args = ["--allow-downgrade"],
					Proxy = options.Proxy,
					ExecutablePath = await GetBrowseExecutablePath(options.Browser.BrowserType),
				}
		);
		return await context.CookiesAsync();
	}

	public static async Task<string> GetBrowseExecutablePath(SystemBrowserType browserType) {
		return browserType == SystemBrowserType.Firefox
				? await InstallPlaywrightsFirefoxIfNecessary() ?? throw new InvalidOperationException("Failed to install Playwright's Firefox")
				: SysBrowserInfoUtil.FindByType(browserType).Path;
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
						$"{Regex.Replace(e.Data.Replace("â–", ""), @"\s+", " ").Trim()}");
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
