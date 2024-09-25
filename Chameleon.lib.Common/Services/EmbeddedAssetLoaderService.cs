using System.Reflection;

using Chameleon.lib.Common.Interfaces.Sys;

namespace Chameleon.lib.Common.Services;
internal class EmbeddedAssetLoaderService(Assembly assembly) : IAssetLoader {
	private readonly Assembly _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));

	public Stream Open(Uri uri)
	{
		return OpenResource(uri, GetResourcePath);
	}

	public Stream OpenAsset(Uri uri)
	{
		return OpenResource(uri, u => u.Authority.Replace("chameleon.app.addons.assets", "Chameleon.app.Addons.Assets"));
	}

	private Stream OpenResource(Uri uri, Func<Uri, string> resourcePathFunc)
	{
		ArgumentNullException.ThrowIfNull(uri, nameof(uri));

		var resourcePath = resourcePathFunc(uri);
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
		if (uri.Scheme != "avares" && uri.Scheme != "embedded")
			throw new ArgumentException($"Unsupported URI scheme: {uri.Scheme}", nameof(uri));

		var path = uri.AbsolutePath;
		if (path.StartsWith('/'))
			path = path[1..];

		// Replace hyphens with underscores
		path = path.Replace("-", "_");

		return $"{_assembly.GetName().Name}.{path.Replace('/', '.')}";
	}
}

