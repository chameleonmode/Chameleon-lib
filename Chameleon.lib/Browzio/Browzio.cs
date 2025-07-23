using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Win32;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using chameleon.assets;
using Chameleon.lib.Browzio.Services.Browzas;
using Chameleon.lib.Helpers;
using Chameleon.lib.Services;
using Chameleon.lib.Util;

namespace Chameleon.lib.Browzio;

#region types
public enum BrowserType {
	Unknown,
	Chrome, Edge, Brave, Opera, Vivaldi, Chromium,
	Firefox, Waterfox, LibreWolf,
	Safari, Yandex, Arc, InternetExplorer
}
public enum BrowserEngine { Unknown, Chromium, Gecko, WebKit, Other }

public record BrowserInfo(BrowserType Type, string ExecutablePath, string Version, BrowserEngine Engine) {
	public string Name => Type.ToString();
	public string DisplayName => !string.IsNullOrEmpty(Version) ? $"{Name} {Version}" : Name;
	public string? IconData { get; } = null;//IconExtractor.ExtractIcon(ExecutablePath);
	public string ExecutableName => Path.GetFileName(ExecutablePath).Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
}

public interface IBrowserDetector {
	List<BrowserInfo> DetectBrowsers();
	BrowserInfo? GetBrowser(BrowserType name);
	bool IsInstalled(BrowserType name);
	List<BrowserInfo> GetBrowsersByEngine(BrowserEngine engine);
	List<BrowserInfo> GetChromiumBrowsers();
	List<BrowserInfo> GetGeckoBrowsers();
}

public record BrowserOption(BrowserType Option) {
	public string IconName { get; } = Option.ToString().ToLower();
}
public class BrowserProxy(string? host = null, int port = 0, string? userName = null, string? password = null) {
	public NetworkCredential Credentials => new(userName, password);
	public WebProxy? WebProxy => host.IsNot() && port > 0 ? new WebProxy($"{host}:{port}") {
		Credentials = Credentials
	} : null;
	public object AddonObject => new {
		enabled = WebProxy != null,
		type = WebProxy?.Address?.Scheme,
		server = WebProxy?.Address?.Authority,
		host = WebProxy?.Address?.Host,
		port = WebProxy?.Address?.Port,
		username = (WebProxy?.Credentials as NetworkCredential)?.UserName,
		password = (WebProxy?.Credentials as NetworkCredential)?.Password,
	};
}
public class BrowserProfile(string? url = null) {
	public int Id { get; init; } = -1; // -1 is a special value for the default profile
	public BrowserProxy Proxy { get; set; } = new();

	public EmulationOptions Emulations { get; init; } = IoC.GetJsonValue<EmulationOptions>(nameof(EmulationOptions)) ?? new();
	public string[] Bookmarks { get; init; } = IoC.GetJsonValue<string[]>(nameof(Bookmarks)) ?? [];

	public string StartPage => url ?? IoC.GetValue(nameof(StartPage))
		.Let(l => l.Is()
			? "about:blank"
			: Uri.TryCreate(l, UriKind.Absolute, out var uriResult)
				? uriResult.AbsoluteUri
				: "http://" + l);
}
public record BrowserSetting(BrowserType BrowserType, BrowserProfile Profile) {
	public int Port { get; set; } = 0;
	public bool WithExtensions => Profile.Id > 0;
	public string CachePath => FilePaths.EnsureDirectoryExists(
		FilePaths.AppDataLocalDir, BrowserType.ToString(), Profile.Id.ToString()
	);
	public string ExtensionsPath =>
		Path.Combine(FilePaths.AppTempDir, Browzio.Extensions.Version, "Chromo", BrowserType.ToString(), Profile.Id.ToString());

	private IBrowserInstance? browser;
	public IBrowserInstance Browser => browser ??= BrowserType switch {
		BrowserType.Firefox => new Firefox() { Settings = this },
		_ => new Chromium() { Settings = this }
	};
}
public record EmulationOptions(
	bool AutoTimezone = true,
	bool SpoofGeoLocation = true,
	bool SpoofWebGLFingerprint = true,
	bool SpoofCanvasFingerprint = true,
	bool SpoofClientRects = true,
	bool SpoofFontFingerprint = true,
	bool SpoofAudio = true,
	bool DisableWebRTC = true,
	bool SpoofNavigator = false
);
#endregion

// Abstract base class with common functionality
public abstract class BrowserDetector : IBrowserDetector {
	// Known browser identifiers and their types/engines
	protected static readonly Dictionary<string, (BrowserType Type, BrowserEngine Engine)> KnownBrowsers = new(StringComparer.OrdinalIgnoreCase) {
		// Chromium-based
		["chrome"] = (BrowserType.Chrome, BrowserEngine.Chromium),
		["google chrome"] = (BrowserType.Chrome, BrowserEngine.Chromium),
		["msedge"] = (BrowserType.Edge, BrowserEngine.Chromium),
		["microsoft edge"] = (BrowserType.Edge, BrowserEngine.Chromium),
		["edge"] = (BrowserType.Edge, BrowserEngine.Chromium),
		["brave"] = (BrowserType.Brave, BrowserEngine.Chromium),
		["brave browser"] = (BrowserType.Brave, BrowserEngine.Chromium),
		["opera"] = (BrowserType.Opera, BrowserEngine.Chromium),
		["vivaldi"] = (BrowserType.Vivaldi, BrowserEngine.Chromium),
		["chromium"] = (BrowserType.Chromium, BrowserEngine.Chromium),
		["yandex"] = (BrowserType.Yandex, BrowserEngine.Chromium),
		["yandexbrowser"] = (BrowserType.Yandex, BrowserEngine.Chromium),
		["arc"] = (BrowserType.Arc, BrowserEngine.Chromium),

		// Gecko-based
		["firefox"] = (BrowserType.Firefox, BrowserEngine.Gecko),
		["waterfox"] = (BrowserType.Waterfox, BrowserEngine.Gecko),
		["librewolf"] = (BrowserType.LibreWolf, BrowserEngine.Gecko),

		// WebKit-based
		["safari"] = (BrowserType.Safari, BrowserEngine.WebKit),

		// Other
		["iexplore"] = (BrowserType.InternetExplorer, BrowserEngine.Other),
		["internet explorer"] = (BrowserType.InternetExplorer, BrowserEngine.Other)
	};

	public abstract List<BrowserInfo> DetectBrowsers();

	public virtual BrowserInfo? GetBrowser(BrowserType type) =>
			DetectBrowsers().FirstOrDefault(b => b.Type == type);

	public virtual bool IsInstalled(BrowserType type) =>
			GetBrowser(type) != null;

	public virtual List<BrowserInfo> GetBrowsersByEngine(BrowserEngine engine) =>
			DetectBrowsers().Where(b => b.Engine == engine).ToList();

	public virtual List<BrowserInfo> GetChromiumBrowsers() =>
			GetBrowsersByEngine(BrowserEngine.Chromium);

	public virtual List<BrowserInfo> GetGeckoBrowsers() =>
			GetBrowsersByEngine(BrowserEngine.Gecko);

