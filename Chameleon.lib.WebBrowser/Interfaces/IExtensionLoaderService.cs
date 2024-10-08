using Chameleon.lib.Common.Constants;

namespace Chameleon.lib.WebBrowser.Interfaces;
public interface IExtensionLoaderService {
	Task LoadExtension(Enums.ExtensionType extensionType, string destinationPath, string? settings = null, string? version = null);
}
