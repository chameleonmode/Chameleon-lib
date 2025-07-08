using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Chameleon.lib.WebBrowser.Services;

public static class BrowserInfo {

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
         using var key = Registry.LocalMachine.OpenSubKey(Path.Combine(registryKey, executable));
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
         using var key = Registry.LocalMachine.OpenSubKey(uninstallKey);
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

   static BrowserRecord FindByName(string executable) {
      if (OperatingSystem.IsMacOS()) {
         var chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
         var bravePath = "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser";
         var firefoxPath = "/Applications/firefox.app/Contents/MacOS/firefox";

         return executable switch {
            "chrome.exe" => File.Exists(chromePath) ? new BrowserRecord("chrome", chromePath) : null,
            "brave.exe" => File.Exists(bravePath) ? new BrowserRecord("brave", bravePath) : null,
            "firefox.exe" => File.Exists(firefoxPath) ? new BrowserRecord("brave", firefoxPath) : null,
            _ => null
         } ?? throw new NotSupportedException(
               $"{char.ToUpper(executable[0]) + executable[1..]} browser is not installed.");
      } else if (OperatingSystem.IsWindows()) {
         var (installed, filepath) = CheckApplication(executable);
         if (installed && !string.IsNullOrWhiteSpace(filepath)) return new BrowserRecord(executable, filepath);
      }

      throw new NotSupportedException(
            $"{char.ToUpper(executable[0]) + executable[1..]} browser is not installed.");
   }

   public static BrowserRecord Find(SystemBrowserType BrowserType) => BrowserType switch {
      SystemBrowserType.Chrome => FindByName("chrome.exe"),
      SystemBrowserType.Brave => FindByName("brave.exe"),
      SystemBrowserType.Firefox => FindByName("firefox.exe"),
      _ => throw new NotSupportedException("Browser type not found."),
   };
}

// private string GetChromeExecutablePath()
// {
//     if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//     {
//         string[] possiblePaths = {
//             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
//             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe")
//         };

//         return possiblePaths.FirstOrDefault(File.Exists) ?? ExePath; // Fall back to existing ExePath
//     }
//     else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
//     {
//         string[] possiblePaths = {
//             "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
//             "/Applications/Chromium.app/Contents/MacOS/Chromium"
//         };

//         return possiblePaths.FirstOrDefault(File.Exists) ?? ExePath;
//     }
//     else // Linux
//     {
//         string[] possiblePaths = {
//             "/usr/bin/google-chrome",
//             "/usr/bin/chromium-browser",
//             "/usr/bin/chromium"
//         };

//         return possiblePaths.FirstOrDefault(File.Exists) ?? ExePath;
//     }
// }