	protected static (BrowserType Type, BrowserEngine Engine) DetermineBrowserInfo(string browserName, string executablePath) {
		// First check by name
		if (KnownBrowsers.TryGetValue(browserName, out var browserInfo)) {
			return browserInfo;
		}

		// Check executable name
		var execName = Path.GetFileNameWithoutExtension(executablePath).ToLower();
		if (KnownBrowsers.TryGetValue(execName, out browserInfo)) {
			return browserInfo;
		}

		// Heuristic detection based on path/name patterns
		var lowerPath = executablePath.ToLower();
		var lowerName = browserName.ToLower();

		if (lowerPath.Contains("chrome") && (!lowerPath.Contains("chromium") || lowerName.Contains("chrome"))) {
			return (BrowserType.Chrome, BrowserEngine.Chromium);
		}
		if (lowerPath.Contains("edge") || lowerName.Contains("edge")) {
			return (BrowserType.Edge, BrowserEngine.Chromium);
		}
		if (lowerPath.Contains("brave") || lowerName.Contains("brave")) {
			return (BrowserType.Brave, BrowserEngine.Chromium);
		}
		if (lowerPath.Contains("opera") || lowerName.Contains("opera")) {
			return (BrowserType.Opera, BrowserEngine.Chromium);
		}
		if (lowerPath.Contains("vivaldi") || lowerName.Contains("vivaldi")) {
			return (BrowserType.Vivaldi, BrowserEngine.Chromium);
		}
		if (lowerPath.Contains("firefox") || lowerName.Contains("firefox")) {
			return (BrowserType.Firefox, BrowserEngine.Gecko);
		}
		if (lowerPath.Contains("waterfox") || lowerName.Contains("waterfox")) {
			return (BrowserType.Waterfox, BrowserEngine.Gecko);
		}
		if (lowerPath.Contains("librewolf") || lowerName.Contains("librewolf")) {
			return (BrowserType.LibreWolf, BrowserEngine.Gecko);
		}
		if (lowerPath.Contains("safari") || lowerName.Contains("safari")) {
			return (BrowserType.Safari, BrowserEngine.WebKit);
		}
		if (lowerPath.Contains("yandex") || lowerName.Contains("yandex")) {
			return (BrowserType.Yandex, BrowserEngine.Chromium);
		}
		if (lowerPath.Contains("chromium") || lowerName.Contains("chromium")) {
			return (BrowserType.Chromium, BrowserEngine.Chromium);
		}

		return (BrowserType.Unknown, BrowserEngine.Unknown);
	}

	protected static string? GetBrowserVersion(string executablePath) {
		try {
			if (!File.Exists(executablePath)) return null;

			var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
			return versionInfo.ProductVersion ?? versionInfo.FileVersion;
		} catch {
			return null;
		}
	}
}

// Windows implementation
[SupportedOSPlatform("windows")]
public class WindowsBrowserDetector : BrowserDetector {
	// Common installation directories to scan
	private static readonly string[] SearchDirectories = {
				@"C:\Program Files",
				@"C:\Program Files (x86)",
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
		};

	public override List<BrowserInfo> DetectBrowsers() {
		var browsers = new List<BrowserInfo>();
		var foundPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Detect from registry
		DetectFromRegistry(browsers, foundPaths);

		// Detect from common installation directories
		DetectFromFileSystem(browsers, foundPaths);

		// Detect from PATH environment variable
		DetectFromPath(browsers, foundPaths);

		var uniqueBrowsers = browsers
			.GroupBy(b => b.Type)
			.Select(typeGroup => {

				var versionGroups = typeGroup.GroupBy(b => b.Version);
				var bestPerVersion = versionGroups.Select(versionGroup => {
					if (versionGroup.Count() == 1) {
						return versionGroup.First();
					}
					return versionGroup
						.OrderBy(b => GetPathPriority(b.ExecutablePath))
						.First();
				});

				return bestPerVersion
					.OrderByDescending(b => ParseVersion(b.Version))
					.First();
			})
			.ToList();

		return [.. uniqueBrowsers.OrderBy(b => b.Type.ToString())];
	}

	private static int GetPathPriority(string path) {
		var lowerPath = path.ToLowerInvariant();

		if (lowerPath.Contains("webview")) return 10;
		if (lowerPath.Contains("application\\")) return 5;
		return lowerPath.Contains("program files") ? 1 : 3;
	}

	private static Version ParseVersion(string versionString) {
		try {
			var cleanVersion = versionString?.Trim();
			if (string.IsNullOrEmpty(cleanVersion)) return new Version(0, 0);

			// Remove any non-version characters (like build info)
			var match = Regex.Match(cleanVersion, @"(\d+(?:\.\d+){0,3})");
			return match.Success
				? new Version(match.Groups[1].Value)
				: new Version(0, 0);
		} catch {
			return new Version(0, 0);
		}
	}


	private void DetectFromRegistry(List<BrowserInfo> browsers, HashSet<string> foundPaths) {
		// Check registered applications
		CheckRegisteredApplications(browsers, foundPaths);

		// Check default programs
		CheckDefaultPrograms(browsers, foundPaths);

		// Check uninstall entries
		CheckUninstallEntries(browsers, foundPaths);
	}

	private void CheckRegisteredApplications(List<BrowserInfo> browsers, HashSet<string> foundPaths) {
		try {
			using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\RegisteredApplications");
			if (key == null) return;

			foreach (var name in key.GetValueNames()) {
				var path = GetPathFromCapabilities(key.GetValue(name) as string);
				if (path != null && foundPaths.Add(path)) {
					var (type, engine) = DetermineBrowserInfo(name, path);
					var version = GetBrowserVersion(path);
					browsers.Add(new BrowserInfo(type, path, version ?? "", engine));
				}
			}
		} catch { }
	}

	private void CheckDefaultPrograms(List<BrowserInfo> browsers, HashSet<string> foundPaths) {
		try {
			using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\http\shell\open\command");
			var command = key?.GetValue("") as string;
			if (!string.IsNullOrEmpty(command)) {
				var match = Regex.Match(command, @"^""([^""]+)""");
				if (match.Success && File.Exists(match.Groups[1].Value) && foundPaths.Add(match.Groups[1].Value)) {
					var path = match.Groups[1].Value;
					var name = Path.GetFileNameWithoutExtension(path);
					var (type, engine) = DetermineBrowserInfo(name, path);
					var version = GetBrowserVersion(path);
					browsers.Add(new BrowserInfo(type, path, version ?? "", engine));
				}
			}
		} catch { }
	}

	private void CheckUninstallEntries(List<BrowserInfo> browsers, HashSet<string> foundPaths) {
		var uninstallKeys = new[] {
						@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
						@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
				};

		foreach (var uninstallKey in uninstallKeys) {
			try {
				using var key = Registry.LocalMachine.OpenSubKey(uninstallKey);
				if (key == null) continue;

				foreach (var subKeyName in key.GetSubKeyNames()) {
					using var subKey = key.OpenSubKey(subKeyName);
					var displayName = subKey?.GetValue("DisplayName") as string;
					var installLocation = subKey?.GetValue("InstallLocation") as string;

					if (IsBrowserEntry(displayName) && !string.IsNullOrEmpty(installLocation)) {
						var executablePath = FindBrowserExecutable(installLocation);
						if (executablePath != null && foundPaths.Add(executablePath)) {
							var (type, engine) = DetermineBrowserInfo(displayName!, executablePath);
							var version = GetBrowserVersion(executablePath);
							browsers.Add(new BrowserInfo(type, executablePath, version ?? "", engine));
						}
					}
				}
			} catch { }
		}
	}

