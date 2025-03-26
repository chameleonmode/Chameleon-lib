using System.Reflection;

namespace chameleon.assets;
public class Loader {
	Loader() { }
	readonly Assembly assembly = Assembly.GetExecutingAssembly();

	public IEnumerable<string> GetAssets(string uri) {
		var basePath = $"{assembly.GetName().Name}.{uri}";
		var names = assembly
			.GetManifestResourceNames();
		return assembly
			.GetManifestResourceNames()
			.Where(x => x.StartsWith(uri, StringComparison.OrdinalIgnoreCase));
	}

	public Stream Open(string uri) {
		var stream = assembly.GetManifestResourceStream(uri)
				?? throw new FileNotFoundException($"Embedded resource not found: {uri}");
		return stream;
	}

	public static Loader Instance { get; } = new Loader();
}