using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.RegularExpressions;

using Chameleon.lib.Common.Constants;

using Chameleon.lib.Common.Util;
using Chameleon.lib.Helpers;
using Microsoft.Playwright;
using Chameleon.lib.Common.Extensions;

namespace Chameleon.lib.Playwright;

/// <summary>
/// Helper/Util class for static Playwright operations
/// </summary>
public static class PlaywrightUtil {
	public static async Task<IReadOnlyList<BrowserContextCookiesResult>> GetCookies(string profileId, Enums.SystemBrowserType browserType, Func<Enums.SystemBrowserType,Task>? openBrowserProfile = null, Func<Process?>? getBrowserProfileProcess = null) {
		// Retrieve path to the browser executable
		var exePath = await GetExecutable(browserType);

		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var playwrightBrowser = browserType == Enums.SystemBrowserType.Firefox
		? playwright.Firefox : playwright.Chromium;

		var context = await GetContextAsync(profileId, browserType, exePath, playwrightBrowser, 0, openBrowserProfile, getBrowserProfileProcess);

		if (context is null) return [];

		var cookies = await context.CookiesAsync();
		await context.CloseAsync();

		return cookies;
	}

	private static async Task<IBrowserContext> GetContextAsync(string profileId, Enums.SystemBrowserType browserType, string exePath, IBrowserType playwrightBrowser, int tries, Func<Enums.SystemBrowserType, Task>? openBrowserProfile = null, Func<Process?>? getBrowserProfileProcess = null) {
		try {
			var context = tries switch {
				0 => await GetNewContextAsync(profileId, browserType, exePath, playwrightBrowser),
				1 => await ConnectToProfileContextAsync(profileId, browserType, exePath, playwrightBrowser, getBrowserProfileProcess),
				_ => await CloseChromeAndOpenAsync(profileId, browserType, exePath, playwrightBrowser)
			};

			if (tries > 1 &&  openBrowserProfile is not null) {
				await openBrowserProfile(browserType);
			}
			return context;
		} catch (Exception ex) when (ex.Message.Contains("Target page, context or browser has been closed") && tries < 3) {
			return await GetContextAsync(profileId, browserType, exePath, playwrightBrowser, ++tries, openBrowserProfile, getBrowserProfileProcess);
		}
	}

	private static async Task<IBrowserContext> GetNewContextAsync(string profileId, Enums.SystemBrowserType browserType, string exePath, IBrowserType playwrightBrowser) {
		var context = await playwrightBrowser.LaunchPersistentContextAsync(
						Path.Combine(Consts.AppDataLocalDir, browserType.ToString(), profileId),
						new() {
							Headless = true,
							ExecutablePath = exePath,
							Args = ["--allow-downgrade"]
						}
				);
		return context;
	}

	private static async Task<IBrowserContext> ConnectToProfileContextAsync(string profileId, Enums.SystemBrowserType browserType, string exePath, IBrowserType playwrightBrowser, Func<Process?>? getBrowserProfileProcess = null) {

		if (getBrowserProfileProcess is null)
			return await GetNewContextAsync(profileId, browserType, exePath, playwrightBrowser);

		var process = getBrowserProfileProcess();
		if (process is null)
			return await GetNewContextAsync(profileId, browserType, exePath, playwrightBrowser);

		var commandLineArguments = process.GetProcessCommandLine();
		if (!string.IsNullOrEmpty(commandLineArguments)) {

			var argPrefix = "--remote-debugging-port=";
			var index = commandLineArguments.IndexOf(argPrefix, StringComparison.OrdinalIgnoreCase);
			if (index < 0)
				return await GetNewContextAsync(profileId, browserType, exePath, playwrightBrowser);

			var start = index + argPrefix.Length;
			var end = commandLineArguments.IndexOf(' ', start);
			if (end < 0) end = commandLineArguments.Length;

			var portStr = commandLineArguments[start..end];
			if (int.TryParse(portStr, out var port)) {
				var browser = await playwrightBrowser.ConnectOverCDPAsync($"http://localhost:{port}");
				return browser.Contexts[0];
			}
		}

		return await GetNewContextAsync(profileId, browserType, exePath, playwrightBrowser);
	}

	private static async Task<IBrowserContext> CloseChromeAndOpenAsync(string profileId, Enums.SystemBrowserType browserType, string exePath, IBrowserType playwrightBrowser) {
		ChromeProcessExtensions.CloseAllChrome();
		return await GetNewContextAsync(profileId, browserType, exePath, playwrightBrowser);
	}

	public static async Task CreateDevmodePrefs(Enums.SystemBrowserType browserType, string profileId) {
		var cachePath = Path.Combine(Consts.AppDataLocalDir, browserType.ToString(), profileId);
		var prefsFile = Path.Combine(cachePath, "Default", "Preferences");

		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();

		try {
			await using var context = await playwright.Chromium.LaunchPersistentContextAsync(
								cachePath,
								new() {
									Headless = false,
									ExecutablePath = await PlaywrightUtil.GetExecutable(browserType),
									Args = ["--allow-downgrade"]
								}
				);
			var page = await context.NewPageAsync();

			//_ = await page.GotoAsync("example.com");
			await page.CloseAsync();
			await context.CloseAsync();
		} catch (Exception ex) {

		}

		while (!File.Exists(prefsFile))
			await Task.Delay(1000);

		if (File.Exists(prefsFile)) {
			var document = JsonDocument.Parse(await File.ReadAllTextAsync(prefsFile));
			var root = document.RootElement.Clone();

			// Convert the root element to a JsonObject
			var mutableRoot = JsonNode.Parse(root.GetRawText())?.AsObject();
			if (mutableRoot != null) {
				if (mutableRoot["extensions"] is JsonObject extensions) {
					if (extensions["ui"] is JsonObject ui) {
						ui["developer_mode"] = true;
					} else {
						extensions["ui"] = new JsonObject {
							["developer_mode"] = true
						};
					}
				} else {
					mutableRoot["extensions"] = new JsonObject {
						["ui"] = new JsonObject {
							["developer_mode"] = true
						}
					};
				}
			}

			// Serialize the modified JsonObject back to JSON
			var modifiedJson = JsonSerializer.Serialize(mutableRoot);
			await File.WriteAllTextAsync(prefsFile, modifiedJson);
		}
	}

	public static async Task<string> GetExecutable(Enums.SystemBrowserType browserType) {
		return browserType == Enums.SystemBrowserType.Firefox
				? await InstallPlaywrightsFirefoxIfNecessary() ?? throw new InvalidOperationException("Failed to install Playwright's Firefox")
				: SysBrowserInfoUtil.FindByType(browserType).Path;
	}

	// Installs Playwright's Firefox if not already installed
	public static async Task<string?> InstallPlaywrightsFirefoxIfNecessary() {
		// 1) Check if it is already installed
		var existingPath = FindPlaywrightFirefox();
		if (existingPath != null) {
			return existingPath;
		}

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
				if (!string.IsNullOrEmpty(e.Data)) {
					Toaster.Error($"[Firefox Sync Update Install/Error]: {e.Data}");
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
	public static string? FindPlaywrightFirefox() {
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
