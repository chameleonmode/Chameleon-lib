using System.Reflection;

using Chameleon.lib.Common.Interfaces.Sys;

namespace Chameleon.lib.Common.Services;
public class EmbeddedResourceAssetLoader(Assembly assembly) : IAssetLoader {
	private readonly Assembly _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));

	public Stream Open(Uri uri)
	{
		return OpenResource(uri);
	}

	public Stream OpenAsset(Uri uri)
	{
		return OpenResource(uri);
	}

	private Stream OpenResource(Uri uri)
	{
		ArgumentNullException.ThrowIfNull(uri, nameof(uri));

		var resourcePath = uri.Authority;
		var stream = _assembly.GetManifestResourceStream(resourcePath)
				?? throw new FileNotFoundException($"Embedded resource not found: {resourcePath}");
		return stream;
	}

	public IEnumerable<Uri> GetAssets(Uri uri, string? pattern)
	{
		ArgumentNullException.ThrowIfNull(uri, nameof(uri));

		var basePath = GetResourcePath(uri);
		var resources = _assembly.GetManifestResourceNames()
				.Where(x => x.StartsWith(basePath, StringComparison.OrdinalIgnoreCase));

		if (!string.IsNullOrEmpty(pattern)) {
			resources = resources.Where(x => Path.GetFileName(x).Contains(pattern));
		}

		return resources.Select(x => new Uri($"embedded://{x}"));
	}

	private string GetResourcePath(Uri uri)
	{
		if (uri.Scheme is not "avares" and not "embedded")
			throw new ArgumentException($"Unsupported URI scheme: {uri.Scheme}", nameof(uri));

		var path = uri.AbsolutePath;
		if (path.StartsWith('/'))
			path = path[1..];

		// Replace hyphens with underscores
		path = path.Replace("-", "_");

		return $"{_assembly.GetName().Name}.{path.Replace('/', '.')}";
	}
}