	private void DetectFromFileSystem(List<BrowserInfo> browsers, HashSet<string> foundPaths) {
		foreach (var directory in SearchDirectories) {
			if (!Directory.Exists(directory)) continue;

			try {
				ScanDirectoryForBrowsers(directory, browsers, foundPaths, 2); // Max depth of 2
			} catch { }
		}
	}

	private void ScanDirectoryForBrowsers(string directory, List<BrowserInfo> browsers, HashSet<string> foundPaths, int maxDepth) {
		if (maxDepth <= 0) return;

		try {
			foreach (var subDir in Directory.GetDirectories(directory)) {
				var dirName = Path.GetFileName(subDir).ToLower();

				// Skip system directories
				if (dirName.StartsWith("windows") || dirName.StartsWith("system") ||
						dirName == "common files" || dirName == "microsoft") continue;

				// Look for browser-like directory names
				if (IsBrowserDirectory(dirName)) {
					var executablePath = FindBrowserExecutable(subDir);
					if (executablePath != null && foundPaths.Add(executablePath)) {
						var (type, engine) = DetermineBrowserInfo(dirName, executablePath);
						var version = GetBrowserVersion(executablePath);
						browsers.Add(new BrowserInfo(type, executablePath, version ?? "", engine));
					}
				} else if (maxDepth > 1) {
					ScanDirectoryForBrowsers(subDir, browsers, foundPaths, maxDepth - 1);
				}
			}
		} catch { }
	}

	private void DetectFromPath(List<BrowserInfo> browsers, HashSet<string> foundPaths) {
		var pathVar = Environment.GetEnvironmentVariable("PATH");
		if (string.IsNullOrEmpty(pathVar)) return;

		var browserExecutables = new[] { "chrome.exe", "firefox.exe", "msedge.exe", "brave.exe", "opera.exe" };

		foreach (var path in pathVar.Split(';')) {
			if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) continue;

			try {
				foreach (var exe in browserExecutables) {
					var fullPath = Path.Combine(path, exe);
					if (File.Exists(fullPath) && foundPaths.Add(fullPath)) {
						var name = Path.GetFileNameWithoutExtension(exe);
						var (type, engine) = DetermineBrowserInfo(name, fullPath);
						var version = GetBrowserVersion(fullPath);
						browsers.Add(new BrowserInfo(type, fullPath, version ?? "", engine));
					}
				}
			} catch { }
		}
	}

	private static string? GetPathFromCapabilities(string? capPath) {
		if (string.IsNullOrEmpty(capPath)) return null;
		try {
			capPath = capPath.Replace("SOFTWARE\\", "");
			using var capKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\{capPath}");
			using var urlKey = capKey?.OpenSubKey("URLAssociations");
			var handler = urlKey?.GetValue("https") ?? urlKey?.GetValue("http");
			if (handler == null) return null;

			using var cmdKey = Registry.ClassesRoot.OpenSubKey($@"{handler}\shell\open\command");
			var cmd = cmdKey?.GetValue("") as string;
			if (cmd == null) return null;

			var match = Regex.Match(cmd, @"^""([^""]+)""");
			return File.Exists(match.Groups[1].Value) ? match.Groups[1].Value : null;
		} catch {
			return null;
		}
	}

	private static bool IsBrowserEntry(string? displayName) {
		if (string.IsNullOrEmpty(displayName)) return false;
		var lower = displayName.ToLower();
		return lower.Contains("chrome") || lower.Contains("firefox") || lower.Contains("edge") ||
					 lower.Contains("brave") || lower.Contains("opera") || lower.Contains("vivaldi") ||
					 lower.Contains("browser") && !lower.Contains("flash");
	}

	private static bool IsBrowserDirectory(string dirName) {
		return dirName.Contains("chrome") || dirName.Contains("firefox") || dirName.Contains("edge") ||
					 dirName.Contains("brave") || dirName.Contains("opera") || dirName.Contains("vivaldi") ||
					 dirName.Contains("mozilla") || dirName.Contains("browser");
	}

	private static string? FindBrowserExecutable(string directory) {
		if (!Directory.Exists(directory)) return null;

		var commonExeNames = new[] { "chrome.exe", "firefox.exe", "msedge.exe", "brave.exe", "opera.exe", "vivaldi.exe", "launcher.exe" };

		// First, look for common browser executables in the directory and subdirectories
		foreach (var exeName in commonExeNames) {
			var files = Directory.GetFiles(directory, exeName, SearchOption.AllDirectories);
			if (files.Length > 0) return files[0];
		}

		// Look for any .exe that might be a browser
		var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories);
		return exeFiles.FirstOrDefault(f => {
			var name = Path.GetFileNameWithoutExtension(f).ToLower();
			return IsBrowserExecutable(name);
		});
	}

	private static bool IsBrowserExecutable(string exeName) {
		return exeName.Contains("chrome") || exeName.Contains("firefox") || exeName.Contains("edge") ||
					 exeName.Contains("brave") || exeName.Contains("opera") || exeName.Contains("vivaldi") ||
					 exeName == "launcher" || exeName.Contains("browser");
	}
}

// macOS implementation
[SupportedOSPlatform("macos")]
public class MacOSBrowserDetector : BrowserDetector {
	private static readonly string[] ApplicationDirectories = {
				"/Applications",
				"/System/Applications",
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications")
		};

	public override List<BrowserInfo> DetectBrowsers() {
		var browsers = new List<BrowserInfo>();
		var foundPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var appDir in ApplicationDirectories) {
			if (!Directory.Exists(appDir)) continue;

			try {
				foreach (var app in Directory.GetDirectories(appDir, "*.app")) {
					if (IsBrowserApp(app)) {
						var execPath = GetMacExecutable(app);
						if (execPath != null && File.Exists(execPath) && foundPaths.Add(execPath)) {
							var appName = Path.GetFileNameWithoutExtension(app);
							var (type, engine) = DetermineBrowserInfo(appName, execPath);
							var version = GetBrowserVersion(execPath) ?? GetMacAppVersion(app);
							browsers.Add(new BrowserInfo(type, execPath, version ?? "", engine));
						}
					}
				}
			} catch { }
		}

		return browsers.OrderBy(b => b.Type.ToString()).ToList();
	}

	private static bool IsBrowserApp(string appPath) {
		var appName = Path.GetFileNameWithoutExtension(appPath).ToLower();
		return appName.Contains("chrome") || appName.Contains("firefox") || appName.Contains("safari") ||
					 appName.Contains("edge") || appName.Contains("brave") || appName.Contains("opera") ||
					 appName.Contains("vivaldi") || appName.Contains("browser");
	}

	private static string? GetMacExecutable(string appPath) {
		var appName = Path.GetFileNameWithoutExtension(appPath);
		var execPath = Path.Combine(appPath, "Contents", "MacOS", appName);

		if (File.Exists(execPath)) return execPath;

		// Try to find any executable in MacOS directory
		var macOSDir = Path.Combine(appPath, "Contents", "MacOS");
		if (Directory.Exists(macOSDir)) {
			var executables = Directory.GetFiles(macOSDir)
					.Where(f => new FileInfo(f).UnixFileMode.HasFlag(UnixFileMode.UserExecute))
					.ToList();
			return executables.FirstOrDefault();
		}

		return null;
	}

	private static string? GetMacAppVersion(string appPath) {
		try {
			var plistPath = Path.Combine(appPath, "Contents", "Info.plist");
			if (!File.Exists(plistPath)) return null;

			var content = File.ReadAllText(plistPath);
			var versionMatch = Regex.Match(content, @"<key>CFBundleShortVersionString</key>\s*<string>([^<]+)</string>");
			return versionMatch.Success ? versionMatch.Groups[1].Value : null;
		} catch {
			return null;
		}
	}
}

