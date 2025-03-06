using System.Reflection;

namespace chameleon.assets;
public class Loader {
	Loader() { }
	readonly Assembly assembly = Assembly.GetExecutingAssembly();

	public Stream Open(Uri uri) {
		return OpenResource(uri);
	}

	private Stream OpenResource(Uri uri) {
		var resourcePath = uri.Authority;
		var stream = assembly.GetManifestResourceStream(resourcePath)
				?? throw new FileNotFoundException($"Embedded resource not found: {resourcePath}");
		return stream;
	}

	public IEnumerable<Uri> GetAssets(Uri uri, string? pattern = null) {
		var basePath = GetResourcePath(uri);
		var resources = assembly
			.GetManifestResourceNames()
			.Where(x => x.StartsWith(basePath, StringComparison.OrdinalIgnoreCase));

		if (!string.IsNullOrEmpty(pattern)) {
			resources = resources.Where(x => Path.GetFileName(x).Contains(pattern));
		}

		return resources.Select(x => new Uri($"embedded://{x}"));
	}

	private string GetResourcePath(Uri uri) {
		if (uri.Scheme is not "embedded")
			throw new ArgumentException($"Unsupported URI scheme: {uri.Scheme}", nameof(uri));

		var path = uri.AbsolutePath;
		if (path.StartsWith('/'))
			path = path[1..];

		// Replace hyphens with underscores
		path = path.Replace("-", "_");

		return $"{assembly.GetName().Name}.{path.Replace('/', '.')}";
	}

	public static Loader Instance { get; } = new Loader();
}