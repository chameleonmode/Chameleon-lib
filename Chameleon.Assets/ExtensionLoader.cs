using System.Text;
using System.Text.Json;

namespace chameleon.assets;
public static class EmbeddedLoader {
	public const string BASE = "chameleon.assets";

	public static async Task LoadFiles(string directory, string destination) {
		var assetUri = $"{BASE}.{directory}";
		var assets = Loader.Instance.GetAssets(assetUri);

		foreach (var asset in assets) {
			var relativePath = GetRelativePathFromAuthority(asset.Split('.'), directory);
			var tempFilePath = Path.GetTempFileName();

			using var stream = Loader.Instance.Open(asset);
			using var tempFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
			await stream.CopyToAsync(tempFileStream);
			File.Copy(tempFilePath, Path.Combine(destination, relativePath), true);
		}
	}

	public static async Task<string> CopyFile(string prefix, string file, string dir, bool overwrite = true) {
		var dist = Path.Combine(dir, file);
		if (!overwrite && File.Exists(dist)) {
			throw new IOException($"File {dist} already exists and overwrite is set to false.");
		}

		using var source = Loader.Instance.Open($"{BASE}.{prefix}.{file}");
		using var fs = new FileStream(dist, FileMode.Create, FileAccess.Write, FileShare.None); 
		await source.CopyToAsync(fs);
		return dist;
	}

	public static async Task<string> LoadExtension(
		ExtensionType extension, 
		string destinationPath, 
		string? settings = null, 
		string? version = null
	) {
		try {
			var assetUri = $"{BASE}.addons.{extension}";
			var assets = Loader.Instance.GetAssets(assetUri).ToList();

			foreach (var asset in assets) {
				var authorityParts = asset.Split('.');
				var relativePath = GetRelativePathFromAuthority(authorityParts,  $"{extension}");

				await CopyFromStream(
					Loader.Instance.Open(asset),
					destinationPath,
					relativePath,
					relativePath.EndsWith("background.js") ? settings : null,
					relativePath.EndsWith("manifest.json") ? version : null
				);
			}
		} catch (Exception ex) {
			Console.WriteLine($"Unexpected error: {ex.Message}");
			throw; // Re-throw unexpected exceptions
		}

		return Path.Combine(destinationPath, $"{extension}");
	}

	public static string GetRelativePathFromAuthority(string[] authorityParts, string? relitiveTo = null) {
		var path = authorityParts.First().Replace('/', Path.DirectorySeparatorChar);
		if (authorityParts.Length > 1) {
			var relativePath = string.Join("/", authorityParts.Take(authorityParts.Length - 1)) + "." + authorityParts.Last();
			path = relativePath.Replace('/', Path.DirectorySeparatorChar);
		}
		return relitiveTo == null ? path : path[path.IndexOf(relitiveTo)..];
	}

	public static async Task CopyFromStream(Stream inputStream, string targetDir, string relativePath, string? header = null, string? version = null) {
		var desPath = Path.Combine(targetDir, relativePath);
		var destDir = Path.GetDirectoryName(desPath);
		ArgumentNullException.ThrowIfNull(destDir);

		if (!Directory.Exists(destDir)) 			
		  _ = Directory.CreateDirectory(destDir);

		var tempFilePath = Path.GetTempFileName();
		try {
			using (var tempFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
				if (!string.IsNullOrWhiteSpace(header)) {
					var headerBytes = Encoding.UTF8.GetBytes(header!);
					await tempFileStream.WriteAsync(headerBytes);
				}

				await inputStream.CopyToAsync(tempFileStream);
			}

			if (!string.IsNullOrWhiteSpace(version)) {
				// Read the JSON file
				var jsonString = await File.ReadAllTextAsync(tempFilePath);
				var manifest = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

				if (manifest != null && manifest.TryGetValue("version", out var value)) {
					manifest["version"] = version!;

					// Serialize back to JSON
					jsonString = JsonSerializer.Serialize(manifest);
				}

				await File.WriteAllTextAsync(desPath, jsonString);
			} else {
				File.Copy(tempFilePath, desPath, true);
			}
		} finally {
			File.Delete(tempFilePath);
			inputStream.Dispose();
		}
	}
}