#region services
public class AddonsServer {
	private WebApplication? app;

	public TaskCompletionSource? IsBusy { get; private set; }

	public int Port { get; }
	public string RedirectUri { get; }

	public TaskCompletionSource<bool> Initialized { get; } = new();
	private readonly ConcurrentDictionary<(string sessionId, int instanceId), (object config, int port, BrowserType bt)> sessions = [];

	internal AddonsServer() {
		foreach (var port in new[] { 3663, 3993, 3693, 3963, 6969, 6996, 9669, 9696 }) {
			try {
				// Create a listener to check if the port is available
				var listener = new TcpListener(IPAddress.Loopback, port);
				listener.Start();
				listener.Stop();
				Port = port;
				break;
			} catch (SocketException) {
				// Port is in use, try the next one
			}
		}

		RedirectUri = $"http://127.0.0.1:{Port}/callback";
	}

	public void AddSession(string sessionId, BrowserSetting settings, object config) {
		IsBusy = new TaskCompletionSource();
		sessions[(sessionId, settings.Profile.Id)] = (config, settings.Port, settings.BrowserType);
	}

	public async Task Init() {
		if (app != null) return;

		// builder configuration
		var builder = WebApplication.CreateBuilder();

		// Add minimal required services
		_ = builder.Services.AddEndpointsApiExplorer()
		 .AddCors(o => o.AddPolicy("AllowAnyOrigin", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

		// Configure to listen on all available interfaces, not just localhost
		_ = builder.WebHost.ConfigureKestrel(options => {
			options.Listen(IPAddress.Loopback, Port);
		});
		app = builder.Build();
		// Use minimal middleware
		_ = app.UseRouting().UseCors("AllowAnyOrigin");

		#region routes
		// Health check endpoint
		app.MapGet("/ping", () =>
			Results.Json(new { status = "ok", time = DateTime.Now })
		);

		app.MapGet("/init", ([FromQuery] int instanceId, [FromQuery] string sessionId) => {
			if (
				sessionId.Is() ||
				!sessions.TryGetValue((sessionId, instanceId), out var instance)
			) return Results.NotFound(new { error = "Session not found" });
			else return Results.Content(JSON.Serialize(instance.config), "application/json");
		});

		// endpoint to receive data from extensions
		app.MapPost("/app/data", (HttpContext context, [FromBody] JsonElement body) => {
			try {
				var instanceId = int.Parse(context.Request.Headers["X-Instance-ID"].ToString());
				var sessionId = context.Request.Headers["X-Session-ID"].ToString();
				return body.GetProperty("type").GetString() switch {
					"init" => sessions.TryGetValue((sessionId, instanceId), out var instance)
						? Results.Json(new { instance.config, instance.port })
						: Results.Json(new { error = "Session not found" }),
					// @TODO: Implement proper handling for "port" type
					_ => Results.BadRequest(new { error = "Invalid type" })
				};
			} catch (Exception e) {
				return Results.BadRequest(new { error = "Invalid", e });
			}
		});

		#endregion

		// Start the server
		await app.StartAsync();

		// Wait for the server to be ready
		do await Task.Delay(100);
		while (await EX.Poly(async () => {
			// Wait for the server to be ready
			if (app == null) throw new InvalidOperationException("AddonsServer is not initialized");
			using var httpClient = new HttpClient();
			httpClient.Timeout = TimeSpan.FromMilliseconds(500);
			var response = await httpClient.GetAsync($"http://127.0.0.1:{Port}/ping");
			return response.EnsureSuccessStatusCode().StatusCode != HttpStatusCode.OK;
		}));
		// Signal that the server has started successfully
		Console.WriteLine($"AddonsServer started successfully on port {Port}");
		_ = Initialized.TrySetResult(true);
	}

	public async Task Stop() {
		if (app == null) return;
		await app.StopAsync();
		await app.DisposeAsync();
		app = null;
	}
}

public class Browzers {
	public enum Event { Unknown, Error, Closed, Opened, Foreground, Background }
	private readonly SemaphoreSlim semaphore = new(1, 1);
	public ConcurrentDictionary<(BrowserType bt, int id), IBrowserInstance> Browsers { get; } = [];
	public ConcurrentDictionary<int, List<Action<object, IBrowserInstance.EventArgs>>> Observers { get; } = [];
	internal Browzers() { }

	public async Task<IBrowserInstance> Launch(BrowserSetting settings) {
		if (settings.WithExtensions) {
			settings.Browser.OnEvent += (sender, args) => {
				if (args.Event == Event.Closed) Browsers.TryRemove((settings.BrowserType, settings.Profile.Id), out _);
				if (Observers.TryGetValue(settings.Profile.Id, out var observer))
					observer.ForEach(x => x.Invoke(sender, args));
			};
			await settings.Browser.Initialize();
			return Browsers[(settings.BrowserType, settings.Profile.Id)] = settings.Browser;
		} else {
			_ = settings.Browser.Initialize();
			await settings.Browser.LoadedTCS.Task;
			return settings.Browser;
		}
	}

	public async Task<IBrowserInstance> Open(BrowserSetting settings) {
		await semaphore.WaitAsync();
		// To wait
		if (
			Browsers.TryGetValue((settings.BrowserType, settings.Profile.Id), out var browser) &&
			browser?.Brocess?.HasExited == false
		) return browser;
		try {
			if (browser != null) await browser.Closee();
			return await EX.Catch(
				async () => browser = await Launch(settings),
				e => {
					if (settings.WithExtensions) Toaster.Error(e.Message);
					_ = Browsers.TryRemove((settings.BrowserType, settings.Profile.Id), out _);
					settings.Browser.InvokeEvent(Event.Error);
				}) ?? throw new InvalidOperationException();
		} finally {
			// Signal
			_ = semaphore.Release();
		}
	}

	public void AddObserver(int id, Action<object, IBrowserInstance.EventArgs> action) {
		if (Observers.TryGetValue(id, out var value)) value.Add(action);
		else Observers[id] = [action];
	}
}
#endregion

public class Browzio : IInit {
	public static class State {
		public static bool Staging { get; } = true && IoC.Debug && Debugger.IsAttached;
		public static string? Version { get => IoC.GetValue(nameof(Extensions)); set => IoC.SetValue(nameof(Extensions), value, null); }
	}
	public static class Extensions {
		public static string Version => IoC.Assembled; //"2025.7.17.4";
		public static string ProdPath => Path.Combine(FilePaths.AppDataDir, "extensions");
		public static string DevPath => OperatingSystem.IsMacOS()
			? "/Users/dev/src/Chameleon-lib/Chameleon.Assets/addons"
			: @"C:\repos\Chameleon-lib\Chameleon.Assets\addons";

		public static string Chromium => FilePaths.EnsureDirectoryExists(ProdPath, "chromium");
		public static string Chromeleon => Path.Combine(State.Staging && Directory.Exists(DevPath) ? DevPath : Chromium, "chromeleon");

		public static string Gecko => Resources.Assert(ProdPath, "gecko");
		public static string Geckoleon => Path.Combine(State.Staging && Directory.Exists(DevPath) ? DevPath : Gecko, "geckoleon.xpi");
	}
	public static class Factory {
		public static BrowserSetting BrowserSettings(BrowserType bt, BrowserProfile profile, int? port = null) => new(bt, profile) {
			Port = port ?? Processez.NextFreePort(9613)
		};
		public static BrowserSetting Chrome(BrowserProfile profile) => BrowserSettings(BrowserType.Chrome, profile);
		public static BrowserSetting Brave(BrowserProfile profile) => BrowserSettings(BrowserType.Brave, profile);
		public static BrowserSetting Firefox(BrowserProfile profile) => BrowserSettings(BrowserType.Firefox, profile);

		public static IBrowserDetector CreateDetector() {
			return OperatingSystem.IsWindows()
				? new WindowsBrowserDetector()
				: OperatingSystem.IsMacOS()
				? new MacOSBrowserDetector()
				: throw new NotSupportedException("Unsupported operating system");
		}
	}
	public static class Utilities {
		private static readonly IBrowserDetector detector = Factory.CreateDetector();

		public static List<BrowserInfo> DetectBrowsers() => detector.DetectBrowsers();
		public static BrowserInfo? GetBrowser(BrowserType type) => detector.GetBrowser(type);
		public static bool IsInstalled(BrowserType type) => detector.IsInstalled(type);
		public static List<BrowserInfo> GetBrowsersByEngine(BrowserEngine engine) => detector.GetBrowsersByEngine(engine);
		public static List<BrowserInfo> GetChromiumBrowsers() => detector.GetChromiumBrowsers();
		public static List<BrowserInfo> GetGeckoBrowsers() => detector.GetGeckoBrowsers();

		[Obsolete("Use IBrowserDetector instead.")]
		public static class Info {
			public record class Information(string Name, string ExecutablePath) {
				public override string ToString() {
					return Name ?? ExecutablePath;
				}
				public bool Exists => !string.IsNullOrEmpty(ExecutablePath) && File.Exists(ExecutablePath);
			}

			[SupportedOSPlatform("windows")]
			private static (bool Installed, string FilePath) CheckApplication(string executable) {
				// Check common installation paths
				string[] commonPaths = [
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), executable),
						Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), executable)
				];

				foreach (var path in commonPaths) {
					if (File.Exists(path)) return (true, path);
				}

				// Check registry
				string[] registryKeys = [
					@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
						@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths"
				];

				foreach (var registryKey in registryKeys) {
					using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(Path.Combine(registryKey, executable));
					if (key != null) {
						var path = key.GetValue(null) as string;
						if (!string.IsNullOrEmpty(path) && File.Exists(path)) return (true, path);
					}
				}

				// Check for user-specific installation
				var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				var userSpecificPaths = Directory.GetFiles(appDataPath, executable, SearchOption.AllDirectories);
				if (userSpecificPaths.Length != 0) return (true, userSpecificPaths.First());

				// Check uninstall registry keys
				string[] uninstallKeys = [
					@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
						@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
				];

				foreach (var uninstallKey in uninstallKeys) {
					using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(uninstallKey);
					if (key != null) {
						foreach (var subKeyName in key.GetSubKeyNames()) {
							using var subKey = key.OpenSubKey(subKeyName);
							var displayName = subKey?.GetValue("DisplayName") as string;
							if (
								!string.IsNullOrEmpty(displayName) &&
								displayName.Contains(Path.GetFileNameWithoutExtension(executable), StringComparison.OrdinalIgnoreCase)
							) {
								var installLocation = subKey?.GetValue("InstallLocation") as string;
								if (!string.IsNullOrEmpty(installLocation)) {
									var fullPath = Path.Combine(installLocation, executable);
									if (File.Exists(fullPath)) return (true, fullPath);
								}
							}
						}
					}
				}

				return (false, string.Empty);
			}

