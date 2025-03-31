using System.Diagnostics;

namespace Chameleon.lib.Util;
public class UMacFileVersionInfo {
  UMacFileVersionInfo() { }

  public string? FilePath { get; private set; }
  public string? ProductVersion { get; private set; }
  public string? BuildVersion { get; private set; }
  public string? BundleIdentifier { get; private set; }
  public string? ProductName { get; private set; }

  public static UMacFileVersionInfo GetVersionInfo(string filePath) {
    var info = new UMacFileVersionInfo {
      FilePath = filePath
    };

    if (Directory.Exists(filePath) && filePath.EndsWith(".app", StringComparison.OrdinalIgnoreCase)) {
      // Handle application bundles
      var plistPath = Path.Combine(filePath, "Contents", "Info.plist");
      if (File.Exists(plistPath)) {
        info.LoadFromPlist(plistPath);
      }
    } else if (File.Exists(filePath)) {
      // Handle regular files using mdls
      info.LoadFromMdls();
    }

    return info;
  }

  private void LoadFromPlist(string plistPath) {
    ProductVersion = ExecutePlistBuddy(plistPath, "CFBundleShortVersionString");
    BuildVersion = ExecutePlistBuddy(plistPath, "CFBundleVersion");
    BundleIdentifier = ExecutePlistBuddy(plistPath, "CFBundleIdentifier");
    ProductName = ExecutePlistBuddy(plistPath, "CFBundleName");
  }

  private void LoadFromMdls() {
    // For non-app files, try to get metadata using mdls
    var output = ExecuteCommand("mdls", $"\"{FilePath}\"");

    // Parse the output to extract relevant metadata
    // This is simplified and might need enhancement for specific cases
    foreach (var line in output.Split('\n')) {
      if (line.Contains("kMDItemVersion")) {
        ProductVersion = ExtractValue(line);
      }
    }
  }

  private static string ExtractValue(string line) {
    var equalsPos = line.IndexOf('=');
    if (equalsPos > 0 && equalsPos < line.Length - 1) {
      var value = line[(equalsPos + 1)..].Trim();
      // Remove quotes if present
      if (value.StartsWith('\"') && value.EndsWith('\"')) {
        value = value[1..^1];
      }
      return value;
    }
    return string.Empty;
  }

  private static string ExecutePlistBuddy(string plistPath, string property) {
    return ExecuteCommand("/usr/libexec/PlistBuddy", $"-c \"Print {property}\" \"{plistPath}\"").Trim();
  }

  private static string ExecuteCommand(string command, string arguments) {
		using var process = Process.Start(new ProcessStartInfo {
      FileName = command,
      Arguments = arguments,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    });
		if (process == null)
			return string.Empty;

		var output = process.StandardOutput.ReadToEnd();
		process.WaitForExit();
		return output;
	}
}