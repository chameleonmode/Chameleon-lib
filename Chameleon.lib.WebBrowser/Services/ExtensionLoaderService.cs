using chameleon.assets;

using Chameleon.lib.Common.Enums;
using Chameleon.lib.Common.Managers;
using Chameleon.lib.Common.Services;
using Chameleon.lib.Common.Util;
using Chameleon.lib.WebBrowser.Interfaces;

namespace Chameleon.lib.WebBrowser.Services;
public class ExtensionLoaderService : IExtensionLoaderService {
	private readonly EmbeddedResourceAssetLoader _assetLoader = new(typeof(Constas).Assembly);
	private const string AddonsBasePath = Constas.AddonsDir;

	public async Task LoadExtension(ExtensionType extensionType, string destinationPath, string settings)
	{
		try {
			var extensionName = extensionType.ToString();
			var assetUri = new Uri($"{AddonsBasePath}/{extensionName}");
			var assets = _assetLoader.GetAssets(assetUri, null).ToList();

			foreach (var asset in assets) {
				var authorityParts = asset.Authority.Split('.');
				var relativePath = IOtil.GetRelativePathFromAuthority(authorityParts, extensionName);

				await IOtil.CopyFromStream(
						_assetLoader.OpenAsset(asset),
						destinationPath, relativePath,
						relativePath.EndsWith("background.js") ? settings : null);
			}
		} catch (Exception ex) {
			Toaster.ShowErr("Failed to load extension", ex.Message);
			throw;
		}
	}
}