			static Information FindByName(string executable) {
				if (OperatingSystem.IsMacOS()) {
					var chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
					var bravePath = "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser";
					var firefoxPath = "/Applications/firefox.app/Contents/MacOS/firefox";

					return executable switch {
						"chrome.exe" => File.Exists(chromePath) ? new Information("chrome", chromePath) : null,
						"brave.exe" => File.Exists(bravePath) ? new Information("brave", bravePath) : null,
						"firefox.exe" => File.Exists(firefoxPath) ? new Information("firefox", firefoxPath) : null,
						_ => null
					} ?? throw new NotSupportedException(
					$"{char.ToUpper(executable[0]) + executable[1..]} browser is not installed.");
				} else if (OperatingSystem.IsWindows()) {
					var (installed, filepath) = CheckApplication(executable);
					if (installed && filepath.IsNot()) return new(executable, filepath);
				}

				throw new NotSupportedException($"{char.ToUpper(executable[0]) + executable[1..]} browser is not installed.");
			}

			public static Information GetBrowser(BrowserType BrowserType) => BrowserType switch {
				BrowserType.Chrome => FindByName("chrome.exe"),
				BrowserType.Brave => FindByName("brave.exe"),
				BrowserType.Firefox => FindByName("firefox.exe"),
				_ => throw new NotSupportedException("Browser type not found."),
			};
		}
	}

	public Browzers Browzas { get; } = new();
	public AddonsServer Loopback { get; } = new();

	public TaskCompletionSource<bool> Initialized { get; } = new();
	public async Task Init() {
		await Loopback.Init();
		await Loopback.Initialized.Task;
		await Resources.CopyFile("addons", "geckoleon.xpi", Extensions.Gecko);
		await Resources.LoadExtension(ExtensionType.chromeleon, Extensions.Chromium);

		_ = Initialized.TrySetResult(true);
	}

	Browzio() { }
	public static Browzio I { get; } = new();
}

// public static void OpenUrl(string url, BrowserType type) {
//   var browser = GetBrowser(type);
//   if (browser == null) throw new InvalidOperationException($"{type} not installed");

//   if (IsMacOS) {
//     Process.Start("open", $"-a \"{browser.Name}\" \"{url}\"");
//   } else {
//     Process.Start(new ProcessStartInfo {
// FileName = browser.Path,
// Arguments = url,
// UseShellExecute = false
//     });
//   }
// }
// [DllImport("shell32.dll", CharSet = CharSet.Auto)]
// private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

// [DllImport("user32.dll", SetLastError = true)]
// private static extern bool DestroyIcon(IntPtr hIcon);

// private static byte[] ExtractIcon(string exePath)
// {
//     if (string.IsNullOrEmpty(exePath)) return null;

//     try
//     {
//         if (IsWindows)
//         {
//          if (!File.Exists(exePath)) return null;

//          var hIcon = ExtractIcon(IntPtr.Zero, exePath, 0);
//          if (hIcon == IntPtr.Zero) return null;

//          using var icon = Icon.FromHandle(hIcon);
//          using var bitmap = icon.ToBitmap();
//          using var stream = new MemoryStream();
//          bitmap.Save(stream, ImageFormat.Png);
//          DestroyIcon(hIcon);
//          return stream.ToArray();
//         }
//         else if (IsMacOS)
//         {
//          return ExtractMacIcon(exePath);
//         }
//     }
//     catch { }

//     return null;
// }

