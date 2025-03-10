using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Nodes;
using chameleon.assets;

using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util;

namespace Chameleon.lib.WebBrowser.System.Chromium;
public class ChromiumSysBrowserInstance : SysBrowserInstance {
	public override string PrefsFile => Path.Combine(
		Settings.SysBrowserProfileCachePath, 
		"Default",
		"Preferences"
		//OperatingSystem.IsWindows() ? "Preferences" : "Secure Preferences"
	);
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
		return string.Join(" ", new[] {
			//"--enable-features=" + string.Join(",", [
			//	"NetworkServiceInProcess2",
			//	"WebContentsDiscard",
			//	"DeferSpeculativeRFHCreation"
			//]),
			//FedCm,DIPS
			"--disable-features=" + string.Join(",", [
				"InstalledApp",
				"InstalledAppProvider",
        // Disable built-in Google Translate service
        "Translate",
        // Disable the Chrome Optimization Guide background networking
        "OptimizationHints",
        //  Disable the Chrome Media Router (cast target discovery) background networking
        "MediaRouter",
        /// Avoid the startup dialog for _Do you want the application “Chromium.app” to accept incoming network connections?_. This is a sub-component of the MediaRouter.
        "DialMediaRouteProvider",
        // Disable the feature of: Calculate window occlusion on Windows will be used in the future to throttle and potentially unload foreground tabs in occluded windows.
        "CalculateNativeWinOcclusion",
        // Disables the Discover feed on NTP
        "InterestFeedContentSuggestions",
        // Don't update the CT lists
        "CertificateTransparencyComponentUpdater",
        // Disables autofill server communication. This feature isn't disabled via other 'parent' flags.
        "AutofillServerCommunication",
        // Disables "Enhanced ad privacy in Chrome" dialog (though as of 2024-03-20 it shouldn't show up if the profile has no stored country).
        "PrivacySandboxSettings4",
			]),
			// Disable all chrome extensions
			//"--disable-extensions",
			// Disable some extensions that aren't affected by --disable-extensions
			//"--disable-component-extensions-with-background-pages",
			// Disable various background network services, including extension updating,
			//   safe browsing service, upgrade detector, translate, UMA
			"--disable-background-networking",
			// Don't update the browser 'components' listed at chrome://components/
			"--disable-component-update",
			// Disables client-side phishing detection.
			"--disable-client-side-phishing-detection",
			// Disable syncing to a Google account
			//"--disable-sync",
			// Disable reporting to UMA, but allows for collection
			"--metrics-recording-only",
			// Disable installation of default apps on first run
			"--disable-default-apps",
			// Mute any audio
			//"--mute-audio",
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
			// Avoids blue bubble "user education" nudges (eg., "… give your browser a new look", Memory Saver)
			"--ash-no-nudges",
			// Additional flags 
			"--bypass-app-banner-engagement-checks",
			"--disable-field-trial-config",
			"--disable-session-crashed-bubble",
			"--disable-hyperlink-auditing",
			"--silent-debugger-extension-api",
			"--profile-directory=Default",
			"--hide-crash-restore-bubble",
			"--restore-last-session",
			$"--remote-debugging-port={Settings.Port}",
			$"--user-data-dir=\"{Settings.SysBrowserProfileCachePath}\"",
			//Settings.Profile.Proxy.CanUse ? $"--proxy-server={Settings.Profile.Proxy.ServerForRequest}" : "",
			//Settings.Profile.Proxy.HasLogin ? $"--proxy-auth={Settings.Profile.Proxy.UserName}:{Settings.Profile.Proxy.Password}" : "",
			exts.Any() ? $"--load-extension=\"{exts.ToCommaSeparatedString()}\"" : "",
			"about:blank"
		}.Where(x => !string.IsNullOrWhiteSpace(x)));
	}

	// ...
	protected override async Task InitializeExtensionPath() {
		if (!File.Exists(PrefsFile)) {
			_ = Directory.CreateDirectory(Path.GetDirectoryName(PrefsFile)!);
			await File.AppendAllTextAsync(PrefsFile, "{\"extensions\": { \"ui\": { \"developer_mode\": true } }}");
		} else {
			// Make sure Chrome is closed before modifying the file
			// var jsonText = await File.ReadAllTextAsync(PrefsFile);
			// using var doc = JsonDocument.Parse(jsonText);

			// // Create a new mutable JSON structure
			// var rootObject = new JsonObject();
			// foreach (var property in doc.RootElement.EnumerateObject()) {
			// 	rootObject.Add(property.Name, JsonNode.Parse(property.Value.GetRawText()));
			// }

			// // Ensure the path exists and set the developer_mode property
			// if (!rootObject.ContainsKey("extensions"))
			// 	rootObject["extensions"] = new JsonObject();

			// if (rootObject["extensions"] is JsonObject extensions) {
			// 	if (!extensions.ContainsKey("ui"))
			// 		extensions["ui"] = new JsonObject();

			// 	if (extensions["ui"] is JsonObject ui) {
			// 		ui["developer_mode"] = true;
			// 	}
			// }
			// // Write back to the file with proper formatting
			// var options = new JsonSerializerOptions { WriteIndented = true };
			// await File.WriteAllTextAsync(PrefsFile, rootObject.ToJsonString(options));

			// Alternative way to modify the file
			//if (
			//	JsonNode.Parse(
			//		JsonDocument.Parse(await File.ReadAllTextAsync(PrefsFile)).RootElement.Clone().GetRawText()
			//	)?.AsObject() is JsonObject root
			//) {
			//	var extensions = root["extensions"] ??= new JsonObject();
			//	var ui = extensions["ui"] ??= new JsonObject();
			//	ui["developer_mode"] = true;
			//	await File.WriteAllTextAsync(PrefsFile, JsonSerializer.Serialize(root));
			//}

			//var root = JsonNode.Parse(
			//	JsonDocument.Parse(await File.ReadAllTextAsync(PrefsFile)).RootElement.Clone().GetRawText()
			//)?.AsObject();
			//
			//// Convert the root element to a JsonObject
			//if (root is JsonObject) {
			//	var extensions = root["extensions"] ??= new JsonObject();
			//	var ui = extensions["ui"] ??= new JsonObject();
			//	ui["developer_mode"] = true;
			//	await File.WriteAllTextAsync(PrefsFile, JsonSerializer.Serialize(root));
			//}
		}

		var extDir = await ExtensionLoader.LoadExtension(ExtensionType.chromeleon, Settings.CachedExtentionsDir);
		//await File.WriteAllTextAsync(Path.Combine(extDir, "settings.json"), settingsBuilder.ToString());
		_ = await Settings.BuildMeleonExtSettings(extDir);

		await File.WriteAllTextAsync(
			Path.Combine(await ExtensionLoader.LoadExtension(ExtensionType.chromoxyproxy, Settings.DestExtentionsDir), "settings.js"), 
			@$"export const settings = {{
			   	type: 'http',
				 	server: '{Settings.Profile.Proxy.Server}',
			   	host: '{Settings.Profile.Proxy.HostForRequest}',
			   	port: {Settings.Profile.Proxy.Port},
			   	username: '{Settings.Profile.Proxy.UserName}',
			   	password: '{Settings.Profile.Proxy.Password}',
			   	enabled: {(Settings.Profile.Proxy.CanUse ? "true" : "false")},
			  	url: '{Settings.StartUrl}'
			}};"
		);

		// foreach (var (ext, (setting, guid, destDir)) in Settings.ExtentionsDirs) {
		// 	_ = await ExtensionLoader.LoadExtension(ext, destDir, setting);
		// }
	}

	[SupportedOSPlatform("windows")]
	protected override async Task WaitForWinHandle() {
		_ = await TaskUtil.AwaitFor(() => Brocess?.MainWindowHandle != IntPtr.Zero, 18);
	}
}
