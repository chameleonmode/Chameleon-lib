namespace Chameleon.lib.Const;
public static class FilePaths {
	public static string AppDataDir => EnsureDirectoryExists(
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Variables.AppName)
	);
	
	public static string AppTempDir => Path.Combine(Path.GetTempPath(), Variables.AppName);
	public static string AppDownloadDir => EnsureDirectoryExists(
		Path.Combine(AppTempDir, "Downloads")
	);

	public static string EnsureDirectoryExists(string path) {
		if (!Directory.Exists(path)) {
			_ = Directory.CreateDirectory(path);
		}
		return path;
	}
}
