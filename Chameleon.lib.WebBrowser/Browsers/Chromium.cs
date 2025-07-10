using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
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

	public override string ExePath => BrowserInfo.Find(Settings.BrowserType).Path;

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
			$"--remote-debugging-port={Settings.Profile.Port}",
			$"--user-data-dir=\"{Settings.BrowserCache}\"",
			// Settings.Profile.Proxy.Server != null ? $"--proxy-server={Settings.Profile.Proxy.Server}" : "",
			// $"--load-extension=\"{(Debugger.IsAttached ? "/Users/dev/src/Chameleon-lib/Chameleon.Assets/addons/chromeleon" : Project.Extensions.Chromeleon)}\"",
			Settings.Profile.Extensions ? $"--load-extension=\"{Project.Extensions.Chromeleon}\"" : null,
			// @TODO: Settings.OpenOptions.Headless ? "--headless=new" : "",
			url ??= Settings.Profile.Extensions ? InitUrl : Settings.Profile.StartUrl
		}.Where(x => x != null));
	}

	// ...
	protected override async Task WaitForWinHandle() {
		await base.WaitForWinHandle();
		if (!OperatingSystem.IsWindows()) return;

		var result = await EX.Poly(async () => {
			await Task.Delay(54);
			return Brocess!.HasExited || Brocess.MainWindowHandle == nint.Zero
				? throw new InvalidOperationException("Failed to retrieve main window handle.")
				: Brocess.MainWindowHandle != nint.Zero;
		}, new(sleep: 100, retries: 6));

		// Verify browser process is running AND has a valid main window handle
		var hasValidHandle = Brocess?.MainWindowHandle != IntPtr.Zero && Brocess?.HasExited == false;
		if (hasValidHandle) {
			_ = LoadedTCS.TrySetResult(true);
			InvokeEvent(Event.Opened);
		} else {
			if (Brocess?.HasExited == true) Close();
			else _ = LoadedTCS.TrySetResult(false);
			// Don't trigger Opened event since browser is not properly connected
		}
	}

	protected virtual int? GetExistingProcessDebuggingPort() {
		var processNames = GetProcessNames();
		var profilePath = Settings.BrowserCache;
		
		foreach (var processName in processNames) {
			var processes = Process.GetProcessesByName(processName);
			
			foreach (var process in processes) {
				try {
					if (process.HasExited) {
						process.Dispose();
						continue;
					}
					var commandLine = GetProcessCommandLine(process.Id);
					
					var hasProfilePath = !string.IsNullOrEmpty(commandLine) &&
										(commandLine.Contains($"\"{profilePath}\"") ||
										 commandLine.Contains($" {profilePath} ") ||
										 commandLine.Contains($" {profilePath}"));

					if (hasProfilePath) {
						var port = commandLine != null ? ExtractDebuggingPortFromCommandLine(commandLine) : null;
						
						if (port.HasValue) {
							// Verify process is still accessible
							try {
								using var testProcess = Process.GetProcessById(process.Id);
								if (testProcess.HasExited) {
									process.Dispose();
									continue;
								}
							} catch (ArgumentException) {
								process.Dispose();
								continue;
							}

							foreach (var p in processes) {
								if (p != process) p.Dispose();
							}
							process.Dispose();
							return port;
						}
					}

					process.Dispose();
				} catch (InvalidOperationException) {
					try {
						process.Dispose();
					} catch { }
					continue;
				} catch (Exception) {
					try {
						process.Dispose();
					} catch { }
					continue;
				}
			}
		}

		return null;
	}

	protected virtual string[] GetProcessNames() {
		return Settings.BrowserType switch {
			BrowserType.Chrome => ["chrome"],
			BrowserType.Brave => ["brave"],
			_ => ["chrome"]
		};
	}

	private static int? ExtractDebuggingPortFromCommandLine(string commandLine) {
		if (string.IsNullOrEmpty(commandLine)) {
			return null;
		}
		var match = Regex.Match(commandLine, @"--remote-debugging-port=(\d+)", RegexOptions.IgnoreCase);
		if (match.Success && int.TryParse(match.Groups[1].Value, out var port)) {
			return port;
		}

		return null;
	}

	private static string? GetProcessCommandLine(int processId) {
		try {
			using var testProcess = Process.GetProcessById(processId);
			if (testProcess.HasExited) {
				return null;
			}
			
			if (OperatingSystem.IsWindows()) {
#pragma warning disable CA1416 // Validate platform compatibility
				using var searcher = new global::System.Management.ManagementObjectSearcher(
					$"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
				using var objects = searcher.Get();
				foreach (var obj in objects) {
					var cmdLine = obj["CommandLine"]?.ToString();
					return cmdLine;
				}
#pragma warning restore CA1416 // Validate platform compatibility
			} else if (OperatingSystem.IsLinux()) {
				var cmdPath = $"/proc/{processId}/cmdline";
				if (File.Exists(cmdPath)) {
					var raw = File.ReadAllText(cmdPath);
					return raw.Replace('\0', ' ').Trim();
				}
			} else if (OperatingSystem.IsMacOS()) {
				var startInfo = new ProcessStartInfo {
					FileName = "/bin/ps",
					Arguments = $"-p {processId} -o command=",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};
				using var psProc = Process.Start(startInfo);
				if (psProc != null) {
					var output = psProc.StandardOutput.ReadToEnd();
					psProc.WaitForExit();
					return output.Trim();
				}
			}
		} catch (Exception) {
			// Do not throw exceptions here
		}

		return null;
	}

	public override async Task Ensure() {
		await base.Ensure();
		var existingPort = GetExistingProcessDebuggingPort();
		if (existingPort.HasValue) {
			var errorMessage = $"Browser instance is already running for profile {Settings.Profile.Id} on port {existingPort.Value}. " +
							   "Close the existing browser instance before launching a new one.";
			throw new InvalidOperationException(errorMessage);
		}
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