using System.Diagnostics;

using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.ServiceManagers;
using Chameleon.lib.Common.Util;
using Chameleon.lib.WebBrowser.Util;

namespace Chameleon.lib.WebBrowser.System.Firefox;
public class FirefoxSysBrowserInstance : SysBrowserInstance {
	private async Task CreateChameleonFirefoxCopy()
	{
		if (IOtil.IsNeedUpdate(Settings.ExePath, Consts.Browser.LocalFirefoxExePath)) {
			await IOtil.DeleteDExistsAsync(Consts.Browser.LocalFirefoxDirPath);

			await IOtil.CopyFolderAsync(OperatingSystem.IsMacOS()
				? "Applications/firefox.app"
				: Path.GetDirectoryName(Settings.ExePath)!, Consts.Browser.LocalFirefoxDirPath);

			await Task.Delay(1000);
		}

		await SysBrowserInfoUtil.AddAutoloadTemporaryAddonFF(Settings.SysBrowserProfileCachePath);
	}

	protected override async Task InitializeExtensionPath()
	{
		await CreateChameleonFirefoxCopy();
		_ = await InitializePrefsJs();

		var inDir = Path.Combine(Settings.SysBrowserProfileCachePath, Consts.Browser.Foxameleon);
		var versionFile = Path.Combine(inDir, "version.txt");
		var version = "2024.1.7.2";
		if (File.Exists(versionFile)) {
			var fileVersion = await File.ReadAllTextAsync(versionFile);
			if(fileVersion.Is()) version = IOtil.IncrementVersion(fileVersion);
		}
		await IOtil.DC(inDir);
		await File.WriteAllTextAsync(versionFile, version);

		//
		ExtentionsDirs.Add(Enums.ExtensionType.foxameleon, await BuildExtSettings());

		//
		ExtentionsDirs.Add(Enums.ExtensionType.foxameleon_proxy, @$"
                let settings = {{
                    enabled: {Settings.Profile.Proxy.CanUse.Tlwr()},
                    type: 'http',
                    host: '{Settings.Profile.Proxy.Host}',
                    port: {Settings.Profile.Proxy.Port},
										server: '{Settings.Profile.Proxy.Server}',
                    username: '{Settings.Profile.Proxy.UserName}',
                    password: '{Settings.Profile.Proxy.Password}',
                    url: '{Settings.StartUrl}',
                    debug: false,
                }};
            ");

		var foxyFname = Guid.NewGuid().ToString() + ".xpi";
		var proxyFname = Guid.NewGuid().ToString() + ".xpi";
		foreach (var (ext, setting) in ExtentionsDirs) {
			await _extensionLoaderService!.LoadExtension(ext, Settings.DestExtentionsDir, setting, version).ConfigureAwait(true);
			var extDir = Path.Combine(Settings.DestExtentionsDir, ext.ToString());
			if (Directory.Exists(extDir)) {
				await IOtil.CreateZipAsync(Path.Combine(inDir, ext == Enums.ExtensionType.foxameleon ? foxyFname : proxyFname), extDir);
			}
		}

//		var policy =
//@$"
//{{
//   ""policies"": {{
//		""ExtensionSettings"": {{
//		  ""uBlock0@raymondhill.net"": {{
//		    ""installation_mode"": ""normal_installed"",
//		    ""install_url"": ""https://addons.mozilla.org/firefox/downloads/latest/ublock-origin/latest.xpi""  
//		  }},
//		  ""adguardadblocker@adguard.com"": {{
//		    ""installation_mode"": ""normal_installed"",
//		    ""install_url"": ""https://addons.mozilla.org/firefox/downloads/latest/adguardadblocker@adguard.com/latest.xpi"" 
//		  }}
//		}}
//	}}
//}}";

		//-Place the `policies.json` file in the appropriate directory based on the operating system:
		//   -**Windows:** `C:\Program Files\Mozilla Firefox\distribution\policies.json`
		//   -**macOS:** `/ Applications / Firefox.app / Contents / Resources / distribution / policies.json`
		//   -**Linux:** `/ usr / lib / firefox / distribution / policies.json` or similar.
		//var distributionDir = Path.Combine(Consts.Browser.LocalFirefoxDirPath, "distribution");
		//Directory.CreateDirectory(distributionDir);
		//File.WriteAllText(Path.Combine(distributionDir, "policies.json"), policy);
	}

	// TODO:
	private async Task<Dictionary<string, string>> InitializePrefsJs()
	{
		var prefs = SysBrowserInfoUtil.FirefoxUserPrefs.ToDictionary();
		foreach (var p in new List<KeyValuePair<string, string>> {
			SysBrowserInfoUtil.user_pref("privacy.trackingprotection.enabled", true),
			SysBrowserInfoUtil.user_pref("browser.shell.checkDefaultBrowser", false),
			SysBrowserInfoUtil.user_pref("app.update.service.enabled", false),
			//"https://arkenfox.github.io/TZP/tzp.html"
			SysBrowserInfoUtil.user_pref("browser.startup.homepage", Settings.StartUrl),
			SysBrowserInfoUtil.user_pref("browser.contentblocking.category", "strict"),
			SysBrowserInfoUtil.user_pref("privacy.fingerprintingProtection.overrides",Settings.Emulation.AutoTimezone && Settings.Profile.Proxy.CanUse ? "+JSDateTimeUTC" : ""),
			SysBrowserInfoUtil.user_pref("network.http.referer.XOriginTrimmingPolicy","0"),
			SysBrowserInfoUtil.user_pref("browser.startup.page", Debugger.IsAttached ? 3 : 1),
			//SysBrowserInfoUtil.user_pref("extensions.webextensions.uuids", ""),
			//SysBrowserInfoUtil.user_pref("browser.uiCustomization.state", ""),
		}) {
			prefs[p.Key] = p.Value;
		}
		// Define a regular expression pattern to extract key-value pairs
		var regex = Regexers.UserPrefRegex();

		var prefsFilePath = Path.Combine(Settings.SysBrowserProfileCachePath, "prefs.js");
		if (File.Exists(prefsFilePath)) {
			foreach (var userPref in await File.ReadAllLinesAsync(prefsFilePath)) {
				if (!userPref.Is()) continue;
				// Match the pattern in the input string
				var match = regex.Match(userPref);

				// If the pattern is found, extract key-value pairs
				if (match.Success) {
					var key = match.Groups[1].Value;
					var value = match.Groups[2].Value.Trim('"');

					// Add key-value pair to the dictionary
					if (!prefs.Any(p=>p.Key.Contains(key))
						&& !SysBrowserInfoUtil.FirefoxDepricatedPrefs.Any(p => p == key)
						&& !key.Contains(".proxy.")
						&& !key.Contains("extensions.webextensions.ExtensionStorageIDB.migrated")) {
						Debug.WriteLine(key + " " + value);
						var p = SysBrowserInfoUtil.user_pref(key, value);
						prefs[p.Key] = p.Value;
					}
				}
			}

			File.Delete(prefsFilePath);
		}

		List<string> filePrefs = [];
		foreach (var item in prefs) {
			filePrefs.Add($"{item.Key} {item.Value}");
		}
		await File.WriteAllLinesAsync(prefsFilePath, filePrefs);
		return prefs;
	}

	protected override string GetCommandLineArguments()
	{
		return string.Join(" ", new List<string> {
			"-new-instance",
			"-no-remote",
			"-wait-for-browser",
			$"-profile \"{Settings.SysBrowserProfileCachePath}\""
		});
	}
}