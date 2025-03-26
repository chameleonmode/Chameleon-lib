using System.Runtime.Versioning;
using chameleon.assets;

using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.ThirdParty.GeoIp;
using Chameleon.lib.Helpers;
using Chameleon.lib.WebBrowser.Services;

namespace Chameleon.lib.WebBrowser.System.Chromium;
public class ChromiumSysBrowserInstance : SysBrowserInstance {
	public override string PrefsFile => Path.Combine(
		Settings.SysBrowserProfileCachePath,
		"Default",
		"Preferences"
	//OperatingSystem.IsWindows() ? "Preferences" : "Secure Preferences"
	);

	public override string ExePath => SysBrowserInfoUtil.FindByType(Settings.BrowserType).Path;

	public string ExtUrl => $"chrome-extension://onmphcpdlamnigcccfcpikhihfaffapp/data/web/register.html?" +
		$"instanceId={Settings.Profile.Id}" +
		$"&sessionId=";

	// ...
	protected override string GetCommandLineArguments() {
		var exts = new[] {
			Settings.DestExtentionsDir,
			Settings.CachedExtentionsDir,
			Settings.SysBrowseUserExtDir,
		}.Where(Directory.Exists).SelectMany(Directory.GetDirectories).ToCommaSeparatedString();

		// Construct URL with parameters for extension

		return string.Join(" ", new[] {
			// "--enable-blink-features=" + string.Join(",", [
			// 	"WebRtcHideLocalIpsWithMdns",
			// 	"ReducedReferrerGranularity",
			// 	"PartitionVisitedLinkDatabase",
			// 	"QuoteEmptySecChUaStringHeadersConsistently",
			// 	"FencedFrames",
			// 	"ReduceUserAgentMinorVersion",
			// 	"ParkableImagesToDisk",
			// 	"SetIntervalWithoutClamp",
			// 	"WebCryptoCurve25519",
			// 	"BackForwardCacheNotRestoredReasons",
			// 	"LowerHighResolutionTimerThreshold",
			// ]),
			// "--disable-blink-features=" + string.Join(",", [
			// 	"WebGL1",
			// 	"WebGL2",
			// 	"Canvas2dImageChromium",
			// 	"WebGLImageChromium",
			// 	"CreateImageBitmapOrientationNone",
			// 	"ComputePressure",
			// 	"DeviceAttributes",
			// 	"ClientHintsDPR_DEPRECATED",
			// 	"ClientHintsDeviceMemory_DEPRECATED",
			// 	"ClientHintsViewportWidth_DEPRECATED",
			// 	"ClientHintsResourceWidth_DEPRECATED",
			// 	"PreciseMemoryInfo",
			// 	"CaptureJSExecutionLocation",
			// 	"IntensiveWakeUpThrottling",
			// ]),
			//"--blink-settings=" + string.Join(",", [
			// 	"webGL1Enabled=false",
			// 	"webGL2Enabled=false",
			// 	"navigatorPlatformOverride=\"Linux x86_64\"",
			// 	"deviceScaleAdjustment=1.0",
			// 	"forceDarkModeEnabled=true",
			// 	"inForcedColors=true",
			// 	"prefersReducedMotion=true",
			// 	"prefersReducedTransparency=true",
			// 	"antialiased2dCanvasEnabled=false",
			// 	"primaryPointerType=mojom::blink::PointerType::kPointerCoarse",
			// 	"primaryHoverType=mojom::blink::HoverType::kHoverHoverable",
			//	"bypassCSP=true",
			//]),

			 //"--enable-blink-features=" + string.Join(",", [
				// "ReducedReferrerGranularity",
				// "WebRtcHideLocalIpsWithMdns",
				// "PartitionVisitedLinkDatabase",
				// "QuoteEmptySecChUaStringHeadersConsistently",
				// "FencedFrames",
				// "ReduceUserAgentMinorVersion",
				// "TopicsAPI",
				// "BackForwardCacheNotRestoredReasons",
			//]),
			//"--blink-settings=" + string.Join(",", [
				// "webGLErrorsToConsoleEnabled=false",
				// "navigatorPlatformOverride=\"Linux x86_64\"",
				// "deviceScaleAdjustment=1.0",
				//"forceDarkModeEnabled=true",
				// "antialiased2dCanvasEnabled=false",
				// "primaryPointerType=mojom::blink::PointerType::kPointerCoarse",
				// "primaryHoverType=mojom::blink::HoverType::kHoverHoverable",
				// "bypassCSP=true",
			//]),
			// "--enable-blink-features=" + string.Join(",", [
			// 	"ReducedReferrerGranularity",
			// 	"WebRtcHideLocalIpsWithMdns",
			// 	"PartitionVisitedLinkDatabase",
			// 	"QuoteEmptySecChUaStringHeadersConsistently",
			// 	"UnifiedScrollableAreas",
			// 	"ForcedColors",
			// 	"CSSScopeImport",
			// 	"WebCrypto",
			// 	"WebPrefetchPrivacyChanges",
			// 	"WebSQLAccess=false",
			// 	"BackForwardCacheNotRestoredReasons",
			// 	"CSSHexAlphaColor",
			// ]),
			// "--disable-blink-features=" + string.Join(",", [
			// 	"WebGL1",
			// 	"WebGL2",
			// 	"Canvas2dImageChromium",
			// 	"NetInfoDownlinkMax",
			// 	"PreciseMemoryInfo",
			// 	"ClientHintsDPR_DEPRECATED",
			// 	"ClientHintsDeviceMemory_DEPRECATED",
			// 	"WebGPUDeveloperFeatures",
			// 	"CSSColorTypedOM",
			// 	"DeviceAttributes",
			// 	"MeasureMemory",
			// 	"HandwritingRecognition",
			// 	"ExtendedTextMetrics",
			// 	"GamepadMultitouch",
			// ]),
			// "--blink-settings=" + string.Join(",", [
			// 	"webGL1Enabled=false",
			// 	"webGL2Enabled=false",
			// 	"webGLErrorsToConsoleEnabled=false",
			// 	"cookieEnabled=false",
			// 	"hyperlinkAuditingEnabled=false",
			// 	"dnsPrefetchingEnabled=false",
			// 	"allowRunningOfInsecureContent=false",
			// 	"disableReadingFromCanvas=true",
			// 	"strictMixedContentChecking=true",
			// 	"strictPowerfulFeatureRestrictions=true",
			// 	"prefersReducedMotion=true",
			// 	"forceDarkModeEnabled=true",
			// 	"prefersReducedTransparency=true",
			// 	"textTrackBackgroundColor=#000000",
			// 	"bypassCSP=false",
			// 	"inForcedColors=true",
			// ]),
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
			"--bypass-app-banner-engagement-checks",
			"--disable-field-trial-config",
			"--disable-session-crashed-bubble",
			"--disable-hyperlink-auditing",
			"--profile-directory=Default",
			"--hide-crash-restore-bubble",
			//"--restore-last-session",
			$"--remote-debugging-port={Settings.Port}",
			$"--user-data-dir=\"{Settings.SysBrowserProfileCachePath}\"",
			Settings.Profile.Proxy.Server != null ? $"--proxy-server={Settings.Profile.Proxy.Server}" : "",
			//Settings.Profile.Proxy.HasLogin ? $"--proxy-auth={Settings.Profile.Proxy.UserName}:{Settings.Profile.Proxy.Password}" : "",
			#if DEBUG
				$"--load-extension=\"{exts}\",/Users/dev/src/Chameleon-lib/Chameleon.Assets/addons/chromeleon",
				//$"--load-extension=\"{exts}\"",
			#else
				$"--load-extension=\"{exts}\"",
			#endif
			ExtUrl + SessionId,
			//"about:blank"
		}.Where(x => !string.IsNullOrWhiteSpace(x)));
	}

