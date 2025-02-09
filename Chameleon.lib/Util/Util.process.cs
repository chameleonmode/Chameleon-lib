using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Chameleon.lib.Util;
public static class ProcessUtil {
	public static void OpenBrowser(string Url) {
		try {
			_ = Process.Start(Url);
		} catch {
			// hack because of this: https://github.com/dotnet/corefx/issues/10361
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				_ = Process.Start(new ProcessStartInfo("cmd", $"/c start {Url.Replace("&", "^&")}") { CreateNoWindow = true });
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				_ = Process.Start("xdg-open", Url);
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				_ = Process.Start("open", Url);
			} else {
				throw;
			}
		}
	}

	/// <summary>
	/// Opens the specified folder in the system file explorer.
	/// </summary>
	/// <param name="folderPath">The full path to the folder to open.</param>
	/// <exception cref="PlatformNotSupportedException">Thrown if the current OS is not Windows or macOS.</exception>
	public static void OpenFolder(string folderPath) {
		if (string.IsNullOrWhiteSpace(folderPath))
			throw new ArgumentException("Folder path cannot be null or whitespace.", nameof(folderPath));

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			// For Windows, use Explorer.exe.
			_ = Process.Start(new ProcessStartInfo {
				FileName = "explorer.exe",
				Arguments = $"\"{folderPath}\"", // Enclose in quotes to handle spaces in the path.
				UseShellExecute = true
			});
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
			// For macOS, use the 'open' command.
			_ = Process.Start("open", folderPath);
		} else {
			throw new PlatformNotSupportedException("This platform is not supported.");
		}
	}
}
