using Chameleon.lib.Common.Enums;
using Chameleon.lib.Common.Models;
using Chameleon.lib.Common.Util;
using Chameleon.lib.WebBrowser.Util;

namespace Chameleon.lib.WebBrowser.Models;
public record SysBrowserOpenOptions(SystemBrowserType BrowserType, UserProfileModel Profile);
public record SysBrowserLaunchOptions(SysBrowserOpenOptions OpenOptions, EmulationOptions Emulation, string StartUrl, int Port) {
	public SystemBrowserType BrowserType => OpenOptions.BrowserType;
	public UserProfileModel Profile => OpenOptions.Profile;

	public string SysBrowserProfileCachePath {
		get {
			return IOtil.EnsureDirectoryExists(
				Path.Combine(Consts.AppDataDir, BrowserType.ToString(), Profile.Id.ToString()));
		}
	}
	public string SysBrowseUserExtDir {
		get {
			return BrowserType switch { 
				SystemBrowserType.Chrome => Consts.Addons.DefaultExtensionsFolderPath_Chrome,
				SystemBrowserType.Brave => Consts.Addons.DefaultExtensionsFolderPath_Brave, 
				SystemBrowserType.Firefox => Consts.Addons.DefaultExtensionsFolderPath_FF,
				_ => throw new IndexOutOfRangeException(nameof(BrowserType))
			};
		}
	}

	private string? _destextPath;
	public string DestExtentionsDir {
		get {
			if (_destextPath == null) {
				_destextPath = Path.Combine(Consts.Addons.AddonExtentionDir, BrowserType.ToString(), Profile.Id.ToString());
				IOtil.DeleteDExists(_destextPath);
				_destextPath = IOtil.EnsureDirectoryExists(Path.Combine(_destextPath, Guid.NewGuid().ToString()));
			}
			return _destextPath;
		}
	}

	public string ExePath => SysBrowserInfoUtil.FindByType(BrowserType).Path;
}
