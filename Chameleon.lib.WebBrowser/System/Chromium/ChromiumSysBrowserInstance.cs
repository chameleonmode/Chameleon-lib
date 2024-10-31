using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.WebBrowser.Services;

namespace Chameleon.lib.WebBrowser.System.Chromium;
public class ChromiumSysBrowserInstance : SysBrowserInstance {
	protected override string GetCommandLineArguments()
	{
		List<string> args =
		[
			"--disable-session-crashed-bubble",
			"--disable-hyperlink-auditing",
			"--hide-crash-restore-bubble",
			"--restore-last-session",
			"--profile-directory=Default",
			"--ash-no-nudges",
			"--disable-domain-reliability",
			"--no-default-browser-check",
			"--no-first-run",
			"--disable-field-trial-config",
			"--silent-debugger-extension-api",
			$"--remote-debugging-port={Settings.Port}",
      //$"--window-name=\"{UserProfile.Title}\"",
     ];

		if (Settings.Profile.Proxy.CanUse) {
			args.Add($"--proxy-server={Settings.Profile.Proxy.ServerForRequest}");
		} else {
			args.Add("--no-proxy-server");
		}

		args.Add($"--user-data-dir=\"{Settings.SysBrowserProfileCachePath}\"");

		List<string> exts = [];
		if (Directory.Exists(Settings.DestExtentionsDir)) {
			foreach (var item in Directory.GetDirectories(Settings.DestExtentionsDir)) {
				exts.Add(item);
			}
		}
		if (Directory.Exists(Settings.CachedExtentionsDir)) {
			foreach (var item in Directory.GetDirectories(Settings.CachedExtentionsDir)) {
				exts.Add(item);
			}
		}

		if (Directory.Exists(Settings.SysBrowseUserExtDir))
			exts.AddRange(Directory.GetDirectories(Settings.SysBrowseUserExtDir));

		if (exts.Count > 0)
			args.Add($"--load-extension=\"{exts.ToCommaSeparatedString()}\"");

		args.Add($"about:blank");

		return string.Join(" ", args);
	}

	protected override async Task InitializeExtensionPath()
	{
		Settings.ExtentionsDirs.Add(Enums.ExtensionType.chromeleon, (
			null,
			Guid.NewGuid().ToString(), 
			Settings.CachedExtentionsDir)
		);

		//Settings.ExtentionsDirs.Add(Enums.ExtensionType.extreloader, (
		//"",
		//Guid.NewGuid().ToString(),
		//Settings.DestExtentionsDir));

		Settings.ExtentionsDirs.Add(Enums.ExtensionType.proxychromeleon, (
			Settings.BuildProxyExtSettings() + await Settings.BuildMeleonExtSettings(GetTimezone),
			Guid.NewGuid().ToString(), 
			Settings.DestExtentionsDir)
		);

		foreach (var (ext, (setting, guid, destDir)) in Settings.ExtentionsDirs) {
			_ = await ExtensionLoaderService.LoadExtension(ext, destDir, setting);
		}
	}
}
