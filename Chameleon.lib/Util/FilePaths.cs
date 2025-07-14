using System.Diagnostics;
using System.Runtime.InteropServices;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Util;

public static class FilePaths {
	public static string AppDataDir => EnsureDirectoryExists(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), IoC.AppName
	);
	public static string AppDataLocalDir => EnsureDirectoryExists(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), IoC.AppName
	);
	public static string AppTempDir => EnsureDirectoryExists(
		Path.GetTempPath(), IoC.AppName
	);
	public static string AppDownloadDir => EnsureDirectoryExists(
		AppTempDir, "Downloads"
	);


	public static string EnsureDirectoryExists(params string[] paths) {
		var path = Path.Combine(paths);
		try {
			if (!Directory.Exists(path)) return Directory.CreateDirectory(path).FullName;
		} catch (Exception ex) {
			Toaster.Error($"Error creating directory: {ex.Message}");
		}
		return path;
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
