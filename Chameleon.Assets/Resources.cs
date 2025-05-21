using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace chameleon.assets;

// Classes to deserialize the mapping file
public record Mapping(List<Map> Files);
public record Map(string OriginalPath, string ResourceName);

public static partial class Resources {
	public const string BASE = "chameleon.assets";

	public static async Task Dir(string directory, string destination, bool overwrite = true) {
		var uri = $"{BASE}.{directory}";
		var assets = Assembly.GetExecutingAssembly()
		.GetManifestResourceNames()
		.Where(x => x.StartsWith(uri, StringComparison.OrdinalIgnoreCase));

		foreach (var asset in assets) {
			var parts = asset.Replace(uri, "")
			.Split('.')
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x =>
				x.StartsWith('_')
				? x.Replace(x[0], '@')
				: x is not "node_modules" and not "third_party"
				? x.Replace('_', '-')
				: x
			).ToArray();
			var path = string.Join(Path.DirectorySeparatorChar, parts[..^1]) + $".{parts.Last()}";
			var dest = Path.Combine(destination, path);
			var dir = Path.GetDirectoryName(dest);
			Debug.WriteLine($"\nCopying\n{asset}\n{parts}\n{path}\n{dest}");
			ArgumentNullException.ThrowIfNull(dir);
			if (!Directory.Exists(dir)) _ = Directory.CreateDirectory(dir);

			var temp = Path.GetTempFileName();
			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(asset)) {
				ArgumentNullException.ThrowIfNull(stream);
				using var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None);
				await stream.CopyToAsync(fs);
			}
			File.Copy(temp, dest, overwrite);
		}
	}

	public static async Task<bool> Copy(string file, string target, bool overwrite = true) {
		if (!overwrite && File.Exists(target)) throw new IOException($"File {target} already exists and overwrite is set to false.");
		// Ensure the directory exists
		_ = Directory.CreateDirectory(Path.GetDirectoryName(target)!);

		var assembly = Assembly.GetExecutingAssembly();
		var prefix = assembly.GetName().Name + ".";
		var uri = $"{prefix}{file}";

		// Check if the resource exists
		var resource = assembly
		.GetManifestResourceNames()
		.First(x => x.StartsWith(uri, StringComparison.OrdinalIgnoreCase))!;

		// Extract the resource
		using (var stream = assembly.GetManifestResourceStream(resource)!) {
			using var fs = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None);
			await stream.CopyToAsync(fs);
		}

		// Check if the file was copied successfully
		return File.Exists(target);
	}
	public static async Task<bool> Mapped(string source, string target) {
		_ = Directory.CreateDirectory(target);
		var assembly = Assembly.GetExecutingAssembly();
		var prefix = assembly.GetName().Name + ".";

		using var stream = assembly.GetManifestResourceStream(prefix + "resource-mapping.json")!;
		using var reader = new StreamReader(stream);
		var json = await reader.ReadToEndAsync();
		var mapping = JsonSerializer.Deserialize<Mapping>(json, options: new() {
			PropertyNameCaseInsensitive = true,
			AllowTrailingCommas = true,
		})!;
		await stream.DisposeAsync();

		// Extract each file according to the mapping
		foreach (var map in mapping.Files) {
			var file = map.OriginalPath;
			var resource = prefix + SpecialCharacters()
			.Replace(map.ResourceName, "_")
			.Replace('\\', '.')
			.Replace('/', '.')
			.Replace("..", "._");
			if (!resource.StartsWith(prefix + source)) continue;

			// Calculate the target path - preserve original filename
			var destination = Path.Combine(target, file);
			_ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

			// Extract the resource
			Debug.WriteLine($"\nCopying \n{resource} \nto {destination}");
			using var mrs = assembly.GetManifestResourceStream(resource)!;
			if (mrs == null)
				continue;
			
			using var fs = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
			await mrs.CopyToAsync(fs);
		}

		// Check if the file was copied successfully
		return Directory.Exists(target);
	}

	public static async Task<string> CopyFile(string prefix, string file, string dir, bool overwrite = true) {
		var dist = Path.Combine(dir, file);
		if (!overwrite && File.Exists(dist)) {
			throw new IOException($"File {dist} already exists and overwrite is set to false.");
		}

		using var source = Loader.Instance.Open($"{BASE}.{prefix}.{file}");
		using (var fs = new FileStream(dist, FileMode.Create, FileAccess.Write, FileShare.None)) {
			await source.CopyToAsync(fs);
		}
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
		ArgumentNullException.ThrowIfNull(destDir);

		if (!Directory.Exists(destDir)) _ = Directory.CreateDirectory(destDir);

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

	[GeneratedRegex(@"[^a-zA-Z0-9_/\\.]")]
	private static partial Regex SpecialCharacters();

}
