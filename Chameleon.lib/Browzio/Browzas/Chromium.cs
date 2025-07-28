using System.Diagnostics;
using System.Net;
using System.Text;
using Chameleon.lib.Util;

namespace Chameleon.lib.Browzio.Services.Browzas;

public class Chromium : Browza {
	public override string PrefsFile => Path.Combine(Settings.CachePath, "Default", "Preferences");

	public override string ExePath => Browzio.Utilities.GetBrowser(Settings.BrowserType)?.ExecutablePath ??
		throw new InvalidOperationException("Browser executable path not found.");

	/**
	 * Returns the command line arguments for launching the browser.
	 *	 --user-data-dir=<dir>
	 *	Use a custom profile directory (isolates extensions, cookies, etc.).
	 *	
	 *	--load-extension=<path>
	 *	Load an unpacked extension from the specified folder.
	 *	
	 *	--pack-extension=<dir>
	 *	Package an extension folder into a .crx file.
	 *	
	 *	--pack-extension-key=<pem_file>
	 *	Specify an existing private key when packing an extension.
	 *	
	 *	--enable-experimental-extension-apis
	 *	Unlock extension APIs marked “experimental.”
	 *	
	 *	--allow-legacy-extension-manifests
	 *	Permit loading of older (legacy) manifest versions.
	 *	
	 *	--allowlisted-extension-id=<ID>
	 *	Treat a given extension ID as if it’s on Chrome’s internal allowlists.
	 *	
	 *	--enable-logging & --v=1
	 *	Turn on verbose logging (creates chrome_debug.log with errors, console output, etc.).
	 *	
	 *	--enable-extension-activity-logging
	 *	Record each extension API call/content-script injection in the Extensions Activity Log.
	 *	
	 *	--enable-extension-activity-log-testing
	 *	Enable activity-log features specifically for automated tests.
	 *	
	 *	--remote-debugging-port=<port>
	 *	Open a DevTools debugging port for inspection (can debug extensions).
	 *	
	 *	--remote-debugging-pipe
	 *	Use a pipe instead of TCP port for DevTools remote debugging.
	 *	
	 *	--enable-unsafe-extension-debugging
	 *	Allow the DevTools Protocol to install/remove extensions at runtime.
	 *	
	 *	--error-console
	 *	Force-enable the extension Error Console in chrome://extensions.
	 *	
	 *	--disable-extensions
	 *	Launch with all extensions disabled.
	 *	
	 *	--disable-extensions-except=<path>
	 *	Disable every extension except the one at the given path.
	 *	
	 *	--extensions-on-chrome-urls
	 *	Let extensions run on chrome:// pages (requires matching permissions).
	 *	
	 *	--disable-extensions-file-access-check
	 *	Bypass the “Allow access to file URLs” prompt for all extensions.
	 *	
	 *	--disable-extensions-http-throttling
	 *	Turn off Chrome’s throttling of background HTTP requests made by extensions.
	 *	
	 *	--disable-component-extensions-with-background-pages
	 *	Prevent built-in component extensions (PDF viewer, etc.) that have background pages from loading.
	 *	
	 *	--extensions-update-frequency=<seconds>
	 *	Set how often (in seconds) Chrome checks for extension updates.
	 *	
	 *	--enable-extension-actor-api
	 *	Unlock the experimental “Actor” extension API.
	 *	
	 *	--enable-extension-ai-data-collection
	 *	Enable the experimental AI Data Collection API for extensions.
	 *	
	 *	--enable-extension-assets-sharing
	 *	Allow sharing of assets among installed extensions.
	 *	
	 *	--extension-content-verification=<mode>
	 *	Control extension file-integrity checking (enforce, bootstrap, none, etc.).
	 *	
	 *	--disable-app-content-verification
	 *	Disable content verification for Chrome Apps (mostly deprecated).
	 */
	protected override string GetCommandLineArguments() {
		var proxy = Settings.Proxio is null
			? null // Use the loopback proxy for local requests
			: $"--proxy-server={Settings.Proxio.Value.host}:{Settings.Proxio.Value.port} --proxy-bypass-list=127.0.0.1:{Browzio.I.Loopback.Port},localhost:{Settings.Port}";
		return string.Join(" ", new string?[] {
			"--enable-features=" + string.Join(",", [
				"UserAgentReduction",
				"StrictOriginIsolation",
				"ReduceUserAgentMinorVersion",
				"ReduceUserAgentPlatformOsCpu",
				"ReduceAcceptLanguage",
			]),
			"--disable-features=" + string.Join(",", [
        // Disable built-in Google Translate service
        // "Translate",
				"msImplicitSignin",
				"AcceptCHFrame",
				"AutoExpandDetailsElement",
				"AvoidUnnecessaryBeforeUnloadCheckSync",
				"NetworkQualityEstimatorWebHoldback",
				"PreciseMemoryInfo",
				"SharedArrayBuffer",
				"WebBluetooth",
				"WebUsb",
				"FractionalScrollOffsets",
				"Canvas2DLayers",
				// Disable the default browser check, do not prompt to set it as such
				"InstalledApp",
				"InstalledAppProvider",
				"Translate", // Disable built-in Google Translate service
				"OptimizationHints", // Disable the Chrome Optimization Guide background networking
				"MediaRouter", // Disable the Chrome Media Router (cast target discovery) background networking
				"DialMediaRouteProvider", // Avoid the startup dialog for _Do you want the application "Chromium.app" to accept incoming network connections?_. This is a sub-component of the MediaRouter.
				"CalculateNativeWinOcclusion", // Disable the feature of: Calculate window occlusion on Windows will be used in the future to throttle and potentially unload foreground tabs in occluded windows.
				"InterestFeedContentSuggestions", // Disables the Discover feed on NTP and Android.
				"CertificateTransparencyComponentUpdater", // Don't update the CT lists
				"AutofillServerCommunication", // Disables autofill server communication. This feature isn't disabled via other 'parent' flags.
				"PrivacySandboxSettings4", // Disables "Enhanced ad privacy in Chrome" dialog (though as of 2024-03-20 it shouldn't show up if the profile has no stored country).
				"DeferRendererTasksAfterInput",
				"ExtensionManifestV2Disabled",
				"GlobalMediaControls",
				"HttpsUpgrades",
				"ImprovedCookieControls",
				"LazyFrameLoading",
				"LensOverlay",
				"PaintHolding",
				"ThirdPartyStoragePartitioning",
				// "WebRtcHWDecoding",
				// "WebRtcHWEncoding",
				"DisableLoadExtensionCommandLineSwitch"
			]),
			"--allowlisted-extension-id=cjemdhglmmgbdogklfgoofcoifgdmflf", // Chameleon extension ID
			"--disable-extensions-file-access-check",
			"--disable-extensions-http-throttling",
			"--extension-content-verification=none",
			"--disable-component-extensions-with-background-pages",
			"--enable-unsafe-extension-debugging",
			$"--remote-debugging-port={Settings.Port}",
			$"--user-data-dir=\"{Settings.CachePath}\"",
			Settings.WithExtensions ? $"{proxy}" : null,
			Settings.WithExtensions ? $"--load-extension=\"{Browzio.Extensions.Chromeleon}\"" : null, // dcelnbkcchhhmjalfimdgfkbapknjgfm
			// "--disable-extensions", // Disable all extensions except the one loaded above
  		// Disable some extensions that aren't affected by --disable-extensions
  		// "--disable-component-extensions-with-background-pages",
			// Disable various background network services, including extension updating,
			//   safe browsing service, upgrade detector, translate, UMA
			"--disable-background-networking",
			// Don't update the browser 'components' listed at chrome://components/
			"--disable-component-update",
			// Disables client-side phishing detection.
			"--disable-client-side-phishing-detection",
			// Disable syncing to a Google account
			"--disable-sync",
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
			"--bypass-app-banner-engagement-checks",
			"--disable-field-trial-config",
			"--disable-session-crashed-bubble",
			"--disable-hyperlink-auditing",
			"--profile-directory=Default",
			"--hide-crash-restore-bubble",
			// "--enable-automation",
			"--disable-back-forward-cache",
			"--disable-breakpad",
			"--disable-dev-shm-usage",
			"--allow-pre-commit-input",
			"--disable-popup-blocking",
			"--force-color-profile=srgb",
			"--no-service-autorun",
			"--export-tagged-pdf",
			"--disable-search-engine-choice-screen",
			"--unsafely-disable-devtools-self-xss-warnings",
			"--enable-use-zoom-for-dsf=false",
			// "--enable-extensions",
			// "--disable-web-security",
			// "--no-sandbox",
			// "--no-startup-window",
			// "--restore-last-session",
			// @TODO: Settings.OpenOptions.Headless ? "--headless=new" : "",
			InitUrl
			//"about:blank" // Use about:blank to avoid loading any page initially
		}.Where(x => x != null));
	}

