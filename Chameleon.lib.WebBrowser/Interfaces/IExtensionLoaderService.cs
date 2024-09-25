using Chameleon.lib.Common.Enums;

namespace Chameleon.lib.WebBrowser.Interfaces;
public interface IExtensionLoaderService {
	Task LoadExtension(ExtensionType extensionType, string destinationPath, string? settings = null);
}
