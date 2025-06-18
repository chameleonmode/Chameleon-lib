using System.Diagnostics;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Browsers;
using Chameleon.lib.WebBrowser.Services;

namespace Chameleon.lib.WebBrowser.System;

public class Chromium : Browser {
	public override string PrefsFile => Path.Combine(
		Settings.BrowserCache,
		"Default",
		"Preferences"
	);

	public override string ExePath => SysBrowserInfoUtil.Find(Settings.BrowserType).Path;
	// string ExtUrl => $"chrome-extension://bpckcldgiohofdmcepkndffkofgimbcm/data/web/register.html?" +
	// 	$"instanceId={Settings.Profile.Id}" +
	// 	$"&sessionId=";
	// override string ExtDir => FilePaths.EnsureDirectoryExists(FilePaths.AppDataLocalDir, "extensions", "chrome");

	// ...
	protected override string GetCommandLineArguments(bool args) {
		// var exts = string.Join(",", new[] {
		// 	Settings.DestExtentionsDir,
		// 	Path.Combine(FilePaths.BrowserExtensions, Settings.BrowserType.GetDescription()),
		// }.Where(Directory.Exists).SelectMany(Directory.GetDirectories));

		return string.Join(" ", new[] {
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
			// TODO: test"--proxy-bypass-list=<loopback>",
			"--bypass-app-banner-engagement-checks",
			"--disable-field-trial-config",
			"--disable-session-crashed-bubble",
			"--disable-hyperlink-auditing",
			"--profile-directory=Default",
			"--hide-crash-restore-bubble",
			// "--restore-last-session",
			$"--remote-debugging-port={Settings.Port}",
			$"--user-data-dir=\"{Settings.BrowserCache}\"",
			// Settings.Profile.Proxy.Server != null ? $"--proxy-server={Settings.Profile.Proxy.Server}" : "",
			// $"--load-extension=\"{(Debugger.IsAttached ? "/Users/dev/src/Chameleon-lib/Chameleon.Assets/addons/chromeleon" : Project.Extensions.Chromeleon)}\"",
			$"--load-extension=\"{Project.Extensions.Chromeleon}\"",
			args ? InitUrl : "about:blank",
			//"about:blank"
		}.Where(x => x != null));
	}

	// ...
	protected override async Task InitializeExtensionPath() {
		_ = await Project.Initialized.Task;
		// return;
		// await IOtil.DirectoryDelete(Path.Combine(FilePaths.AppDataLocalDir, "extensions", "chrome"));
		// _ = await Resources.LoadExtension(ExtensionType.chromeleon, Settings.DestExtentionsDir);
	}

	protected override async Task WaitForWinHandle() {
		if (OperatingSystem.IsWindows()) _ = await TaskUtil.AwaitFor(() => Brocess?.MainWindowHandle != nint.Zero, 18);
		else if (OperatingSystem.IsMacOS()) await base.WaitForWinHandle();
		// TODO:  return;
		// if (Settings.BrowserType == Enums.SystemBrowserType.Chrome)
		// 	      async Task<string?> GetWebSocketDebuggerUrl()
		//     {
		//         using var httpClient = new HttpClient();
		//         // Query Chrome's /json/version endpoint to get the active WebSocket URL
		//         var resp = await httpClient.GetStringAsync($"http://localhost:{Settings.Port}/json/version");
		//         using var doc = JsonDocument.Parse(resp);
		//         if (doc.RootElement.TryGetProperty("webSocketDebuggerUrl", out var wsUrl))
		//         {
		//             return wsUrl.GetString();
		//         }
		//         return null;
		//     }
		// {
		// 	try
		// 	{
		// 		// 1. Fetch the WebSocket Debugger URL
		// 		var webSocketUrl = await GetWebSocketDebuggerUrl();
		// 		if (string.IsNullOrEmpty(webSocketUrl))
		// 		{
		// 			await MessageBox.ShowErrorAsync(
		// 				"WebSocket URL Error",
		// 				"Failed to retrieve WebSocket Debugger URL. Ensure that the browser is running with remote debugging enabled."
		// 			);
		// 			return;
		// 		}

		// 		Debug.WriteLine($"Debugger WebSocket URL: {webSocketUrl}");

		// 		// 2. Connect to Chrome DevTools Protocol via WebSocket
		// 		using var ws = new ClientWebSocket();
		// 		await ws.ConnectAsync(new Uri(webSocketUrl), CancellationToken.None);
		// 		Debug.WriteLine("WebSocket connected.");

		// 		// 3. Build the CDP message for Extensions.loadUnpacked
		// 		//    Method: "Extensions.loadUnpacked"
		// 		//    Params: { path: "<absolute_path_to_unpacked_extension>" }
		// 		var cdpMessage = new
		// 		{
		// 			id = 1,
		// 			method = "Extensions.loadUnpacked",
		// 			@params = new
		// 			{
		// 				path = Project.Extensions.Chromeleon.Replace("\\", "\\\\")
		// 			}
		// 		};
		// 		var jsonPayload = JsonSerializer.Serialize(cdpMessage);

		// 		// 4. Send the JSON payload over WebSocket
		// 		var bytesToSend = Encoding.UTF8.GetBytes(jsonPayload);
		// 		await ws.SendAsync(new ArraySegment<byte>(bytesToSend), WebSocketMessageType.Text, true, CancellationToken.None);
		// 		Debug.WriteLine("Sent Extensions.loadUnpacked command.");

		// 		// 5. Await and print the response
		// 		var buffer = new byte[8192];
		// 		var sb = new StringBuilder();
		// 		WebSocketReceiveResult result;
		// 		do
		// 		{
		// 			result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
		// 			sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
		// 		} while (!result.EndOfMessage);

		// 		Debug.WriteLine("Response from CDP:");
		// 		Debug.WriteLine(sb.ToString());

		// 		// 6. Close the WebSocket
		// 		await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
		// 	}
		// 	catch (Exception ex)
		// 	{
		// 		Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
		// 	}
		// }
	}
}

// TODO: 
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