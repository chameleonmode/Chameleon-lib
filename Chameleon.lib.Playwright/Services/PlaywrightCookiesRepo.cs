using System.Diagnostics;
using System.Runtime.InteropServices;

using Chameleon.lib.Abs;
using Chameleon.lib.Api;
using Chameleon.lib.Common;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.ServiceManagers;
using Chameleon.lib.Common.Util;

using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Services;
public class PlaywrightCookiesRepo {
	// Lazy-loaded singleton
	private static readonly Lazy<PlaywrightCookiesRepo> _instance = new(() => new PlaywrightCookiesRepo());
	public static PlaywrightCookiesRepo Instance => _instance.Value;
	// ------------------------

	private readonly ABService _abService = ABService.Instance;
	public List<BaseObject<CookieObject<BrowserContextCookiesResult>>> CookiesCache { get; } = [];

	private PlaywrightCookiesRepo()
	{
		_abService.SetLoaders(
				() => Tuple.Create(
						Auther.AuthSession!.UserId,
						Auther.AuthSession!.UserName!,
						Auther.AuthSession!.LicenseKey!,
						Auther.AuthSession!.CreatorUserId
				)
		);
	}
	public async Task CheckAuthenticated()
	{
		if(!_abService.IsAuthenticated) {
			var token = await _abService.GetTokenAsync() 
				?? throw new Exception("Failed to activate permissions for cookies sync");

			//IoC.SetValue(token, IoCKeys.TokenObject);
		}
	}

	public async Task PutChromiumCookies(string userId, string profileId, Enums.SystemBrowserType browserType)
	{
		await CheckAuthenticated();

		using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(
			Path.Combine(Consts.AppDataLocalDir, browserType.ToString(), profileId)
			, new() {
				Headless = true,
				ExecutablePath = SysBrowserInfoUtil.FindByType(browserType).Path
			}
		);
		var cookies = await browserContext.CookiesAsync();
		await browserContext.CloseAsync();
		if (cookies.Any()) {
			_ = await _abService.AddCookiesAsync(
				userId
				, new { profileId, cookies }
			);
			Toaster.ShowSuccess("Sent");
		} else {
			Toaster.ShowInf("No cookies found to upload");
		}
	}

	public async Task GetCookiesAsync()
	{
		await CheckAuthenticated();

		var results = (await _abService.GetCookiesAsync<BrowserContextCookiesResult>())?.Data;
		ArgumentNullException.ThrowIfNull(results, "Response is unreadable");

		CookiesCache.Clear();
		CookiesCache.AddRange(results);
	}

	public async Task<bool> HasCookies()
	{
		await GetCookiesAsync();
		return CookiesCache.Count > 0;
	}

	public async Task SyncCookies(Enums.SystemBrowserType browserType, bool delete = true)
	{
		//check if there are cookies to load from response
		if (!await HasCookies()) {
			return;
		}

		//
		Exception? anyEx = null;

		// get actual exe path incase gecko type is used 
		var exePath = SysBrowserInfoUtil.FindByType(browserType).Path;
		if (browserType == Enums.SystemBrowserType.Firefox) {
			//C:\repos\Chameleon\Chameleon.Avalonia\src\Chameleon.Avalonia.Desktop\obj\outwin\.playwright\node\win32_x64\node.exe C:\repos\Chameleon\Chameleon.Avalonia\src\Chameleon.Avalonia.Desktop\obj\outwin\.playwright\package\cli.js install firefox
			exePath = await InstallPlaywrightsFirefoxIfNecessary();
			ArgumentNullException.ThrowIfNull(exePath, "Failed to install playwrights firefox");
		}

		//add loop to add cookies to playwright context
		for (var i = CookiesCache.Count - 1; i >= 0; i--) {
			var cookies = CookiesCache[i];
			var pcookies = new List<Microsoft.Playwright.Cookie>();
			foreach (var cookie in cookies.Data.Cookies!) {
				pcookies.Add(new Microsoft.Playwright.Cookie {
					Domain = cookie.Domain,
					Expires = cookie.Expires,
					HttpOnly = cookie.HttpOnly,
					Name = cookie.Name,
					Path = cookie.Path,
					SameSite = cookie.SameSite,
					Secure = cookie.Secure,
					Value = cookie.Value
				});
			}
			if (cookies.Data.ProfileId is null || !cookies.Data.ProfileId.Is()) {
				continue;
			}

			// show toaster for starting sync out of items left to sync
			Toaster.ShowInf($"Syncing ... {i} left");

			// sync cookies
			try {
				using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
				var playwrightType = browserType == Enums.SystemBrowserType.Firefox ? playwright.Firefox : playwright.Chromium;
				var browserContext = await playwrightType.LaunchPersistentContextAsync(
					Path.Combine(Consts.AppDataLocalDir, browserType.ToString(), cookies.Data.ProfileId!)
					, new() {
						Headless = true,
						ExecutablePath = exePath
					}
				);
				await browserContext.AddCookiesAsync(pcookies);
				await browserContext.CloseAsync();
			} catch (Exception e) {
				Console.WriteLine(e.Message);
				anyEx = e;
				continue;
			} finally {
				if (delete) {
					var deleted = await _abService.DeleteCookieAsync(cookies.Id);
					if (deleted) {
						CookiesCache.RemoveAt(i);
					}
				}
			}
		}

		if (anyEx != null) {
			throw anyEx;
		}
	}

