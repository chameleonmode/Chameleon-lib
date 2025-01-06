using chameleon.assets;

using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.ServiceManagers;
using Chameleon.lib.Common.Util;

namespace Chameleon.lib.WebBrowser.Services;
public static class ExtensionLoaderService {
	public static async Task<string> LoadExtension(Enums.ExtensionType extensionType, string destinationPath, string? settings = null, string? version = null)
	{
		try {
			var extensionName = extensionType.ToString();
			var assetUri = new Uri($"{Consts.Addons.AddonsEmbeddedDir}/{extensionName}");
			var assets = Loader.Instance.GetAssets(assetUri).ToList();

			foreach (var asset in assets) {
				var authorityParts = asset.Authority.Split('.');
				var relativePath = IOtil.GetRelativePathFromAuthority(authorityParts, extensionName);

				await IOtil.CopyFromStream(
						Loader.Instance.Open(asset),
						destinationPath, relativePath,
						relativePath.EndsWith("background.js") ? settings : null,
						relativePath.EndsWith("manifest.json") ? version : null);
			}
		} catch (Exception ex) {
			Toaster.Error("Failed to load extension", ex.Message);
			throw;
		}

		return Path.Combine(destinationPath, extensionType.ToString());
	}
}