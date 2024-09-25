using Chameleon.lib.Common.Enums;
using Chameleon.lib.Common.Models;
using Chameleon.lib.Common.Util;

namespace Chameleon.lib.WebBrowser.Models;
public record SysBrowserOpenOptions(SystemBrowserType BrowserType, UserProfile Profile);
public record SysBrowserLaunchOptions(SysBrowserOpenOptions OpenOptions, EmulationOptions Emulation, string StartUrl, int Port) {
	public SystemBrowserType BrowserType => OpenOptions.BrowserType;
	public UserProfile Profile => OpenOptions.Profile;

	public string SysBrowserProfileCachePath {
		get {
			return IOtil.EnsureDirectoryExists(
				Path.Combine(Consts.AppDataDir, BrowserType.ToString(), Profile.Id.ToString()));
		}
	}

	private string? _destextPath;
	public string DestExtentionsDir {
		get {
			if (_destextPath == null) {
				_destextPath = Path.Combine(Consts.Addons.AddonExtentionDir, BrowserType.ToString(), Profile.Id.ToString());
				IOtil.DC(_destextPath).Wait();
				_destextPath = IOtil.EnsureDirectoryExists(Path.Combine(_destextPath, new Guid().ToString()));
			}
			return _destextPath;
		}
	}
}
