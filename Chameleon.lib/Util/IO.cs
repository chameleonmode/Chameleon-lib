using System.IO.Compression;

namespace Chameleon.lib.Util;
public static class IOtil {
	public static Task<string> DC(string dir) => Task.Run(() => {
		DeleteDir(dir);
		return EnsureDirectoryExists(dir);
	});

	public static Task CreateDirectoryAsync(string path) => Task.Run(() => {
		_ = EnsureDirectoryExists(path);
	});
	public static string EnsureDirectoryExists(string path) {
		return FilePaths.EnsureDirectoryExists(path);
	}

	public static Task CreateZipAsync(string sourceDir, string zipDir, string? zipFile = null, bool dc = true) => Task.Run(async () => {
		if (!Directory.Exists(sourceDir)) 			throw new DirectoryNotFoundException($"The directory '{sourceDir}' does not exist.");

		if (dc) await DC(zipDir);
		else if (!Directory.Exists(zipDir)) _ = Directory.CreateDirectory(zipDir);

		zipFile ??= Guid.NewGuid() + ".zip";

		ZipFile.CreateFromDirectory(sourceDir, Path.Combine(zipDir, zipFile), CompressionLevel.Fastest, false);
	});

	public static Task CreateZipAsync(string filePath, Dictionary<string, string> files)
					=> Task.Run(async () => {
						using var fileStream = new FileStream(filePath, FileMode.CreateNew);
						using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true);
						foreach (var file in files) {
							var zipArchiveManifest = archive.CreateEntry(file.Key, CompressionLevel.Fastest);
							using var zipStream = zipArchiveManifest.Open();
							using var writer = new StreamWriter(zipStream);
							await writer.WriteAsync(file.Value);
							writer.Close();
							zipStream.Close();

							await DeleteFExists(Path.Combine(filePath, file.Key));
						}
					});
	public static string[] ReadDirectory(string path) => Directory.Exists(path) ? Directory.GetFiles(path) : [];
	public static Task<string[]> ReadDirectoryAsync(string path)
					=> Task.Run(() => ReadDirectory(path));

	public static async Task DeleteFExists(string filePath) {
		if (File.Exists(filePath)) {
			try {
				await Task.Run(() => File.Delete(filePath));
			} catch (IOException ex) {
				// Handle I/O exception, e.g., log it
				Console.WriteLine($"I/O error occurred: {ex.Message}");
			} catch (UnauthorizedAccessException ex) {
				// Handle unauthorized access exception, e.g., log it
				Console.WriteLine($"Access error occurred: {ex.Message}");
			} catch (Exception ex) {
				// Handle any other exception, e.g., log it
				Console.WriteLine($"An error occurred: {ex.Message}");
			}
		}
	}

	public static Task DirectoryDelete(string filePath, bool recuersive = true) => Task.Run(() => DeleteDir(filePath, recuersive));

	public static void DeleteDir(string filePath, bool recuersive = true) {
		if (Directory.Exists(filePath)) {
			try {
				Directory.Delete(filePath, recuersive);
			} catch (IOException ex) {
				// Handle I/O exception, e.g., log it
				Console.WriteLine($"I/O error occurred: {ex.Message}");
			} catch (UnauthorizedAccessException ex) {
				// Handle unauthorized access exception, e.g., log it
				Console.WriteLine($"Access error occurred: {ex.Message}");
			} catch (Exception ex) {
				// Handle any other exception, e.g., log it
				Console.WriteLine($"An error occurred: {ex.Message}");
			}
		}
	}

	public static Task CopyDirectory(string source, string destination) => Task.Run(() => {
		// Create the destination directory if it doesn't exist
		_ = Directory.CreateDirectory(destination);

		// Get all files in the source directory and subdirectories
		var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);

		foreach (var file in files) {
			// Compute the relative path of the file from the source directory
			var relativePath = file[source.Length..].TrimStart(Path.DirectorySeparatorChar);

			// Compute the destination path
			var destFile = Path.Combine(destination, relativePath);

			// Create the directory structure for the destination file if it doesn't exist
			var destDir = Path.GetDirectoryName(destFile);
			if (!string.IsNullOrWhiteSpace(destDir) && !Directory.Exists(destDir)) 				_ = Directory.CreateDirectory(destDir);

			// Copy the file
			File.Copy(file, destFile, true);
		}
	});

	public static Task CopyFolderAsync(string directory, string directoryForCopy) =>
		Task.Run(() => CopyFolder(directory, directoryForCopy));
	public static void CopyFolder(string directory, string directoryForCopy) {
		_ = Directory.CreateDirectory(directoryForCopy);

		var filePaths = Directory.GetFiles(directory);
		foreach (var filePath in filePaths) {
			var fileName = Path.GetFileName(filePath);
			var newFile = Path.Combine(directoryForCopy, fileName);

			File.Copy(filePath, newFile);
		}

		var subdirectoryPaths = Directory.GetDirectories(directory);
		foreach (var subdirectory in subdirectoryPaths) {
			var subdirectoryName = Path.GetFileName(subdirectory);
			var newSubdirectory = Path.Combine(directoryForCopy, subdirectoryName);

			CopyFolder(subdirectory, newSubdirectory);
		}
	}

	public static async Task WriteTextToFileAsync(string filePath, string content, int maxRetries = 3, int delayMilliseconds = 1000) {
		if (string.IsNullOrWhiteSpace(filePath))
			throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

		if (content == null)
			throw new ArgumentNullException(nameof(content), "Content cannot be null.");

		var attempt = 0;

		while (attempt < maxRetries) {
			try {
				await File.WriteAllTextAsync(filePath, content);
				Console.WriteLine("File written successfully.");
				return; // Exit if write is successful
			} catch (UnauthorizedAccessException ex) {
				Console.WriteLine($"Access denied: {ex.Message}");
				break; // Don't retry on access denied
			} catch (DirectoryNotFoundException ex) {
				Console.WriteLine($"Directory not found: {ex.Message}");
				break; // Don't retry if the directory doesn't exist
			} catch (IOException ex) {
				attempt++;
				Console.WriteLine($"IO error (attempt {attempt}): {ex.Message}");
				if (attempt >= maxRetries) 					throw; // Re-throw the exception if maximum retries are reached
				await Task.Delay(delayMilliseconds); // Wait before retrying
			} catch (Exception ex) {
				Console.WriteLine($"Unexpected error: {ex.Message}");
				throw; // Re-throw unexpected exceptions
			}
		}
	}

	public static string IncrementVersion(string version) {
		// Example: Increment the minor version for simplicity
		var parts = version.Split('.');
		if (parts.Length >= 2 && int.TryParse(parts[^1], out var buildNumber)) 			parts[^1] = (buildNumber + 1).ToString();
		return string.Join('.', parts);
	}
}

