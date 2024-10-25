
using System;

using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.WebBrowser.Services;

namespace Chameleon.lib.WebBrowser.System.Chrome;
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

		if (Directory.Exists(Settings.SysBrowseUserExtDir))
			exts.AddRange(Directory.GetDirectories(Settings.SysBrowseUserExtDir));

		if (exts.Count > 0)
			args.Add($"--load-extension=\"{exts.ToCommaSeparatedString()}\"");

		args.Add($"about:blank");

		return string.Join(" ", args);
	}

	protected override async Task InitializeExtensionPath()
	{
		Settings.ExtentionsDirs.Add(Enums.ExtensionType.chromeleon, (await Settings.BuildExtSettings(GetTimezone), Guid.NewGuid().ToString()));

		var enabled = Settings.Profile.Proxy.CanUse ? "true" : "false";

		var starturl = 
			(Uri.TryCreate(Settings.StartUrl, UriKind.Absolute, out var uriResult) &&
			(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
			? Settings.StartUrl
			: "http://" + Settings.StartUrl;

		Settings.ExtentionsDirs.Add(Enums.ExtensionType.chromeleon_auto_proxy, (
			@$"let settings = {{
			   enabled: {enabled},
			   type: 'http',
			   host: '{Settings.Profile.Proxy.Host}',
			   port: {Settings.Profile.Proxy.Port},
			   username: '{Settings.Profile.Proxy.UserName}',
			   password: '{Settings.Profile.Proxy.Password}',
			   url: '{starturl}',
			   debug: false,
			}};", Guid.NewGuid().ToString()));

		foreach (var (ext, (setting, guid)) in Settings.ExtentionsDirs) {
			await ExtensionLoaderService.Instance.LoadExtension(ext, Settings.DestExtentionsDir, setting);
		}
	}
}
