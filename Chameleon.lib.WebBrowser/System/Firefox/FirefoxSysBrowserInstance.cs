using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;
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

		await SysBrowserInfoUtil.AddAutoloadTemporaryAddonFF(Consts.Browser.LocalFirefoxDirPath);
	}

	protected override async Task InitializeExtensionPath()
	{
		await CreateChameleonFirefoxCopy();
		_ = await InitializePrefsJs();

		var inDir = Path.Combine(Settings.SysBrowserProfileCachePath, Consts.Browser.Foxameleon);
		await IOtil.DC(inDir);

		ExtentionsDirs.Add(Enums.ExtensionType.foxameleon, await BuildExtSettings());

		if (Settings.Profile.Proxy.CanUse) {
			ExtentionsDirs.Add(Enums.ExtensionType.foxameleon_proxy, @$"
                let settings = {{
                    enabled: true,
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
		}

		foreach (var (ext, setting) in ExtentionsDirs) {
			await _extensionLoaderService!.LoadExtension(ext, Settings.DestExtentionsDir, setting).ConfigureAwait(true);
			var extDir = Path.Combine(Settings.DestExtentionsDir, ext.ToString());
			if (Directory.Exists(extDir)) {
				await IOtil.CreateZipAsync(Path.Combine(inDir, Guid.NewGuid().ToString() + ".zip"), extDir);
			}
		}

	}

	// TODO:
	private async Task<Dictionary<string, object>> InitializePrefsJs()
	{
		var prefs = new Dictionary<string, object>() {
			["privacy.trackingprotection.enabled"] = true
		};
		foreach (var pref in SysBrowserInfoUtil.FirefoxUserPrefs) {
			if (SysBrowserInfoUtil.FirefoxDepricatedPrefs.Any(pref.Key.Contains))
				continue;
			prefs.Add(pref.Key, pref.Value);
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
					if (!prefs.ContainsKey(key)
						&& !SysBrowserInfoUtil.FirefoxDepricatedPrefs.Any(p => p == key)
						&& !key.Contains(".proxy.")) {
						prefs[key] = value;
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
			$"-url about:newtab",
			$"-profile \"{Settings.SysBrowserProfileCachePath}\""
		});
	}
}