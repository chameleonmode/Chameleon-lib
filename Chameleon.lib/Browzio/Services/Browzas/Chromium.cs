using System.Diagnostics;
using Chameleon.lib.Util;

namespace Chameleon.lib.Browzio.Services.Browzas;

public abstract class Chromium : Browza {
	public override string PrefsFile => Path.Combine(
		Settings.CachePath,
		"Default",
		"Preferences"
	);

	public override string ExePath => Browzio.Utilities.GetBrowser(Settings.BrowserType)?.ExecutablePath ??
		throw new InvalidOperationException("Browser executable path not found.");

	// ...
	protected override string GetCommandLineArguments(string? url) {
		return string.Join(" ", new string?[] {
			"--enable-features=" + string.Join(",", [
				"UserAgentReduction",
				//"NetworkQualityEstimatorWebHoldback",
				//"StrictOriginIsolation",
				"ReduceUserAgentMinorVersion",
				"ReduceUserAgentPlatformOsCpu",
				"ReduceAcceptLanguage",
			]),
			"--disable-features=" + string.Join(",", [
				"PreciseMemoryInfo",
				"SharedArrayBuffer",
				"WebBluetooth",
				"WebUsb",
				"FractionalScrollOffsets",
				"Canvas2DLayers",
				// Disable the default browser check, do not prompt to set it as such
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
				// webrtc-hw-decoding Enables HW decode acceleration for WebRTC. ✅
				// webrtc-hw-encoding	Enables HW encode acceleration for WebRTC. ✅
				// "WebRtcHWDecoding",
				// "WebRtcHWEncoding",
				"DisableLoadExtensionCommandLineSwitch"
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
			// The id of the extension which you intend to debug. Attaching to an extension background page is only possible when the --silent-debugger-extension-api command-line switch is used.
			"--silent-debugger-extension-api",
			// Additional flags 
			// @TODO: test"--proxy-bypass-list=<loopback>",
			"--bypass-app-banner-engagement-checks",
			"--disable-field-trial-config",
			"--disable-session-crashed-bubble",
			"--disable-hyperlink-auditing",
			"--profile-directory=Default",
			"--hide-crash-restore-bubble",
			// "--restore-last-session",
			$"--remote-debugging-port={Settings.Port}",
			$"--user-data-dir=\"{Settings.CachePath}\"",
			// Settings.Profile.Proxy.Server != null ? $"--proxy-server={Settings.Profile.Proxy.Server}" : "",
			// $"--load-extension=\"{(Debugger.IsAttached ? "/Users/dev/src/Chameleon-lib/Chameleon.Assets/addons/chromeleon" : Project.Extensions.Chromeleon)}\"",
			Settings.Profile.Id > 0 ? $"--load-extension=\"{(Browzio.State.Staging ? Browzio.Extensions.Chromeleon : Settings.ExtensionsPath)}\"" : null,
			//Settings.Profile.Extensions ? $"--load-extension=\"{Settings.ExtensionsPath}\"" : null,
			// @TODO: Settings.OpenOptions.Headless ? "--headless=new" : "",
			url ??= Settings.Profile.Id > 0 ? InitUrl : Settings.Profile.StartPage
		}.Where(x => x != null));
	}

	// ...
	protected override async Task WaitForWinHandle() {
		await base.WaitForWinHandle();
		if (!OperatingSystem.IsWindows()) return;

		await EX.Poly(async () => {
			await Task.Delay(60);
			return (Brocess!.HasExited || Brocess.MainWindowHandle == IntPtr.Zero).ThrowIfTrue();
		}, new(sleep: 96, retries: 3));
	}

	protected virtual int? GetExistingProcessDebuggingPort() {
		return null;
	}

	public override async Task Ensure() {
		await base.Ensure();
		foreach (var process in Process.GetProcessesByName(ProcessName)) {
			if (
					process.ExtractArgs<int?>(
						@"--remote-debugging-port=(\d+)",
						(@"--user-data-dir=(""?([^""]+)""?)", Settings.CachePath)
					) is { }
			) await Processez.TryKillProcess(process);
		}

		if (Settings.Profile.Id < 0 || Directory.Exists(Settings.ExtensionsPath)) return;
		await IO.CopyDirectory(Browzio.Extensions.Chromeleon, Settings.ExtensionsPath);
		// Settings.Profile.StartPage = "about:blank";
	}

	public abstract string ProcessName { get; }
}

public class Brave : Chromium {
	public override string ProcessName => OperatingSystem.IsWindows() ? "brave" : "Brave Browser";
}
public class Chrome : Chromium {
	public override string ProcessName => OperatingSystem.IsWindows() ? "chrome" : "Google Chrome";
}