// private static byte[] ExtractMacIcon(string appPath)
// {
//     try
//     {
//         // Get the app bundle path from executable path
//         var bundlePath = appPath;
//         if (appPath.Contains("/Contents/MacOS/"))
//         {
//          bundlePath = appPath.Substring(0, appPath.IndexOf("/Contents/MacOS/"));
//         }

//         // Read Info.plist to get icon name
//         var plistPath = Path.Combine(bundlePath, "Contents", "Info.plist");
//         if (!File.Exists(plistPath)) return null;

//         var iconName = "AppIcon"; // Default
//         var plistContent = File.ReadAllText(plistPath);
//         var iconMatch = Regex.Match(plistContent, @"<key>CFBundleIconFile</key>\s*<string>([^<]+)</string>");
//         if (iconMatch.Success)
//          iconName = iconMatch.Groups[1].Value.Replace(".icns", "");

//         // Try to find the icns file
//         var icnsPath = Path.Combine(bundlePath, "Contents", "Resources", $"{iconName}.icns");
//         if (!File.Exists(icnsPath))
//          icnsPath = Path.Combine(bundlePath, "Contents", "Resources", "AppIcon.icns");

//         if (File.Exists(icnsPath))
//         {
//          // Use sips to convert icns to png
//          var tempFile = Path.GetTempFileName() + ".png";
//          var process = Process.Start(new ProcessStartInfo
//          {
//              FileName = "sips",
//              Arguments = $"-s format png \"{icnsPath}\" --out \"{tempFile}\"",
//              UseShellExecute = false,
//              CreateNoWindow = true
//          });
//          process.WaitForExit();

//          if (File.Exists(tempFile))
//          {
//              var data = File.ReadAllBytes(tempFile);
//              File.Delete(tempFile);
//              return data;
//          }
//         }
//     }
//     catch { }

//     return null;
// }
// TDODOZ
// namespace BrowserUtilities
// {
//     /// <summary>
//     /// Supported browser types
//     /// </summary>
//     public enum BrowserType
//     {
//         Unknown,
//         Chrome,
//         Edge,
//         Firefox,
//         Safari,
//         Brave,
//         Opera,
//         Vivaldi,
//         Chromium,
//         Waterfox,
//         LibreWolf,
//         Tor,
//         Yandex,
//         Arc,
//         SRWareIron,
//         Maxthon,
//         Palemoon,
//         IceCat,
//         SeaMonkey,
//         InternetExplorer
//     }

//     /// <summary>
//     /// Browser engine types
//     /// </summary>
//     public enum BrowserEngine
//     {
//         Unknown,
//         Chromium,
//         Gecko,
//         WebKit,
//         EdgeHTML,
//         Trident
//     }

//     /// <summary>
//     /// Browser information
//     /// </summary>
//     public class BrowserInfo
//     {
//         public BrowserType Type { get; set; }
//         public string Name { get; set; }
//         public string ExecutablePath { get; set; }
//         public string ProcessName { get; set; }
//         public BrowserEngine Engine { get; set; }
//         public string Version { get; set; }
//         public bool IsDefault { get; set; }
//         public bool IsRunning => BrowserUtility.IsBrowserRunning(this);
//     }

//     /// <summary>
//     /// Simplified browser detection and management utility
//     /// </summary>
//     public static class BrowserUtility
//     {
//         #region Browser Configurations

//         private static readonly Dictionary<BrowserType, BrowserConfig> BrowserConfigs = new()
//         {
//          [BrowserType.Chrome] = new BrowserConfig
//          {
//              DisplayName = "Google Chrome",
//              ProcessNames = new[] { "chrome", "Google Chrome" },
//              Engine = BrowserEngine.Chromium,
//              WindowsPaths = new[]
//              {
//               @"C:\Program Files\Google\Chrome\Application\chrome.exe",
//               @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
//               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe")
//              },
//              MacPaths = new[] { "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" },
//              LinuxPaths = new[] { "/usr/bin/google-chrome", "/usr/bin/google-chrome-stable", "/opt/google/chrome/chrome" }
//          },
//          [BrowserType.Edge] = new BrowserConfig
//          {
//              DisplayName = "Microsoft Edge",
//              ProcessNames = new[] { "msedge", "Microsoft Edge" },
//              Engine = BrowserEngine.Chromium,
//              WindowsPaths = new[]
//              {
//               @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
//               @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
//              },
//              MacPaths = new[] { "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge" },
//              LinuxPaths = new[] { "/usr/bin/microsoft-edge", "/opt/microsoft/edge/microsoft-edge" }
//          },
//          [BrowserType.Firefox] = new BrowserConfig
//          {
//              DisplayName = "Mozilla Firefox",
//              ProcessNames = new[] { "firefox", "firefox-bin" },
//              Engine = BrowserEngine.Gecko,
//              WindowsPaths = new[]
//              {
//               @"C:\Program Files\Mozilla Firefox\firefox.exe",
//               @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe"
//              },
//              MacPaths = new[] { "/Applications/Firefox.app/Contents/MacOS/firefox" },
//              LinuxPaths = new[] { "/usr/bin/firefox", "/snap/bin/firefox", "/usr/lib/firefox/firefox" }
//          },
//          [BrowserType.Brave] = new BrowserConfig
//          {
//              DisplayName = "Brave Browser",
//              ProcessNames = new[] { "brave", "Brave Browser" },
//              Engine = BrowserEngine.Chromium,
//              WindowsPaths = new[]
//              {
//               @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe",
//               @"C:\Program Files (x86)\BraveSoftware\Brave-Browser\Application\brave.exe",
//               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"BraveSoftware\Brave-Browser\Application\brave.exe")
//              },
//              MacPaths = new[] { "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser" },
//              LinuxPaths = new[] { "/usr/bin/brave-browser", "/opt/brave.com/brave/brave", "/snap/bin/brave" }
//          },
//          [BrowserType.Opera] = new BrowserConfig
//          {
//              DisplayName = "Opera",
//              ProcessNames = new[] { "opera", "launcher" },
//              Engine = BrowserEngine.Chromium,
//              WindowsPaths = new[]
//              {
//               @"C:\Program Files\Opera\launcher.exe",
//               @"C:\Program Files (x86)\Opera\launcher.exe",
//               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Opera\launcher.exe")
//              },
//              MacPaths = new[] { "/Applications/Opera.app/Contents/MacOS/Opera" },
//              LinuxPaths = new[] { "/usr/bin/opera", "/usr/lib/opera/opera" }
//          },
//          [BrowserType.Vivaldi] = new BrowserConfig
//          {
//              DisplayName = "Vivaldi",
//              ProcessNames = new[] { "vivaldi", "vivaldi-bin" },
//              Engine = BrowserEngine.Chromium,
//              WindowsPaths = new[]
//              {
//               @"C:\Program Files\Vivaldi\Application\vivaldi.exe",
//               @"C:\Program Files (x86)\Vivaldi\Application\vivaldi.exe",
//               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Vivaldi\Application\vivaldi.exe")
//              },
//              MacPaths = new[] { "/Applications/Vivaldi.app/Contents/MacOS/Vivaldi" },
//              LinuxPaths = new[] { "/usr/bin/vivaldi", "/opt/vivaldi/vivaldi" }
//          },
//          [BrowserType.Safari] = new BrowserConfig
//          {
//              DisplayName = "Safari",
//              ProcessNames = new[] { "Safari", "com.apple.Safari" },
//              Engine = BrowserEngine.WebKit,
//              MacPaths = new[] { "/Applications/Safari.app/Contents/MacOS/Safari" }
//          },
//          [BrowserType.Chromium] = new BrowserConfig
//          {
//              DisplayName = "Chromium",
//              ProcessNames = new[] { "chromium", "chromium-browser" },
//              Engine = BrowserEngine.Chromium,
//              WindowsPaths = new[]
//              {
//               @"C:\Program Files\Chromium\Application\chrome.exe",
//               @"C:\Program Files (x86)\Chromium\Application\chrome.exe"
//              },
//              LinuxPaths = new[] { "/usr/bin/chromium", "/usr/bin/chromium-browser", "/snap/bin/chromium" }
//          },
//          [BrowserType.Waterfox] = new BrowserConfig
//          {
//              DisplayName = "Waterfox",
//              ProcessNames = new[] { "waterfox", "waterfox-bin" },
//              Engine = BrowserEngine.Gecko
//          },
//          [BrowserType.LibreWolf] = new BrowserConfig
//          {
//              DisplayName = "LibreWolf",
//              ProcessNames = new[] { "librewolf" },
//              Engine = BrowserEngine.Gecko
//          },
//          [BrowserType.Tor] = new BrowserConfig
//          {
//              DisplayName = "Tor Browser",
//              ProcessNames = new[] { "firefox", "tor" },
//              Engine = BrowserEngine.Gecko
//          },
//          [BrowserType.Arc] = new BrowserConfig
//          {
//              DisplayName = "Arc",
//              ProcessNames = new[] { "Arc" },
//              Engine = BrowserEngine.Chromium
//          }
//         };

