using Chameleon.lib.Common.Constants;

using Chameleon.lib.Common.Util;
using static Chameleon.lib.Common.Constants.Enums;
using Chameleon.lib.Const;
using chameleon.assets;

namespace Chameleon.lib.WebBrowser.Models;
public record SysBrowserEvent(SysBrowserOpenOptions OpenOptions, SysBrowserEventType EventType);
public record SysBrowserSettings(SysBrowserOpenOptions OpenOptions) {
	public SystemBrowserType BrowserType => OpenOptions.BrowserType;
	public BrowserProfile Profile => OpenOptions.Profile;
	public string SysBrowseUserExtDir => Path.Combine(
		Consts.Addons.DefaultExtensionsFolderPath, BrowserType.GetDescription()
		);

	public string SysBrowserProfileCachePath => IOtil.EnsureDirectoryExists(
		Path.Combine(FilePaths.AppDataLocalDir, BrowserType.ToString(), Profile.Id.ToString())
		);

	private string? destextPath;
	public string DestExtentionsDir {
		get {
			if (destextPath == null) {
				destextPath = Path.Combine(FilePaths.AppTempDir, "Addons", BrowserType.ToString(), Profile.Id.ToString());
				IOtil.DeleteDir(destextPath);
				destextPath = IOtil.EnsureDirectoryExists(Path.Combine(destextPath, Guid.NewGuid().ToString()));
			}
			return destextPath;
		}
	}
	private string? cachedExtentionsDir;
	public string CachedExtentionsDir {
		get {
			cachedExtentionsDir ??= IOtil.EnsureDirectoryExists(
				Path.Combine(FilePaths.AppDataDir, "cache", BrowserType.ToString(), Profile.Id.ToString())
			);
			return cachedExtentionsDir;
		}
	}

	public SysBrowserEvent CreateEvent(SysBrowserEventType sysBrowserEventType) => new(OpenOptions, sysBrowserEventType);

	public Dictionary<ExtensionType, (string? settings, string guid, string destDir)> ExtentionsDirs { get; } = [];
}
