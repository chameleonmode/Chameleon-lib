using Chameleon.lib.Const;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Util;
public static class FilePaths {
	public static string AppDataDir => EnsureDirectoryExists(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Project.AppName
	);
	public static string AppDataLocalDir => EnsureDirectoryExists(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Project.AppName
	);
	public static string AppTempDir => EnsureDirectoryExists(
		Path.GetTempPath(), Project.AppName
	);
	public static string AppDownloadDir => EnsureDirectoryExists(
		AppTempDir, "Downloads"
	);
	
	public static string BrowserExtensions => OperatingSystem.IsMacOS()
			? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..Resources/browser/extensions")
			: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\browser\\extensions");
	public static string Roboto => EnsureDirectoryExists(
		AppDataDir, "Roboto"
	);
	

	public static string EnsureDirectoryExists(params string[] paths) {
		var path = Path.Combine(paths);
		try {
			if (!Directory.Exists(path)) 				return Directory.CreateDirectory(path).FullName;
		} catch (Exception ex) {
			Toaster.Error($"Error creating directory: {ex.Message}");
		}
		return path;
	}
}