//         private class BrowserConfig
//         {
//          public string DisplayName { get; set; }
//          public string[] ProcessNames { get; set; }
//          public BrowserEngine Engine { get; set; }
//          public string[] WindowsPaths { get; set; } = Array.Empty<string>();
//          public string[] MacPaths { get; set; } = Array.Empty<string>();
//          public string[] LinuxPaths { get; set; } = Array.Empty<string>();
//         }

//         #endregion

//         #region Public Methods

//         /// <summary>
//         /// Detects all installed browsers on the system
//         /// </summary>
//         public static List<BrowserInfo> DetectInstalledBrowsers()
//         {
//          var browsers = new List<BrowserInfo>();

//          if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//              browsers = DetectWindowsBrowsers();
//          else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
//              browsers = DetectMacOSBrowsers();
//          else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//              browsers = DetectLinuxBrowsers();

//          // Mark default browser
//          var defaultBrowser = GetDefaultBrowser();
//          if (defaultBrowser != null)
//          {
//              var matchingBrowser = browsers.FirstOrDefault(b => 
//               b.ExecutablePath == defaultBrowser.ExecutablePath);
//              if (matchingBrowser != null)
//               matchingBrowser.IsDefault = true;
//          }

//          return browsers;
//         }

//         /// <summary>
//         /// Gets a specific browser by type
//         /// </summary>
//         public static BrowserInfo GetBrowser(BrowserType type)
//         {
//          var browsers = DetectInstalledBrowsers();
//          return browsers.FirstOrDefault(b => b.Type == type);
//         }

//         /// <summary>
//         /// Checks if a specific browser is installed
//         /// </summary>
//         public static bool IsBrowserInstalled(BrowserType type)
//         {
//          return GetBrowser(type) != null;
//         }

//         /// <summary>
//         /// Checks if a browser is currently running
//         /// </summary>
//         public static bool IsBrowserRunning(BrowserInfo browser)
//         {
//          if (browser == null || string.IsNullOrEmpty(browser.ProcessName))
//              return false;

//          try
//          {
//              var processes = Process.GetProcessesByName(browser.ProcessName);
//              return processes.Length > 0;
//          }
//          catch
//          {
//              return false;
//          }
//         }

//         /// <summary>
//         /// Checks if a specific browser type is running
//         /// </summary>
//         public static bool IsBrowserRunning(BrowserType type)
//         {
//          var browser = GetBrowser(type);
//          return browser != null && IsBrowserRunning(browser);
//         }

//         /// <summary>
//         /// Gets all currently running browsers
//         /// </summary>
//         public static List<BrowserInfo> GetRunningBrowsers()
//         {
//          var browsers = DetectInstalledBrowsers();
//          return browsers.Where(b => b.IsRunning).ToList();
//         }

//         /// <summary>
//         /// Opens a URL in a specific browser
//         /// </summary>
//         public static void OpenUrl(string url, BrowserType type)
//         {
//          var browser = GetBrowser(type);
//          if (browser == null)
//              throw new InvalidOperationException($"Browser {type} is not installed");

//          OpenUrl(url, browser);
//         }

//         /// <summary>
//         /// Opens a URL in a specific browser
//         /// </summary>
//         public static void OpenUrl(string url, BrowserInfo browser)
//         {
//          if (browser == null || !File.Exists(browser.ExecutablePath))
//              throw new ArgumentException("Invalid browser or browser not found");

//          Process.Start(new ProcessStartInfo
//          {
//              FileName = browser.ExecutablePath,
//              Arguments = url,
//              UseShellExecute = false
//          });
//         }

//         /// <summary>
//         /// Opens a URL in the default browser
//         /// </summary>
//         public static void OpenUrlInDefaultBrowser(string url)
//         {
//          Process.Start(new ProcessStartInfo
//          {
//              FileName = url,
//              UseShellExecute = true
//          });
//         }

//         /// <summary>
//         /// Terminates all instances of a browser
//         /// </summary>
//         public static void TerminateBrowser(BrowserType type, bool force = false)
//         {
//          var browser = GetBrowser(type);
//          if (browser != null)
//              TerminateBrowser(browser, force);
//         }

//         /// <summary>
//         /// Terminates all instances of a browser
//         /// </summary>
//         public static void TerminateBrowser(BrowserInfo browser, bool force = false)
//         {
//          if (browser == null || string.IsNullOrEmpty(browser.ProcessName))
//              return;

//          try
//          {
//              var processes = Process.GetProcessesByName(browser.ProcessName);
//              foreach (var process in processes)
//              {
//               try
//               {
//                   if (force)
//                       process.Kill();
//                   else
//                       process.CloseMainWindow();
//               }
//               catch { }
//              }
//          }
//          catch { }
//         }

//         /// <summary>
//         /// Gets all browsers of a specific engine type
//         /// </summary>
//         public static List<BrowserInfo> GetBrowsersByEngine(BrowserEngine engine)
//         {
//          var browsers = DetectInstalledBrowsers();
//          return browsers.Where(b => b.Engine == engine).ToList();
//         }

//         #endregion

//         #region Platform-Specific Detection

//         private static List<BrowserInfo> DetectWindowsBrowsers()
//         {
//          var browsers = new List<BrowserInfo>();

//          // Check each browser type
//          foreach (var config in BrowserConfigs)
//          {
//              // Check file paths
//              foreach (var path in config.Value.WindowsPaths)
//              {
//               if (File.Exists(path))
//               {
//                   browsers.Add(CreateBrowserInfo(config.Key, path));
//                   break; // Found this browser, move to next
//               }
//              }
//          }

//          // Also check registry for additional browsers
//          DetectFromWindowsRegistry(browsers);

