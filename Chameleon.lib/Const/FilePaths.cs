using Chameleon.lib.Helpers;

namespace Chameleon.lib.Const;
public static class FilePaths {
	public static string AppDataDir => EnsureDirectoryExists(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Variables.AppName
	);
	public static string AppDataLocalDir => EnsureDirectoryExists(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Variables.AppName
	);
	public static string AppTempDir => EnsureDirectoryExists(
		Path.GetTempPath(), Variables.AppName
	);
	public static string AppTempScripts => EnsureDirectoryExists(
		AppTempDir, "Playwright"
	);
	public static string AppDownloadDir => EnsureDirectoryExists(
		AppTempDir, "Downloads"
	);

	public static string EnsureDirectoryExists(params string[] paths) {
		var path = Path.Combine(paths);
		try{
			if (!Directory.Exists(path)) {
				return Directory.CreateDirectory(path).FullName;
			}
		} catch (Exception ex) {
			Toaster.Error($"Error creating directory: {ex.Message}");
		}
		return path;
	}
}
