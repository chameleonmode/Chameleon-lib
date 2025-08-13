using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace chameleon.assets;

// Classes to deserialize the mapping file
public record Mapping(List<Map> Files);
public record Map(string Path);

public static partial class Resources {
	[GeneratedRegex(@"[^a-zA-Z0-9_/\\.]")] private static partial Regex SpecialCharacters();

	public static string Assert(params string?[] paths) {
		var path = Path.Combine(paths!);
		Debug.WriteLine($"Assert \n{path}");
		if (!Directory.Exists(path)) _ = Directory.CreateDirectory(path);
		return path;
	}

	public static Stream? Streamer(string resource) {
		Debug.WriteLine($"Streamer \n{resource} ");
		var assembly = Assembly.GetExecutingAssembly();
		var prefix = assembly.GetName().Name + ".";
		if (!resource.StartsWith(prefix)) resource = prefix + resource;
		return assembly.GetManifestResourceStream(resource);
	}

	public static async Task<IEnumerable<Map>> Mapper() {
		using var stream = Streamer("resource-mapping.json")!;
		using var reader = new StreamReader(stream);
		var json = await reader.ReadToEndAsync();
		var mapping = json.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => new Map(x));
		// var mapping = JsonSerializer.Deserialize<Mapping>(json, options: new() {
		// 	PropertyNameCaseInsensitive = true,
		// 	AllowTrailingCommas = true,
		// })!;
		return mapping;
	}

	public static FileStream FS(string path, FileMode mode = FileMode.Create, FileAccess access = FileAccess.Write, FileShare share = FileShare.None) {
		Debug.WriteLine($"FS \n{path}");
		return new FileStream(path, mode, access, share);
	}

	public static async Task<bool> Copy(string source, string target) {
		Assert(Path.GetDirectoryName(target));

		// Extract the resource
		using (var stream = Streamer(source)!) {
			using var fs = FS(target);
			await stream.CopyToAsync(fs);
		}

		// Check if the file was copied successfully
		return File.Exists(target);
	}
	public static async Task<bool> Mapped(string source, string target) {
		Assert(target);

		// Extract each file according to the mapping
		var mapping = await Mapper();
		foreach (var map in mapping) {
			var file = map.Path;
			var name = Path.GetFileName(file);
			var resource = source + "." + SpecialCharacters()
			.Replace(map.Path, "_")
			.Replace('\\', '.')
			.Replace('/', '.')[..^name.Length] + name.Replace("..", "._");

			// Calculate the target path - preserve original filename
			var path = Path.Combine(target, file);
			var directory = Path.GetDirectoryName(path);
			Assert(directory);

			// Extract the resource
			using var mrs = Streamer(resource);
			if (mrs != null) {
				using var fs = FS(path);
				await mrs.CopyToAsync(fs);
			}else {
				Debug.WriteLine($"Resource not found: \n{path}\n{resource}");
			}
		}

		// Check if the file was copied successfully
		return Directory.Exists(target);
	}

	public static async Task<bool> Dir(string directory, string destination, bool overwrite = true) {
		var assembly = Assembly.GetExecutingAssembly();
		var prefix = assembly.GetName().Name + ".";
		var uri = $"{prefix}{directory}";
		var assets = Assembly.GetExecutingAssembly()
		.GetManifestResourceNames()
		.Where(x => x.StartsWith(uri, StringComparison.OrdinalIgnoreCase));

		foreach (var asset in assets) {
			var parts = asset.Replace(uri, "")
			.Split('.')
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToArray();
			var path = string.Join(Path.DirectorySeparatorChar, parts[..^1]) + $".{parts.Last()}";
			var dest = Path.Combine(destination, path);
			var dir = Path.GetDirectoryName(dest)!;
			Debug.WriteLine($"\nCopying\n{asset}\n{parts}\n{path}\n{dest}");
			if (!Directory.Exists(dir)) _ = Directory.CreateDirectory(dir);

			var temp = Path.GetTempFileName();
			using (var stream = Streamer(asset)!) {
				using var fs = FS(temp);
				await stream.CopyToAsync(fs);
			}
			File.Copy(temp, dest, overwrite);
		}
		return Directory.Exists(destination);
	}

	public static async Task<string> CopyFile(string prefix, string file, string dir, bool overwrite = true) {
		var dist = Path.Combine(dir, file);
		if (!overwrite && File.Exists(dist)) {
			throw new IOException($"File {dist} already exists and overwrite is set to false.");
		}
		Assert(Path.GetDirectoryName(dist));

		using var source = Streamer($"{prefix}.{file}")!;
		using (var fs = FS(dist)) {
			await source.CopyToAsync(fs);
		}
		return dist;
	}

	public static async Task<string> LoadExtension(
		ExtensionType extension, string destinationPath,
		string? settings = null,
		string? version = null
	) {

		if (Directory.Exists(destinationPath)) {
			Directory.Delete(destinationPath, true);
		}
		var assembly = Assembly.GetExecutingAssembly();
		var prefix = assembly.GetName().Name + ".";
		try {
			var assetUri = $"{prefix}addons.{extension}";
			var assets = Loader.Instance.GetAssets(assetUri).ToList();

			foreach (var asset in assets) {
				var authorityParts = asset.Split('.');
				var relativePath = GetRelativePathFromAuthority(authorityParts, $"{extension}");

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
		Assert(destDir);

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