//          return browsers;
//         }

//         private static List<BrowserInfo> DetectMacOSBrowsers()
//         {
//          var browsers = new List<BrowserInfo>();

//          foreach (var config in BrowserConfigs)
//          {
//              foreach (var path in config.Value.MacPaths)
//              {
//               if (File.Exists(path))
//               {
//                   browsers.Add(CreateBrowserInfo(config.Key, path));
//                   break;
//               }
//              }
//          }

//          return browsers;
//         }

//         private static List<BrowserInfo> DetectLinuxBrowsers()
//         {
//          var browsers = new List<BrowserInfo>();

//          foreach (var config in BrowserConfigs)
//          {
//              foreach (var path in config.Value.LinuxPaths)
//              {
//               if (File.Exists(path))
//               {
//                   browsers.Add(CreateBrowserInfo(config.Key, path));
//                   break;
//               }
//              }
//          }

//          return browsers;
//         }

//         private static void DetectFromWindowsRegistry(List<BrowserInfo> browsers)
//         {
//          try
//          {
//              using var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\RegisteredApplications");
//              if (regKey == null) return;

//              foreach (var appName in regKey.GetValueNames())
//              {
//               var capPath = regKey.GetValue(appName) as string;
//               if (string.IsNullOrEmpty(capPath)) continue;

//               // Remove "SOFTWARE\" prefix if present
//               capPath = capPath.Replace("SOFTWARE\\", "");

//               using var capKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\{capPath}");
//               if (capKey == null) continue;

//               using var urlAssocKey = capKey.OpenSubKey("URLAssociations");
//               if (urlAssocKey == null) continue;

//               var httpHandler = urlAssocKey.GetValue("http") as string;
//               var httpsHandler = urlAssocKey.GetValue("https") as string;

//               if (string.IsNullOrEmpty(httpHandler) && string.IsNullOrEmpty(httpsHandler))
//                   continue;

//               var handler = httpsHandler ?? httpHandler;
//               var execPath = GetExecutableFromHandler(handler);

//               if (!string.IsNullOrEmpty(execPath) && File.Exists(execPath))
//               {
//                   var browserType = DetermineBrowserType(appName, execPath);
//                   if (!browsers.Any(b => b.ExecutablePath == execPath))
//                   {
//                       browsers.Add(CreateBrowserInfo(browserType, execPath));
//                   }
//               }
//              }
//          }
//          catch { }
//         }

//         private static string GetExecutableFromHandler(string handler)
//         {
//          try
//          {
//              using var handlerKey = Registry.ClassesRoot.OpenSubKey($@"{handler}\shell\open\command");
//              if (handlerKey == null) return null;

//              var command = handlerKey.GetValue("") as string;
//              if (string.IsNullOrEmpty(command)) return null;

//              // Extract executable path from command
//              var match = Regex.Match(command, @"^""([^""]+)""");
//              if (match.Success)
//               return match.Groups[1].Value;

//              // Try without quotes
//              var parts = command.Split(' ');
//              if (parts.Length > 0 && File.Exists(parts[0]))
//               return parts[0];
//          }
//          catch { }

//          return null;
//         }

//         #endregion

//         #region Helper Methods

//         private static BrowserInfo CreateBrowserInfo(BrowserType type, string executablePath)
//         {
//          var config = BrowserConfigs.GetValueOrDefault(type);
//          var processName = GetProcessName(executablePath);

//          return new BrowserInfo
//          {
//              Type = type,
//              Name = config?.DisplayName ?? type.ToString(),
//              ExecutablePath = executablePath,
//              ProcessName = processName,
//              Engine = config?.Engine ?? BrowserEngine.Unknown,
//              Version = GetBrowserVersion(executablePath)
//          };
//         }

//         private static string GetProcessName(string executablePath)
//         {
//          if (string.IsNullOrEmpty(executablePath))
//              return null;

//          return Path.GetFileNameWithoutExtension(executablePath);
//         }

//         private static string GetBrowserVersion(string executablePath)
//         {
//          if (!File.Exists(executablePath))
//              return "Unknown";

//          try
//          {
//              var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
//              if (!string.IsNullOrEmpty(versionInfo.ProductVersion))
//               return versionInfo.ProductVersion;
//          }
//          catch { }

//          return "Unknown";
//         }

//         private static BrowserType DetermineBrowserType(string name, string path)
//         {
//          var combined = (name + " " + path).ToLower();

//          if (combined.Contains("chrome") && !combined.Contains("chromium"))
//              return BrowserType.Chrome;
//          if (combined.Contains("edge") || combined.Contains("msedge"))
//              return BrowserType.Edge;
//          if (combined.Contains("firefox"))
//              return BrowserType.Firefox;
//          if (combined.Contains("brave"))
//              return BrowserType.Brave;
//          if (combined.Contains("opera"))
//              return BrowserType.Opera;
//          if (combined.Contains("vivaldi"))
//              return BrowserType.Vivaldi;
//          if (combined.Contains("safari"))
//              return BrowserType.Safari;
//          if (combined.Contains("chromium"))
//              return BrowserType.Chromium;

//          return BrowserType.Unknown;
//         }

//         private static BrowserInfo GetDefaultBrowser()
//         {
//          if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//              return GetWindowsDefaultBrowser();
//          // Add macOS and Linux default browser detection if needed
//          return null;
//         }

//         private static BrowserInfo GetWindowsDefaultBrowser()
//         {
//          try
//          {
//              using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice");
//              if (key == null) return null;

//              var progId = key.GetValue("ProgId") as string;
//              if (string.IsNullOrEmpty(progId)) return null;

//              var execPath = GetExecutableFromHandler(progId);
//              if (string.IsNullOrEmpty(execPath) || !File.Exists(execPath))
//               return null;

//              var browserType = DetermineBrowserType(progId, execPath);
//              return CreateBrowserInfo(browserType, execPath);
//          }
//          catch { }

//          return null;
//         }

//         #endregion
//     }

//     // Example usage
//     class Program
//     {
//         static void Main()
//         {
//          // Detect all browsers
//          var browsers = BrowserUtility.DetectInstalledBrowsers();

//          Console.WriteLine("Installed Browsers:");
//          foreach (var browser in browsers)
//          {
//              Console.WriteLine($"- {browser.Name} ({browser.Type})");
//              Console.WriteLine($"  Path: {browser.ExecutablePath}");
//              Console.WriteLine($"  Engine: {browser.Engine}");
//              Console.WriteLine($"  Running: {browser.IsRunning}");
//              Console.WriteLine($"  Default: {browser.IsDefault}");
//              Console.WriteLine();
//          }

//          // Check specific browser
//          if (BrowserUtility.IsBrowserInstalled(BrowserType.Chrome))
//          {
//              Console.WriteLine("Chrome is installed!");

//              if (BrowserUtility.IsBrowserRunning(BrowserType.Chrome))
//              {
//               Console.WriteLine("Chrome is currently running.");
//              }
//          }

//          // Get all Chromium-based browsers
//          var chromiumBrowsers = BrowserUtility.GetBrowsersByEngine(BrowserEngine.Chromium);
//          Console.WriteLine($"\nFound {chromiumBrowsers.Count} Chromium-based browsers");

//          // Open URL in specific browser
//          // BrowserUtility.OpenUrl("https://example.com", BrowserType.Firefox);
//         }
//     }
// }