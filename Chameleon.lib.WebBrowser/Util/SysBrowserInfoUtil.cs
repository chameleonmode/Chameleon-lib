using System.Diagnostics;
using System.Runtime.Versioning;

using Chameleon.lib.Common.Enums;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.WebBrowser.Models;

using Microsoft.Win32;

namespace Chameleon.lib.WebBrowser.Util;
public static class SysBrowserInfoUtil {

	[SupportedOSPlatform("windows")]
	private static (bool IsInstalled, string FilePath) CheckApplication(string executableName)
	{
		// Check common installation paths
		string[] commonPaths = [
						Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), executableName),
						Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), executableName)
				];

		foreach (var path in commonPaths) {
			if (File.Exists(path)) {
				return (true, path);
			}
		}

		// Check registry
		string[] registryKeys = [
						@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
						@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths"
		];

		foreach (var registryKey in registryKeys) {
			using var key = Registry.LocalMachine.OpenSubKey(Path.Combine(registryKey, executableName));
			if (key != null) {
				var path = key.GetValue(null) as string;
				if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
					return (true, path);
				}
			}
		}

		// Check for user-specific installation
		var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var userSpecificPaths = Directory.GetFiles(appDataPath, executableName, SearchOption.AllDirectories);
		if (userSpecificPaths.Length != 0) {
			return (true, userSpecificPaths.First());
		}

		// Check uninstall registry keys
		string[] uninstallKeys = [
						@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
						@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
				];

		foreach (var uninstallKey in uninstallKeys) {
			using var key = Registry.LocalMachine.OpenSubKey(uninstallKey);
			if (key != null) {
				foreach (var subKeyName in key.GetSubKeyNames()) {
					using var subKey = key.OpenSubKey(subKeyName);
					var displayName = subKey?.GetValue("DisplayName") as string;
					if (!string.IsNullOrEmpty(displayName) && displayName.Contains(Path.GetFileNameWithoutExtension(executableName), StringComparison.OrdinalIgnoreCase)) {
						var installLocation = subKey?.GetValue("InstallLocation") as string;
						if (!string.IsNullOrEmpty(installLocation)) {
							var fullPath = Path.Combine(installLocation, executableName);
							if (File.Exists(fullPath)) {
								return (true, fullPath);
							}
						}
					}
				}
			}
		}

		return (false, string.Empty);
	}

	public static SysBrowserRecord FindByName(string browserName)
	{
		SysBrowserRecord? inf = null;
		if (OperatingSystem.IsMacOS()) {
			var chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
			var bravePath = "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser";
			var firefoxPath = "/Applications/firefox.app/Contents/MacOS/firefox";

			inf = browserName switch {
				"chrome.exe" => File.Exists(chromePath) ? new SysBrowserRecord("chrome", chromePath) : null,
				"brave.exe" => File.Exists(bravePath) ? new SysBrowserRecord("brave", bravePath) : null,
				"firefox.exe" => File.Exists(firefoxPath) ? new SysBrowserRecord("brave", firefoxPath) : null,
				_ => null
			};
		} else {
#pragma warning disable CA1416 // Validate platform compatibility

			var (isinstalled, filepath) = CheckApplication(browserName);
			if (isinstalled && filepath.Is()) {
				inf = new SysBrowserRecord(browserName, filepath);
			}

#pragma warning restore CA1416 // Validate platform compatibility
		}

		return inf ?? throw new NotSupportedException(
				$"{char.ToUpper(browserName[0]) + browserName[1..]} browser is not installed.");
	}

	public static SysBrowserRecord FindByType(SystemBrowserType BrowserType) => BrowserType switch {
		SystemBrowserType.Chrome => FindByName("chrome.exe"),
		SystemBrowserType.Brave => FindByName("brave.exe"),
		SystemBrowserType.Firefox => FindByName("firefox.exe"),
		_ => throw new NotSupportedException("Browser type not found."),
	};
}

//TODO
//using Microsoft.Win32;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;

//public static class ApplicationChecker {
//	public static Dictionary<string, string> FindInstalledApplications(string[] executableNames)
//	{
//		var results = new Dictionary<string, string>();

//		foreach (var executableName in executableNames) {
//			var (isInstalled, filePath) = CheckApplication(executableName);
//			if (isInstalled) {
//				results[executableName] = filePath;
//			}
//		}

//		return results;
//	}

//	private static (bool IsInstalled, string FilePath) CheckApplication(string executableName)
//	{
//		// Check common installation paths
//		string[] commonPaths = {
//						Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), executableName),
//						Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), executableName)
//				};

//		foreach (string path in commonPaths) {
//			if (File.Exists(path)) {
//				return (true, path);
//			}
//		}

//		// Check registry
//		string[] registryKeys = {
//						@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
//						@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths"
//				};

//		foreach (string registryKey in registryKeys) {
//			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(Path.Combine(registryKey, executableName))) {
//				if (key != null) {
//					string path = key.GetValue(null) as string;
//					if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
//						return (true, path);
//					}
//				}
//			}
//		}

//		// Check for user-specific installation
//		string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
//		var userSpecificPaths = Directory.GetFiles(appDataPath, executableName, SearchOption.AllDirectories);
//		if (userSpecificPaths.Any()) {
//			return (true, userSpecificPaths.First());
//		}

//		// Check uninstall registry keys
//		string[] uninstallKeys = {
//						@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
//						@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
//				};

//		foreach (string uninstallKey in uninstallKeys) {
//			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(uninstallKey)) {
//				if (key != null) {
//					foreach (string subKeyName in key.GetSubKeyNames()) {
//						using (RegistryKey subKey = key.OpenSubKey(subKeyName)) {
//							string displayName = subKey.GetValue("DisplayName") as string;
//							if (!string.IsNullOrEmpty(displayName) && displayName.Contains(Path.GetFileNameWithoutExtension(executableName), StringComparison.OrdinalIgnoreCase)) {
//								string installLocation = subKey.GetValue("InstallLocation") as string;
//								if (!string.IsNullOrEmpty(installLocation)) {
//									string fullPath = Path.Combine(installLocation, executableName);
//									if (File.Exists(fullPath)) {
//										return (true, fullPath);
//									}
//								}
//							}
//						}
//					}
//				}
//			}
//		}

//		return (false, string.Empty);
//	}
//}