	public async Task<string?> InstallPlaywrightsFirefoxIfNecessary()
	{
		var dirpath = IsPlaywrightFirefoxInstalled();
		try {
			// 1) Check if Firefox is already installed in the Playwright cache.
			if (dirpath != null) {
				Console.WriteLine("Playwright Firefox is already installed.");
				return dirpath;
			}

			Console.WriteLine("Playwright Firefox not found; proceeding to install...");

			// 2) Dynamically set up our base path (where .playwright is located).
			//    On macOS: ../Resources/.playwright
			//    On Windows: .playwright (relative to the current directory).
			var basePath = AppDomain.CurrentDomain.BaseDirectory;
			basePath = Path.Combine(basePath, OperatingSystem.IsMacOS()
					? "../Resources/.playwright"
					: ".playwright");

			// 3) Construct the node binary path for macOS vs. Windows.
			//    - macOS => node/darwin-x64/node
			//    - Windows => node\win32_x64\node.exe
			//    Then optionally wrap it in quotes on Windows.
			var nodePath = Path.Combine(
					basePath,
					OperatingSystem.IsMacOS()
							? "node/darwin-x64/node"
							: "node\\win32_x64\\node.exe"
			);

			if (!OperatingSystem.IsMacOS())
				nodePath = @$"""{nodePath}""";

			// 4) Build the path to the Playwright CLI script: package\cli.js
			//    Then optionally wrap it in quotes on Windows.
			var playwrightCliPath = Path.Combine(
					basePath,
					OperatingSystem.IsMacOS()
							? "package/cli.js"
							: "package\\cli.js"
			);

			if (!OperatingSystem.IsMacOS())
				playwrightCliPath = @$"""{playwrightCliPath}""";

			// 5) We want to run: node <index.js> install firefox
			//    So the arguments will be something like: "<path-to-index.js> install firefox"
			var arguments = $"{playwrightCliPath} install firefox";

			// 6) Create our process start info. We’ll capture stdout/stderr for logging.
			var startInfo = new ProcessStartInfo {
				FileName = nodePath,
				Arguments = arguments,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
				// WorkingDirectory = Path.GetDirectoryName(nodePath) // optional
			};

			var _nodeProcess = new Process { StartInfo = startInfo };
			_nodeProcess.Start();

			// 7) (Optional) read output or just wait:
			//var stdout = _nodeProcess.StandardOutput.ReadToEnd();
			//var stderr = _nodeProcess.StandardError.ReadToEnd();

			// 8) Check results
			//if (!string.IsNullOrEmpty(stdout)) Console.WriteLine("STDOUT:\n" + stdout);
			//if (!string.IsNullOrEmpty(stderr)) Console.WriteLine("STDERR:\n" + stderr);

			await _nodeProcess.WaitForExitAsync();
			if (_nodeProcess.ExitCode == 0) {
				Console.WriteLine("Playwright Firefox installation succeeded.");
			} else {
				Console.WriteLine($"Playwright Firefox installation failed with ExitCode={_nodeProcess.ExitCode}.");
			}
		} catch (Exception ex) {
			Console.WriteLine("Exception: " + ex);
		}

		return IsPlaywrightFirefoxInstalled();
	}

	/// <summary>
	/// Checks if a folder named "firefox-XXXXXX" is present in the default
	/// Playwright cache directory (indicating Firefox is installed).
	/// </summary>
	private static string? IsPlaywrightFirefoxInstalled()
	{
		var cacheDir = GetPlaywrightCacheDir();
		if (!Directory.Exists(cacheDir))
			return null;

    // Look for the latest directory that starts with "firefox-"
    var firefoxDirs = Directory.GetDirectories(cacheDir, "firefox-*", SearchOption.TopDirectoryOnly);
		return firefoxDirs.Length == 0 || firefoxDirs.OrderByDescending(d => d).FirstOrDefault() is not string directory
			? null
			: Path.Combine(directory, "firefox", "firefox.exe");
	}

	/// <summary>
	/// Returns the OS-specific default Playwright cache path.
	/// </summary>
	private static string GetPlaywrightCacheDir()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			// e.g. C:\Users\USER\AppData\Local\ms-playwright
			var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
			return Path.Combine(localAppData ?? "", "ms-playwright");
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
			// e.g. /Users/USER/Library/Caches/ms-playwright
			var home = Environment.GetEnvironmentVariable("HOME") ?? "~";
			return Path.Combine(home, "Library", "Caches", "ms-playwright");
		} else {
			// e.g. /home/USER/.cache/ms-playwright
			var home = Environment.GetEnvironmentVariable("HOME") ?? "~";
			return Path.Combine(home, ".cache", "ms-playwright");
		}
	}

	/// <summary>
	/// Returns a base path for your application. You can implement it as:
	///   Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
	/// or you may have a different approach depending on your runtime.
	/// </summary>
	private static string GetBaseAppPath()
	{
		// Example: get the directory of the current executable.
		// Adjust as needed for your environment.
		return Path.GetDirectoryName(
				System.Reflection.Assembly.GetExecutingAssembly().Location
		) ?? Environment.CurrentDirectory;
	}

	/// <summary>
	/// Returns the full path to the correct Node binary for the current OS in
	/// the .playwright\node\ subfolder. On Windows, node.exe; on other OS, node.
	/// If your directory structure differs, adjust accordingly.
	/// 
	/// Example structure:
	///  .playwright
	///     └─ node
	///        ├─ win32_x64
	///        │   └─ node.exe
	///        ├─ macos_x64
	///        │   └─ node
	///        ├─ linux_x64
	///        │   └─ node
	/// </summary>
	private static string GetNodeBinaryPath(string baseAppPath)
	{
		// Simple example mapping (you may need to handle ARM, etc.)
		string osSubfolder;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			osSubfolder = "win32_x64";
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			osSubfolder = "macos_x64";
		else
			osSubfolder = "linux_x64";

		var nodeFilename = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? "node.exe"
				: "node";

		return Path.Combine(baseAppPath, ".playwright", "node", osSubfolder, nodeFilename);
	}
}