	// ...
	protected override async Task WaitForWinHandle() {
		// using var Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		// var browser = await Playwright.Chromium.ConnectOverCDPAsync($"http://localhost:{Settings.Port}");
		// var context = browser.Contexts[0];
		// var page = context.Pages[0];
		// var cdpSession = await context.NewCDPSessionAsync(page);
		// await EX.Poly(async () => {
		// 	using var client = new TcpClient();
		// 	await client.ConnectAsync("127.0.0.1", Settings.Port);
		// 	// Send the request
		// 	var request = new {
		// 		id = 1337,
		// 		method = "Extensions.loadUnpacked",
		// 		@params = new { path = $"file:///{Browzio.Extensions.Chromeleon.Replace("\\", "/")}" }
		// 	};

		// 	// Wait for the response
		// 	return true;
		// },
		// new(sleep: 36, retries: 9));
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
		if(File.Exists(PrefsFile)) {
			// Read existing file
			var fileContent = File.ReadAllText(PrefsFile, Encoding.UTF8);
			var existingContent = JSON.Deserialize<Dictionary<string, object>>(fileContent);
		}
		foreach (var process in Process.GetProcessesByName(ProcessName)) {
			if (
					process.ExtractArgs<int?>(
						@"--remote-debugging-port=(\d+)",
						(@"--user-data-dir=(""?([^""]+)""?)", Settings.CachePath)
					) is not { }
			) continue;
			await Processez.TryKillProcess(process);
			await Task.Delay(900);
		}

		// if (!Settings.WithExtensions || Directory.Exists(Settings.ExtensionsPath)) return;
		// await IO.CopyDirectory(Browzio.Extensions.Chromeleon, Settings.ExtensionsPath);
		// Settings.Profile.StartPage = "about:blank";
	}

	public string ProcessName => Browzio.Utilities.GetBrowser(Settings.BrowserType)?.ExecutableName
		?? Path.GetFileName(ExePath).Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
}