	// ...
	protected override async Task InitializeExtensionPath() {
		Toaster.Info($"Requesting timezone/geo data for {Settings.Profile.Proxy.WebProxy?.Address?.Host ?? "local"}");
		var ipapi = await GeoIpApi.GetIpapi(Settings.Profile.Proxy.WebProxy, e => Toaster.Error(e)) ?? new() {
			timezone = "Pacific/Honolulu",
			lat = 34.052235,
			lon = -118.243683,
			tzSystem = true
		};
		Toaster.Info($"Timezone: {ipapi.timezone}, Lat: {ipapi.lat}, Lon: {ipapi.lon}");

		// set the extension settings
		AddonsServer.Instance.AddonInstances[SessionId] = new {
			urls = new {
				start = Settings.Profile.StartUrl,
			},
			tz = new {
				enabled = Settings.Profile.Emulations.AutoTimezone,
				zone = ipapi.timezone,
				useSystem = ipapi.tzSystem
			},
			geo = new {
				enabled = Settings.Profile.Emulations.SpoofGeoLocation,
				ipapi.lat,
				ipapi.lon,
			},
			canvas = new {
				enabled = Settings.Profile.Emulations.SpoofCanvasFingerprint,
			},
			webgl = new {
				enabled = Settings.Profile.Emulations.SpoofWebGLFingerprint,
			},
			rects = new {
				enabled = Settings.Profile.Emulations.SpoofClientRects,
			},
			fonts = new {
				enabled = Settings.Profile.Emulations.SpoofFontFingerprint,
			},
			audio = new {
				enabled = Settings.Profile.Emulations.SpoofAudio,
			},
			navi = new {
				enabled = Settings.Profile.Emulations.SpoofNavigator,
			},
		};
		// var chromeleon = Path.Combine(Settings.CachedExtentionsDir, ExtensionType.chromeleon.ToString());
		// if (!Directory.Exists(chromeleon)) {
		// 	_ = await ExtensionLoader.LoadExtension(ExtensionType.chromeleon, Settings.CachedExtentionsDir);
		// }

		await File.WriteAllTextAsync(
			Path.Combine(
				await ExtensionLoader.LoadExtension(ExtensionType.chroxyproxy, Settings.DestExtentionsDir),
				"settings.js"
			),
			@$"export const settings = {{
			   	type: 'http',
				 	server: '{Settings.Profile.Proxy.Server}',
			   	host: '{Settings.Profile.Proxy.HostForRequest}',
			   	port: {Settings.Profile.Proxy.Port},
			   	username: '{Settings.Profile.Proxy.UserName}',
			   	password: '{Settings.Profile.Proxy.Password}',
			   	enabled: {(Settings.Profile.Proxy.CanUse ? "true" : "false")}
			}};"
		);
	}

	[SupportedOSPlatform("windows")]
	protected override async Task WaitForWinHandle() {
		_ = await TaskUtil.AwaitFor(() => Brocess?.MainWindowHandle != IntPtr.Zero, 18);
	}
}
