using System.Diagnostics;
using System.Linq;
using System.Text;

using Chameleon.lib.Common.Enums;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util;
using Chameleon.lib.WebBrowser.Models;
using Chameleon.lib.WebBrowser.Util;

using Newtonsoft.Json.Linq;

namespace Chameleon.lib.WebBrowser.System.Firefox;
public class FirefoxSysBrowserInstance : SysBrowserInstance {
	private async Task CreateChameleonFirefoxCopy()
	{
		if (IOtil.IsNeedUpdate(Options.ExePath, Consts.Browser.LocalFirefoxExePath)) {
			await IOtil.DeleteDExistsAsync(Consts.Browser.LocalFirefoxDirPath);

			await IOtil.CopyFolderAsync(OperatingSystem.IsMacOS()
				? "Applications/firefox.app"
				: Path.GetDirectoryName(Options.ExePath)!, Consts.Browser.LocalFirefoxDirPath);

			await Task.Delay(1000);
		}

		await SysBrowserInfoUtil.AddAutoloadTemporaryAddonFF(Consts.Browser.LocalFirefoxDirPath);
	}

	protected override async Task InitializeExtensionPath()
	{
		await CreateChameleonFirefoxCopy();
		_ = await InitializePrefsJs();

		var inDir = Path.Combine(Options.SysBrowserProfileCachePath, Consts.Browser.Foxameleon);
		await IOtil.DC(inDir);

		ExtentionsDirs.Add(ExtensionType.foxameleon, await BuildExtSettings());

		if (Options.Profile.Proxy.CanUse) {
			ExtentionsDirs.Add(ExtensionType.foxameleon_proxy, @$"
                let settings = {{
                    enabled: true,
                    type: 'http',
                    host: '{Options.Profile.Proxy.Host}',
                    port: {Options.Profile.Proxy.Port},
										server: '{Options.Profile.Proxy.Server}',
                    username: '{Options.Profile.Proxy.UserName}',
                    password: '{Options.Profile.Proxy.Password}',
                    url: '{Options.StartUrl}',
                    debug: false,
                }};
            ");
		}

		foreach (var (ext, setting) in ExtentionsDirs) {
			await _extensionLoaderService!.LoadExtension(ext, Options.DestExtentionsDir, setting).ConfigureAwait(true);
			var extDir = Path.Combine(Options.DestExtentionsDir, ext.ToString());
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
		var regex = Consts.Regexers.UserPrefRegex();

		var prefsFilePath = Path.Combine(Options.SysBrowserProfileCachePath, "prefs.js");
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
			$"-profile \"{Options.SysBrowserProfileCachePath}\""
		});
	}
}