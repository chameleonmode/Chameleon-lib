using System.Diagnostics;
using Chameleon.lib.Common.Constants;

using Chameleon.lib.Common.Util;
using static Chameleon.lib.Common.Constants.Enums;
using Chameleon.lib.Common.Util.ThirdParty.GeoIp;
using Chameleon.lib.Common.Extensions;
using System.Text;
using Chameleon.lib.Helpers;
using Chameleon.lib.Const;
using chameleon.assets;
namespace Chameleon.lib.Common.Models;
public class EmulationOptions {
	public bool AutoTimezone { get; set; } = true;
	public bool SpoofGeoLocation { get; set; } = true;
	public bool SpoofWebGLFingerprint { get; set; } = true;
	public bool SpoofCanvasFingerprint { get; set; } = true;
	public bool SpoofClientRects { get; set; } = true;
	public bool SpoofFontFingerprint { get; set; } = true;
	public bool SpoofAudio { get; set; } = true;
	public bool DisableWebRTC { get; set; } = true;
	public bool SpoofNavigator { get; set; } = false;
}
public record SysBrowserEvent(SysBrowserOpenOptions OpenOptions, SysBrowserEventType EventType);
public record class SysBrowserRecord(string Name, string Path) {
	public override string ToString()
	{
		return Name ?? Path;
	}
}
public record SysBrowserOpenOptions(SystemBrowserType BrowserType, SysBrowserProfile Profile);
public record SysBrowserSettings(SysBrowserOpenOptions OpenOptions, EmulationOptions Emulation, string StartUrl, int Port) {
	public Enums.SystemBrowserType BrowserType => OpenOptions.BrowserType;
	public SysBrowserProfile Profile => OpenOptions.Profile;
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

	public SysBrowserEvent CreateEvent(Enums.SysBrowserEventType sysBrowserEventType) => new(OpenOptions, sysBrowserEventType);

	public Dictionary<ExtensionType, (string? settings, string guid, string destDir)> ExtentionsDirs { get; } = [];

	public async Task<string> BuildMeleonExtSettings(string extDir)
	{
		var tzSpoofing = Emulation.AutoTimezone || Emulation.SpoofGeoLocation;

		var options = new HashSet<KeyValuePair<string, string>>() {
			new ("webglSpoofing", Emulation.SpoofWebGLFingerprint.Tlwr()),
			new ("canvasProtection", Emulation.SpoofCanvasFingerprint.Tlwr()),
			new ("clientRectsSpoofing",Emulation.SpoofClientRects.Tlwr()),
			new ("fontsSpoofing", Emulation.SpoofFontFingerprint.Tlwr()),
			new ("audioSpoofing", Emulation.SpoofAudio.Tlwr()),
			new ("geoSpoofing", tzSpoofing.Tlwr()),
			new ("timezoneSpoofing", tzSpoofing.Tlwr())
		}; 
		var settingsBuilder = new StringBuilder();
		_ = settingsBuilder.AppendLine("{");
		_ = settingsBuilder.AppendLine($"\"enabled\": {options.Any(o => o.Value == "true").Tlwr()},");
		_ = settingsBuilder.AppendLine($"\"debug\":{(Debugger.IsAttached ? 5 : -1)},");
		foreach (var o in options) {
			_ = settingsBuilder.AppendLine($"\"{o.Key}\": {o.Value},");
		}
		var ipapi = await GeoIpApi.GetIpapi(
			proxy: Profile.Proxy.WebProxy,
			onretry: e => Toaster.Error(e)
		) ?? new () {	
			timezone = "America/Los_Angeles", 
			lat = 34.052235,
			lon = -118.243683,
			tzSystem = true
		};
		_ = settingsBuilder.AppendLine($"\"timezone\": \"{ipapi.timezone}\",");
		_ = settingsBuilder.AppendLine($"\"latitude\": {ipapi.lat},");
		_ = settingsBuilder.AppendLine($"\"longitude\":{ipapi.lon},");
		_ = settingsBuilder.AppendLine(
"""
"myIP": false,
"dAPI": true,
"webRtcEnabled": true,
"randomizeTZ": false,
"randomizeGeo": false,
"noiseLevel": "medium",
"eMode": "disable_non_proxied_udp",
"dMode": "default_public_interface_only",
"locale": "en-US",
"accuracy": 69.96,
"bypass": [],
"history": []
}
"""
);
		await File.WriteAllTextAsync(Path.Combine(extDir, "settings.json"), settingsBuilder.ToString());

		return settingsBuilder.ToString();
	}

	public string BuildProxyExtSettings()
	{
		var enabled = Profile.Proxy.CanUse ? "true" : "false";

		return @$"let settings = {{
			   enabled: {enabled},
			   type: 'http',
				 server: '{Profile.Proxy.Server}',
			   host: '{Profile.Proxy.Host}',
			   port: {Profile.Proxy.Port},
			   username: '{Profile.Proxy.UserName}',
			   password: '{Profile.Proxy.Password}',
			   url: '{StartUrl}',
			   debug: true,
			}};";
	}
}
