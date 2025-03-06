using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Nodes;
using chameleon.assets;

using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util;

namespace Chameleon.lib.WebBrowser.System.Chromium;
public class ChromiumSysBrowserInstance : SysBrowserInstance {
	public override string PrefsFile => Path.Combine(Settings.SysBrowserProfileCachePath, "Default", "Preferences");
	public override string ExePath => SysBrowserInfoUtil.FindByType(Settings.BrowserType).Path;

	// ...
	protected override string GetCommandLineArguments() {
		var exts = new[] {
			Settings.DestExtentionsDir,
			Settings.CachedExtentionsDir,
			Settings.SysBrowseUserExtDir
		}.Where(Directory.Exists).SelectMany(Directory.GetDirectories);
		
		//https://niek.github.io/chrome-features/
		//https://github.com/GoogleChrome/chrome-launcher/blob/main/src/flags.ts
		return string.Join(" ", [
			"--enable-features=NetworkServiceInProcess2,WebContentsDiscard,SkiaGraphite,CooperativeScheduling,DeferSpeculativeRFHCreation",
			"--disable-features=InstalledApp,InstalledAppProvider,FedCm,DIPS,OptimizationHints,GlobalMediaControls,AvoidUnnecessaryBeforeUnloadCheckSync,MediaRouter,DialMediaRouteProvider,CalculateNativeWinOcclusion,InterestFeedContentSuggestions,CertificateTransparencyComponentUpdater,PrivacySandboxSettings4",
			// Disable all chrome extensions
			//'--disable-extensions',
			// Disable some extensions that aren't affected by --disable-extensions
			//'--disable-component-extensions-with-background-pages',
			// Disable various background network services, including extension updating,
			//   safe browsing service, upgrade detector, translate, UMA
			"--disable-background-networking",
			// Don't update the browser 'components' listed at chrome://components/
			"--disable-component-update",
			// Disables client-side phishing detection.
			"--disable-client-side-phishing-detection",
			// Disable syncing to a Google account
			//'--disable-sync',
			// Disable reporting to UMA, but allows for collection
			"--metrics-recording-only",
			// Disable installation of default apps on first run
			"--disable-default-apps",
			// Mute any audio
			//'--mute-audio',
			// Disable the default browser check, do not prompt to set it as such
			"--no-default-browser-check",
			// Skip first run wizards
			"--no-first-run",
			// Disable backgrounding renders for occluded windows
			"--disable-backgrounding-occluded-windows",
			// Disable renderer process backgrounding
			"--disable-renderer-backgrounding",
			// Disable task throttling of timer tasks from background pages.
			"--disable-background-timer-throttling",
			// Disable the default throttling of IPC between renderer & browser processes.
			"--disable-ipc-flooding-protection",
			// Avoid potential instability of using Gnome Keyring or KDE wallet. crbug.com/571003 crbug.com/991424
			"--password-store=basic",
			// Use mock keychain on Mac to prevent blocking permissions dialogs
			"--use-mock-keychain",
			// Disable background tracing (aka slow reports & deep reports) to avoid 'Tracing already started'
			"--force-fieldtrials=*BackgroundTracing/default/",
			// Suppresses hang monitor dialogs in renderer processes. This flag may allow slow unload handlers on a page to prevent the tab from closing.
			"--disable-hang-monitor",
			// Reloading a page that came from a POST normally prompts the user.
			"--disable-prompt-on-repost",
			// Disables Domain Reliability Monitoring, which tracks whether the browser has difficulty contacting Google-owned sites and uploads reports to Google.
			"--disable-domain-reliability",
			// Disable the in-product Help (IPH) system.
			"--propagate-iph-for-testing",
			//
			"--bypass-app-banner-engagement-checks",
			"--disable-field-trial-config",
			"--disable-session-crashed-bubble",
			"--disable-hyperlink-auditing",
			"--disable-domain-reliability",
			"--hide-crash-restore-bubble",
			"--restore-last-session",
			"--profile-directory=Default",
			"--ash-no-nudges",
			"--silent-debugger-extension-api",
			$"--remote-debugging-port={Settings.Port}",
			$"--user-data-dir=\"{Settings.SysBrowserProfileCachePath}\"",
			Settings.Profile.Proxy.CanUse ? $"--proxy-server={Settings.Profile.Proxy.ServerForRequest}" : "--no-proxy-server",
			Settings.Profile.Proxy.HasLogin ? $"--proxy-auth={Settings.Profile.Proxy.UserName}:{Settings.Profile.Proxy.Password}" : "",
			exts.Any() ? $"--load-extension=\"{exts.ToCommaSeparatedString()}\"" : "",
			"about:blank"
		 ]);
	}

	// ...
	protected override async Task InitializeExtensionPath() {
		if (!File.Exists(PrefsFile)) {
			_ = Directory.CreateDirectory(Path.GetDirectoryName(PrefsFile)!);
			await File.AppendAllTextAsync(PrefsFile, "{\"extensions\": { \"ui\": { \"developer_mode\": true } }}");
		} else {
			var root = JsonNode.Parse(
				JsonDocument.Parse(await File.ReadAllTextAsync(PrefsFile)).RootElement.Clone().GetRawText()
			)?.AsObject();

			// Convert the root element to a JsonObject
			if (root is JsonObject) {
				var extensions = root["extensions"] ??= new JsonObject();
				var ui = extensions["ui"] ??= new JsonObject();
				ui["developer_mode"] = true;

				await File.WriteAllTextAsync(PrefsFile, JsonSerializer.Serialize(root));
			}
		}

		var extDir = await ExtensionLoader.LoadExtension(ExtensionType.chromeleon, Settings.CachedExtentionsDir);
		_ = await Settings.BuildMeleonExtSettings(extDir);

		Settings.ExtentionsDirs.Add(ExtensionType.proxychromeleon, (
			Settings.BuildProxyExtSettings(),
			Guid.NewGuid().ToString(),
			Settings.DestExtentionsDir)
		);

		foreach (var (ext, (setting, guid, destDir)) in Settings.ExtentionsDirs) {
			_ = await ExtensionLoader.LoadExtension(ext, destDir, setting);
		}
	}

	[SupportedOSPlatform("windows")]
	protected override async Task WaitForWinHandle() {
		_ = await TaskUtil.AwaitFor(() => Brocess?.MainWindowHandle != IntPtr.Zero, 18);
	}
}
