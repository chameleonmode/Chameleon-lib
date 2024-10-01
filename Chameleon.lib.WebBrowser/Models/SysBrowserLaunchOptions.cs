using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Models;
using Chameleon.lib.Common.Util;
using Chameleon.lib.WebBrowser.Util;

using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.WebBrowser.Models;
public record SysBrowserOpenOptions(Enums.SystemBrowserType BrowserType, UserProfileModel Profile);
public record SysBrowserSettings(SysBrowserOpenOptions OpenOptions, EmulationOptions Emulation, string StartUrl, int Port) {
	public Enums.SystemBrowserType BrowserType => OpenOptions.BrowserType;
	public UserProfileModel Profile => OpenOptions.Profile;
	public string SysBrowseUserExtDir => Path.Combine(Consts.Addons.DefaultExtensionsFolderPath, BrowserType.GetDescription());
	public string ExePath => SysBrowserInfoUtil.FindByType(BrowserType).Path;
	public string SysBrowserProfileCachePath => IOtil.EnsureDirectoryExists(Path.Combine(Consts.AppDataLocalDir, BrowserType.ToString(), Profile.Id.ToString()));

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


	public SysBrowserEvent CreateEvent(Enums.SysBrowserEventType sysBrowserEventType) => new(OpenOptions, sysBrowserEventType);
}
