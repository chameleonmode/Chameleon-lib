using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Chameleon.lib.Helpers;
using Chameleon.lib.WebBrowser;
using Chameleon.lib.WebBrowser.Services;
using Chameleon.lib.Util; // Added for FilePaths
using Microsoft.Playwright;


namespace Chameleon.lib.Playwright.Services;

/// <summary>
/// Helper/Util class for static Playwright operations
/// </summary>
public static class Util {
	public static Task<IReadOnlyList<BrowserContextCookiesResult>> GetCookies(Options options) =>
		GetCookiesWithRetryPolicy(options);
	private static async Task<IReadOnlyList<BrowserContextCookiesResult>> GetCookiesWithRetryPolicy(Options options, int tries = 0) {
		try {
			return tries switch {
				0 => await GetCookiesAsync(options),
				1 when options.Port != null => await GetCookiesAsync(new(options.Browser, null)),
				2 when options.Port == null => await GetCookiesAsync(options),
				_ => throw new InvalidOperationException("Failed to connect to browser context"),
			};
		} catch (Exception ex) when ((ex.Message.Contains("Target page, context or browser has been closed") ||
																	 ex.Message.Contains("Connection closed") ||
																	 ex.Message.Contains("Browser has been closed") ||
																	 ex.Message.Contains("Protocol error") ||
																	 ex.Message.Contains("WebSocket") ||
																	 ex.Message.Contains("net::ERR_CONNECTION_REFUSED")) && tries < 3) {
			// Add progressive delay before retrying to allow any pending operations to complete
			await Task.Delay(500 * (tries + 1));
			return await GetCookiesWithRetryPolicy(options, ++tries);
		}
	}
	private static async Task<IReadOnlyList<BrowserContextCookiesResult>> GetCookiesAsync(Options options) {
		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var playwrightBrowser = options.Browser.BrowserType == SystemBrowserType.Firefox ? playwright.Firefox : playwright.Chromium; 
		
		if (options.Port != null) {
			try {
				await using var browser = await playwrightBrowser.ConnectOverCDPAsync($"http://localhost:{options.Port}");

				var context = browser.Contexts.Count > 0 ? browser.Contexts[0] : await browser.NewContextAsync();
				return await context.CookiesAsync();
			} catch (Exception ex) {
				throw new InvalidOperationException($"Failed to connect to browser on port {options.Port}. " +
					$"Ensure the browser is running with remote debugging enabled. Error: {ex.Message}", ex);
			}
		} else {
			var userProfileActualDir = options.Dir;
			if (string.IsNullOrEmpty(userProfileActualDir) || !Directory.Exists(userProfileActualDir))
			{
				// Consider logging this error. If options.Dir is not set, cookies will be empty.
				// This case should ideally be prevented by ensuring options.Dir is always populated.
				Console.WriteLine($"Error: User profile directory 'options.Dir' is not set or does not exist: {userProfileActualDir}");
				return new List<BrowserContextCookiesResult>(); // Return empty list
			}

			var tempDir = Path.Combine(Path.GetTempPath(), "chameleon-cookie-temp", Guid.NewGuid().ToString());
			try {
				_ = Directory.CreateDirectory(tempDir);
				 
				if (options.Browser.BrowserType == SystemBrowserType.Firefox)
				{
					// For Firefox, Playwright expects cookies.sqlite at the root of the userDataDir.
					var originalCookieFile = Path.Combine(userProfileActualDir, "cookies.sqlite");
					if (File.Exists(originalCookieFile))
					{
						File.Copy(originalCookieFile, Path.Combine(tempDir, "cookies.sqlite"), true);
					}
					// Optionally, copy other essential files like prefs.js if they prove necessary.
				}
				else // Chromium-based (Chrome, Brave, Edge, etc.)
				{
					// For Chromium, copy the entire "Default" directory to make the temp profile more complete.
					// Playwright expects Default/Network/Cookies within the userDataDir.
					var chromiumDefaultDirOriginal = Path.Combine(userProfileActualDir, "Default");
					var tempChromiumDefaultDir = Path.Combine(tempDir, "Default");
					if (Directory.Exists(chromiumDefaultDirOriginal))
					{
						CopyDirectory(chromiumDefaultDirOriginal, tempChromiumDefaultDir, true);
					}
					else
					{
					    // If the Default directory doesn't exist, create the structure for the cookie file path
					    // to avoid errors if we later try to copy just the cookie file to a non-existent Default/Network.
					    var tempNetworkDir = Path.Combine(tempDir, "Default", "Network");
                        _ = Directory.CreateDirectory(tempNetworkDir);
					    // And attempt to copy the specific cookie file if the Default dir was missing but the cookie file might exist at its expected path.
					    var originalCookieFile = Path.Combine(userProfileActualDir, "Default", "Network", "Cookies");
					    if(File.Exists(originalCookieFile))
					    {
					        File.Copy(originalCookieFile, Path.Combine(tempNetworkDir, "Cookies"), true);
					    }
					}
				}

				await using var context = await playwrightBrowser.LaunchPersistentContextAsync(
					tempDir,
					new() {
						Headless = true,
						Args = ["--allow-downgrade"],
						Proxy = options.Proxy,
						ExecutablePath = await GetBrowseExecutablePath(options.Browser.BrowserType),
					}
				);
				var cookies = await context.CookiesAsync();
				Console.WriteLine($"Found {cookies.Count} cookies in Util.cs for profile {options.Dir}"); // DEBUG LOG
				return cookies;
			} finally {
				try {
					if (Directory.Exists(tempDir)) {
						Directory.Delete(tempDir, true);
					}
				} catch (Exception ex) {
					Console.WriteLine($"Error cleaning up temp directory {tempDir}: {ex.Message}"); // DEBUG LOG
					// Ignore cleanup errors
				}
			}
		}
	}

	public static async Task<string> GetBrowseExecutablePath(SystemBrowserType browserType) {
		return browserType == SystemBrowserType.Firefox
				? await InstallPlaywrightsFirefoxIfNecessary() ?? throw new InvalidOperationException("Failed to install Playwright's Firefox")
				: SysBrowserInfoUtil.Find(browserType).Path;
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

	// Helper method to recursively copy a directory
	private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
	{
		var dir = new DirectoryInfo(sourceDir);
		if (!dir.Exists) return; // Changed: If source doesn't exist, just return to avoid exception if Default dir is missing.

		DirectoryInfo[] dirs = dir.GetDirectories();
		Directory.CreateDirectory(destinationDir);

		foreach (FileInfo file in dir.GetFiles())
		{
			string targetFilePath = Path.Combine(destinationDir, file.Name);
			try { file.CopyTo(targetFilePath, true); }
			catch (IOException ex) {
			    Console.WriteLine($"IOException copying {file.FullName} to {targetFilePath}: {ex.Message}. File might be locked.");
			    // Decide if you want to continue or re-throw. For cookie export, maybe continue with what could be copied.
			}
		}

		if (recursive)
		{
			foreach (DirectoryInfo subDir in dirs)
			{
				string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
				CopyDirectory(subDir.FullName, newDestinationDir, true);
			}
		}
	}
}
