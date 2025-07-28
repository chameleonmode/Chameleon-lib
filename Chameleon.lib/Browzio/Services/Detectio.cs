using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Chameleon.lib.Browzio.Services;

public interface IBrowserDetector {
  List<BrowserInfo> DetectedBrowsers { get; }
  BrowserInfo? GetBrowser(BrowserType name);
  bool IsInstalled(BrowserType name);
  List<BrowserInfo> GetBrowsersByEngine(BrowserEngine engine);
}

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

	private List<BrowserInfo>? detectedBrowsers;
	public virtual List<BrowserInfo> DetectedBrowsers { get { return detectedBrowsers ??= DetectBrowsers(); } }
	public abstract List<BrowserInfo> DetectBrowsers();

	public virtual BrowserInfo? GetBrowser(BrowserType type) =>
			DetectBrowsers().FirstOrDefault(b => b.Type == type);

	public virtual bool IsInstalled(BrowserType type) =>
			GetBrowser(type) != null;

	public virtual List<BrowserInfo> GetBrowsersByEngine(BrowserEngine engine) =>
			DetectBrowsers().Where(b => b.Engine == engine).ToList();

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
