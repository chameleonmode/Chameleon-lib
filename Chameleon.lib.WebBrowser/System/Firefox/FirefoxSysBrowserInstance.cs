using System.Diagnostics;
using System.Text.RegularExpressions;
using chameleon.assets;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.Const;
using Chameleon.lib.Helpers;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Services;

namespace Chameleon.lib.WebBrowser.System.Firefox;
public class FirefoxSysBrowserInstance : SysBrowserInstance {

	// public override Process Start(ProcessStartInfo startInfo) {
	// 	startInfo.EnvironmentVariables["MOZ_REMOTE_SETTINGS_DEVTOOLS"] = "1";
	// 	return base.Start(startInfo);
	// }
	public override string PrefsFile => Path.Combine(Settings.SysBrowserProfileCachePath, "prefs.js");
	public override string ExeDir { get; } = OperatingSystem.IsMacOS()
		? Path.Combine(FilePaths.AppDataLocalDir, "gecko", "firefox.app")
		: Path.Combine(FilePaths.AppDataLocalDir, "gecko");
	public override string ExePath => OperatingSystem.IsMacOS()
		? Path.Combine(ExeDir, "Contents", "MacOS", "firefox")
		: Path.Combine(ExeDir, "firefox.exe");

	public string AddonPath => "/Users/dev/Downloads/938fc3dd55a44188ab6b-2025.3.26.xpi";//Path.Combine(FilePaths.AppDataLocalDir, "ext", "gecko");

	public override async Task Ensure() {
		// clean old copies
		IOtil.DeleteDir(Path.Combine(FilePaths.AppDataLocalDir, "Foxameleon"));
		IOtil.DeleteDir(Path.Combine(FilePaths.AppDataLocalDir, "FirefoxChameleon"));
		IOtil.DeleteDir(Path.Combine(FilePaths.AppDataLocalDir, "Geckoleon"));

		var system = OperatingSystem.IsMacOS()
			? "/Applications/firefox.app"
			: SysBrowserInfoUtil.Find(Enums.SystemBrowserType.Firefox).Path;

		var needsUpdate = !Path.Exists(ExePath) || (OperatingSystem.IsMacOS()
				? UMacFileVersionInfo.GetVersionInfo(ExeDir).ProductVersion != UMacFileVersionInfo.GetVersionInfo(system).ProductVersion
				: FileVersionInfo.GetVersionInfo(ExePath).ProductVersion != FileVersionInfo.GetVersionInfo(system).ProductVersion);

		if (needsUpdate) {
			Toaster.Info("Updating Firefox browser...");
			IOtil.DeleteDir(ExeDir);
			await IOtil.CopyDirectory(
				OperatingSystem.IsMacOS() ? system : Path.GetDirectoryName(system)!, ExeDir
			);
		}

		await base.Ensure();
	}


	// ""Install"": [
	//   ""file:///{AddonPath.Replace("\\", "/")}""
	// ]
	protected override async Task InitializeExtensionPath() {
		//await SysBrowserInfoUtil.AddAutoloadTemporaryAddonFF();
		var dir = OperatingSystem.IsMacOS()
			? Path.Combine(ExeDir, "Contents", "Resources", "distribution")
			: Path.Combine(ExeDir, "distribution");
		await IOtil.DC(dir);
		var json = JS.Serialize(new {
			policies = new {
				AppAutoUpdate = false,
				BackgroundAppUpdate = false,
				DisableAppUpdate = true,
				DisableProfileRefresh = true,
				DisableSystemAddonUpdate = true,
				DisableTelemetry = true,
				EnableTrackingProtection = new {
					Value = true,
					Locked = true,
					Cryptomining = true,
					Fingerprinting = true,
					EmailTracking = false
				},
				ExtensionUpdate = false,
				FirefoxSuggest = new {
					WebSuggestions = false,
					SponsoredSuggestions = false,
					ImproveSuggest = false,
					Locked = false
				},
				HardwareAcceleration = true,
				ManualAppUpdateOnly = true,
				NewTabPage = false,
				NoDefaultBookmarks = true,
				OverrideFirstRunPage = "",
				OverridePostUpdatePage = "",
				PopupBlocking = new {
					Default = true,
					Locked = false
				},
				UserMessaging = new {
					ExtensionRecommendations = false,
					FeatureRecommendations = false,
					UrlbarInterventions = false,
					SkipOnboarding = true,
					MoreFromMozilla = false,
					FirefoxLabs = false,
					Locked = false
				},
				Preferences = new Dictionary<string, object>
				{
						{ "accessibility.force_disabled", new { Value = 1, Status = "default", Type = "number" } },
						{ "browser.tabs.warnOnClose", new { Value = false, Status = "locked" } },
						{ "browser.shell.checkDefaultBrowser", new { Value = false, Status = "locked" } }
				},
				// ExtensionSettings = new Dictionary<string, object>
				// {
				// 		{
				// 				"greckoleon@chameleonmode.com",
				// 				new
				// 				{
				// 						installation_mode = "normal_installed",
				// 						default_area = "navbar",
				// 						private_browsing = true,
				// 						install_url = $"file:///{AddonPath.Replace("\\", "/")}" // Correct path handling
				//         }
				// 		}
				// }
			}
		}, new() {
			WriteIndented = true, // Pretty print JSON
			PropertyNamingPolicy = null // This preserves the original casing
		});
		await File.WriteAllTextAsync(Path.Combine(dir, "policies.json"), json);
// 		await File.WriteAllTextAsync(Path.Combine(dir, "policies.json"),
// """
// {
// 	"policies": {
// 		"3rdparty": {
// 		  "Extensions": {
// 		    "greckoleon@chameleonmode.com": {
// 					"x": {
// 	    			"sessionId": "null-nuller-nullish",
// 	    			"instanceId": 1
// 	  			}
// 		    }
// 		  }
// 		},
// 	  "AppAutoUpdate": false,
// 		"BackgroundAppUpdate": false,
// 		"DisableAppUpdate": true,
// 		"DisableProfileRefresh": true,
// 		"DisableSystemAddonUpdate": true,
// 		"DisableTelemetry": true,
// 		"EnableTrackingProtection": {
// 	    "Value": true,
// 	    "Locked": true,
// 	    "Cryptomining": true,
// 	    "Fingerprinting": true,
// 			"EmailTracking": false
// 	  },
// 		"ExtensionUpdate": false,
// 		"FirefoxSuggest": {
// 	    "WebSuggestions": false,
// 	    "SponsoredSuggestions": false,
// 	    "ImproveSuggest": false,
// 	    "Locked": false
// 	  },
// 		"HardwareAcceleration": true,
// 		"ManualAppUpdateOnly": true,
// 		"NewTabPage": false,
// 		"NoDefaultBookmarks": true,
// 		"OverrideFirstRunPage": "",
// 		"OverridePostUpdatePage": "",
// 		"PopupBlocking": {
// 	    "Default": true,
// 	    "Locked": false
// 	  },
// 		"UserMessaging": {
// 	    "ExtensionRecommendations": false,
// 	    "FeatureRecommendations": false,
// 	    "UrlbarInterventions": false,
// 	    "SkipOnboarding": true,
// 	    "MoreFromMozilla": false,
// 	    "FirefoxLabs": false,
// 	    "Locked": false
// 	  },
// 		"Preferences": {
// 	    "accessibility.force_disabled": {
// 	      "Value": 1,
// 	      "Status": "default",
// 	      "Type": "number"
// 	    },
// 	    "browser.tabs.warnOnClose": {
// 	      "Value": false,
// 	      "Status": "locked"
// 	    },
// 			"browser.shell.checkDefaultBrowser": {
// 				"Value": false,
// 				"Status": "locked"
// 			}
// 	  },
// """
// + @$"
// 		""ExtensionSettings"": {{
// 			""greckoleon@chameleonmode.com"": {{
// 				""installation_mode"": ""normal_installed"",
// 				""default_area"": ""navbar"",
// 				""private_browsing"": true,
// 				""install_url"": ""file:///{AddonPath.Replace("\\", "/")}""
// 			}}
// 	  }},
// 	}}
// }}
// ");
		if (Path.Exists(PrefsFile)) {
			// Build the list of deprecated/removed prefs to filter out
			var deprecatedPrefs = new HashSet<string>
			{
      // DEPRECATED
      "webchannel.allowObject.urlWhitelist",
			"browser.contentanalysis.default_allow",
			"browser.messaging-system.whatsNewPanel.enabled",
			"browser.ping-centre.telemetry",
			"dom.webnotifications.serviceworker.enabled",
			"javascript.use_us_english_locale",
			"layout.css.font-visibility.private",
			"layout.css.font-visibility.resistFingerprinting",
			"layout.css.font-visibility.standard",
			"layout.css.font-visibility.trackingprotection",
			"network.dns.skipTRR-when-parental-control-enabled",
			"permissions.delegation.enabled",
			"security.family_safety.mode",
			"widget.non-native-theme.enabled",
			"browser.cache.offline.enable",
			"extensions.formautofill.heuristics.enabled",
			"network.cookie.lifetimePolicy",
			"privacy.clearsitedata.cache.enabled",
			"privacy.resistFingerprinting.testGranularityMask",
			"security.pki.sha1_enforcement_level",
			"browser.urlbar.suggest.quicksuggest",
			"dom.securecontext.whitelist_onions",
			"dom.storage.next_gen",
			"network.http.spdy.enabled",
			"network.http.spdy.enabled.deps",
			"network.http.spdy.enabled.http2",
			"network.http.spdy.websockets",
			"layout.css.font-visibility.level",
			"security.ask_for_password",
			"security.csp.enable",
			"security.password_lifetime",
			"security.ssl3.rsa_des_ede3_sha",
      
      // REMOVED
      "dom.securecontext.allowlist_onions",
			"network.http.referer.hideOnionSource",
			"privacy.clearOnShutdown.cache",
			"privacy.clearOnShutdown.cookies",
			"privacy.clearOnShutdown.downloads",
			"privacy.clearOnShutdown.formdata",
			"privacy.clearOnShutdown.history",
			"privacy.clearOnShutdown.offlineApps",
			"privacy.clearOnShutdown.sessions",
			"privacy.cpd.cache",
			"privacy.cpd.cookies",
			"privacy.cpd.formdata",
			"privacy.cpd.history",
			"privacy.cpd.offlineApps",
			"privacy.cpd.sessions",
			"browser.fixup.alternate.enabled",
			"browser.taskbar.previews.enable",
			"browser.urlbar.dnsResolveSingleWordsAfterSearch",
			"geo.provider.network.url",
			"geo.provider.network.logging.enabled",
			"geo.provider.use_gpsd",
			"media.gmp-widevinecdm.enabled",
			"network.protocol-handler.external.ms-windows-store",
			"privacy.partition.always_partition_third_party_non_cookie_storage",
			"privacy.partition.always_partition_third_party_non_cookie_storage.exempt_sessionstorage",
			"privacy.partition.serviceWorkers",
			"beacon.enabled",
			"browser.startup.blankWindow",
			"browser.newtab.preload",
			"browser.newtabpage.activity-stream.feeds.discoverystreamfeed",
			"browser.newtabpage.activity-stream.feeds.snippets",
			"browser.region.network.url",
			"browser.region.update.enabled",
			"browser.search.region",
			"browser.ssl_override_behavior",
			"browser.tabs.warnOnClose",
			"devtools.chrome.enabled",
			"dom.disable_beforeunload",
			"dom.disable_open_during_load",
			"dom.netinfo.enabled",
			"dom.vr.enabled",
			"extensions.formautofill.addresses.supported",
			"extensions.formautofill.available",
			"extensions.formautofill.creditCards.available",
			"extensions.formautofill.creditCards.supported",
			"middlemouse.contentLoadURL",
			"network.http.altsvc.oe",
			"browser.urlbar.trimURLs",
			"dom.caches.enabled",
			"dom.storageManager.enabled",
			"dom.storage_access.enabled",
			"dom.targetBlankNoOpener.enabled",
			"network.cookie.thirdparty.sessionOnly",
			"network.cookie.thirdparty.nonsecureSessionOnly",
			"privacy.firstparty.isolate.block_post_message",
			"privacy.firstparty.isolate.restrict_opener_access",
			"privacy.firstparty.isolate.use_site",
			"privacy.window.name.update.enabled",
			"security.insecure_connection_text.enabled",
			"_user.js.parrot",

			// Additional items identified as deprecated from the latest content
      "general.appname.override",
			"general.appversion.override",
			"general.buildID.override",
			"general.oscpu.override",
			"general.platform.override",
			"general.useragent.override",
			"media.navigator.enabled",
			"browser.display.use_document_fonts",
			"browser.zoom.siteSpecific",
			"device.sensors.enabled",
			"dom.enable_performance",
			"dom.enable_resource_timing",
			"dom.gamepad.enabled",
			"dom.maxHardwareConcurrency",
			"dom.w3c_touch_events.enabled",
			"dom.webaudio.enabled",
			"font.system.whitelist",
			"media.ondevicechange.enabled",
			"media.video_stats.enabled",
			"media.webspeech.synth.enabled",
			"ui.use_standins_for_native_colors",
			"webgl.enable-debug-renderer-info",
      
      // Special items found in comparison (these were in v5 but removed/renamed in latest)
      "focusmanager.testmode",
			"gfx.color_management.mode",
			"gfx.color_management.rendering_intent",
			"geo.provider.testing",
			"webgl.forbid-software",
			"browser.tabs.disableBackgroundZombification",
			"ui.systemUsesDarkTheme"
		};

			try {
				// Read the user.js file
				var lines = await File.ReadAllLinesAsync(PrefsFile);

				// Pattern to extract preference name from user_pref
				var regex = new Regex(@"user_pref\([""'](.+?)[""'],");

				// Filter out the deprecated prefs
				var filteredLines = new List<string>();

				foreach (var line in lines) {
					var match = regex.Match(line);
					if (match.Success) {
						var prefName = match.Groups[1].Value;
						if (!deprecatedPrefs.Contains(prefName)) {
							filteredLines.Add(line);
						} else {
							Console.WriteLine($"Removed: {prefName}");
						}
					} else {
						// Keep non-pref lines (like comments)
						filteredLines.Add(line);
					}
				}

				// Write the cleaned file
				File.WriteAllLines(PrefsFile, filteredLines);
			} catch (Exception ex) {
				Console.WriteLine($"Error: {ex.Message}");
			}
		}
		var userJS = Path.Combine(Settings.SysBrowserProfileCachePath, "user.js");
		await EmbeddedLoader.LoadFile("js.firefox.user.js", userJS);

		//await InitializePrefsJs();

		//
		// await IOtil.CreateZipAsync(
		// 	await ExtensionLoader.LoadExtension(ExtensionType.geckoleon, Settings.CachedExtentionsDir),
		// 	Path.Combine(FilePaths.AppDataLocalDir, "gecko")
		// );

		//
		// await IOtil.CreateZipAsync(
		// 	await ExtensionLoader.LoadExtension(ExtensionType.foxyproxy, Settings.DestExtentionsDir, 
		// 		@$"const settings = {{
		// 			type: 'http',
		// 			server: '{Settings.Profile.Proxy.Server}',
		// 			host: '{Settings.Profile.Proxy.Host}',
		// 			port: {Settings.Profile.Proxy.Port},
		// 			username: '{Settings.Profile.Proxy.UserName}',
		// 			password: '{Settings.Profile.Proxy.Password}',
		// 			enabled: {(Settings.Profile.Proxy.CanUse ? "true" : "false")},
		// 			instanceId: '{Settings.Profile.Id}',
		// 			sessionId: '{SessionId}',
		// 		}};"), 
		// 	Path.Combine(Settings.SysBrowserProfileCachePath, Consts.Browser.Geckoleon)
		// );
		// await IOtil.DeleteDExistsAsync(Settings.DestExtentionsDir);
	}
	protected override string GetCommandLineArguments() {
		return string.Join(" ", [
			"-allow-downgrade",
			"-no-remote",
			#if DEBUG
			//"-devtools",
			"-jsconsole",
			#endif
			$"-profile \"{Settings.SysBrowserProfileCachePath}\"",
			$"https://chameleon.mode.com?instanceId={Settings.Profile.Id}&sessionId={SessionId}"
		]);
	}

	protected override async Task WaitForWinHandle() {
		if (OperatingSystem.IsWindows()) {
#pragma warning disable CA1416 // Validate platform compatibility
			TaskCompletionSource<Process?> thisTcs = new();
			new Thread(() => {
				for (var i = 0; i < 18; i++) {
					_ = ExUtil.TryCatch(() => {
						var currentProcesses = Process.GetProcessesByName("firefox");
						foreach (var p in currentProcesses) {
							if (Brocess != null && p.ParentProcessId() == Brocess.Id) {
								var childProcess = Process.GetProcessById(p.Id);
								if (childProcess?.HasExited == false) {

									var thishandle = U32til.FindMainWindowHandle(childProcess.Id);

									if (U32.IsWindow(thishandle)) {
										_ = thisTcs.TrySetResult(childProcess);
										break;
									}
								}
							}
						}
						return true;
					});
					if (Brocess?.MainWindowHandle != IntPtr.Zero)
						break;
					Thread.Sleep(100);
				}
				if (Brocess?.MainWindowHandle == IntPtr.Zero)
					_ = thisTcs.TrySetResult(null);
			}).Start();
			try {
				Brocess = await thisTcs.Task.WaitAsync(TimeSpan.FromSeconds(8));
			} catch {
				Close();
			}
#pragma warning restore CA1416 // Validate platform compatibility
		} else if (OperatingSystem.IsMacOS()) {
			await base.WaitForWinHandle();
		}
	}

	// TODO:
	private async Task InitializePrefsJs() {
		//"https://arkenfox.github.io/TZP/tzp.html"
		var prefs = new List<string>(SysBrowserInfoUtil.FirefoxUserPrefs
			.Where(p => !SysBrowserInfoUtil.FirefoxDepricatedPrefs.Contains(p.Key))
			.Select(p => p.Value)
			.ToList());
		foreach (var p in new Dictionary<string, object>() {
			// =================================================================
			// THESE ARE THE PROPERTIES FROM https://mullvad.net/en/browser/hard-facts
			// =================================================================
			["privacy.fingerprintingProtection"] = true,
			["privacy.resistFingerprinting"] = true,
			["privacy.resistFingerprinting.autoDeclineNoUserInputCanvasPrompts"] = true,
			["privacy.resistFingerprinting.exemptedDomains"] = "",
			["privacy.resistFingerprinting.jsmloglevel"] = "Warn",
			["privacy.resistFingerprinting.letterboxing"] = true,
			["privacy.resistFingerprinting.randomDataOnCanvasExtract"] = true,
			["privacy.resistFingerprinting.reduceTimerPrecision.jitter"] = true,
			["privacy.resistFingerprinting.reduceTimerPrecision.microseconds"] = 1000,
			["privacy.resistFingerprinting.target_video_res"] = 480,
			["privacy.resistFingerprinting.testGranularityMask"] = 0,
			["services.sync.prefs.sync.privacy.resistFingerprinting.reduceTimerPrecision.jitter"] = true,
			["services.sync.prefs.sync.privacy.resistFingerprinting.reduceTimerPrecision.microseconds"] = true,
			["network.negotiate-auth.allow-proxies"] = true,
			["prompts.authentication_dialog_abuse_limit"] = -1,
			["browser.newtab.preload"] = false,
			["extensions.pendingOperations"] = false,
			["media.hardware-video-decoding.failed"] = false,
			["sanity-test.running"] = false,
			// =================================================================
			// THESE ARE THE PROPERTIES THAT MUST BE ENABLED FOR JUGGLER TO WORK
			// =================================================================
			["dom.input_events.security.minNumTicks"] = 0,
			["dom.input_events.security.minTimeElapsedInMS"] = 0,
			["dom.iframe_lazy_loading.enabled"] = false,
			//["datareporting.policy.dataSubmissionEnabled"] = false,
			["datareporting.policy.dataSubmissionPolicyAccepted"] = false,
			["datareporting.policy.dataSubmissionPolicyBypassNotification"] = false,
			// Force pdfs into downloads.
			//pref("pdfjs.disabled", true);
			// This preference breaks our authentication flow.  
			["network.auth.use_redirect_for_retries"] = false,
			// Disable cross-process iframes, but not cross-process navigations.  
			["fission.webContentIsolationStrategy"] = 0,
			// Disable BFCache in parent process.
			// We also separately disable BFCache in content via docSchell property.  
			["fission.bfcacheInParent"] = false,
			// Disable first-party-based cookie partitioning.
			// When it is enabled, we have to retain "thirdPartyCookie^" permissions
			// in the storageState.      
			["network.cookie.cookieBehavior"] = 4,
			// Increase max number of child web processes so that new pages
			// get a new process by default and we have a process isolation
			// between pages from different contexts. If this becomes a performance
			// issue we can povide custom '@mozilla.org/ipc/processselector;1'    
			["dom.ipc.processCount"] = 60000,

			// Never reuse processes as they may keep previously overridden values
			// (locale, timezone etc.).       
			["dom.ipc.processPrelaunch.enabled"] = false,
			// Isolate permissions by user context.      
			["permissions.isolateBy.userContex"] = true,

			// Allow creating files in content process - required for
			// |Page.setFileInputFiles| protocol method. 
			["dom.file.createInChild"] = true,
			// Do not warn when closing all open tabs   
			["browser.tabs.warnOnClose"] = false,
			// Do not warn when closing all other open tabs   
			["browser.tabs.warnOnCloseOtherTabs"] = false,
			// Do not warn when multiple tabs will be opened    
			["browser.tabs.warnOnOpen"] = false,
			// Do not warn on quitting Firefox     
			["browser.warnOnQuit"] = false,
			// Disable popup-blocker
			//pref("dom.disable_open_during_load", false);
			// Disable the ProcessHangMonitor        
			["dom.ipc.reportProcessHangs"] = false,
			["hangmonitor.timeout"] = 0,
			// Allow the application to have focus even it runs in the background 
			//["focusmanager.testmode"] = true,
			// No ICC color correction. We need this for reproducible screenshots.
			// See https://developer.mozilla.org/en/docs/Mozilla/Firefox/Releases/3.5/ICC_color_correction_in_Firefox.
			//pref("gfx.color_management.mode", 0);
			//pref("gfx.color_management.rendering_intent", 3);
			// Always use network provider for geolocation tests so we bypass the
			// macOS dialog raised by the corelocation provider   
			//["geo.provider.testing"] = true,
			// =================================================================
			// THESE ARE NICHE PROPERTIES THAT ARE NICE TO HAVE
			// =================================================================
			// Enable software-backed webgl. See https://phabricator.services.mozilla.com/D164016
			//pref("webgl.forbid-software", false);
			// Disable auto-fill for credit cards and addresses.
			// See https://github.com/microsoft/playwright/issues/21393
			//pref("extensions.formautofill.creditCards.supported", "off");
			//pref("extensions.formautofill.addresses.supported", "off");
			// Allow access to system-added self-signed certificates. This aligns
			// firefox behavior with other browser defaults.
			["security.enterprise_roots.enabled"] = true,
			// Avoid stalling on shutdown, after "xpcom-will-shutdown" phase.
			// This at least happens when shutting down soon after launching.
			// See AppShutdown.cpp for more details on shutdown phases.
			["toolkit.shutdown.fastShutdownStage"] = 3,
			// Use light theme by default.
			//pref("ui.systemUsesDarkTheme", 0);
			// Do not use system colors - they are affected by themes.
			["ui.use_standins_for_native_colors"] = true,
			// Turn off the Push service.
			["dom.push.serverURL"] = "",
			// Prevent Remote Settings (firefox.settings.services.mozilla.com) to issue non local connections.
			["services.settings.server"] = "",
			// Prevent location.services.mozilla.com to issue non local connections.
			["browser.region.network.url"] = "",
			["browser.pocket.enabled"] = false,
			["browser.newtabpage.activity-stream.feeds.topsites"] = false,
			// required to prevent non-local access to push.services.mozilla.com
			["dom.push.connection.enabled"] = false,
			// Prevent contile.services.mozilla.com to issue non local connections.
			["browser.topsites.contile.enabled"] = false,
			["browser.safebrowsing.provider.mozilla.updateURL"] = "",
			["browser.library.activity-stream.enabled"] = false,
			["browser.search.geoSpecificDefaults"] = false,
			["browser.search.geoSpecificDefaults.url"] = "",
			//["captivedetect.canonicalURL"] = "",
			["network.captive-portal-service.enabled"] = false,
			//["network.connectivity-service.enabled"] = false,
			["browser.newtabpage.activity-stream.asrouter.providers.snippets"] = "",
			// Make sure Shield doesn't hit the network.
			//["app.normandy.api_url"] = "",
			//["app.normandy.enabled"] = false,     
			//["app.normandy.first_run"] = false,
			// Disable updater
			["app.update.enabled"] = false,
			// Disable Firefox old build background check   
			["app.update.checkInstallTime"] = false,
			// Disable automatically upgrading Firefox     
			["app.update.disabledForTesting"] = true,
			// make absolutely sure it is really off
			["app.update.auto"] = false,
			["app.update.silent"] = true,
			["app.update.mode"] = 0,
			// Do not redirect user when a milstone upgrade of Firefox is detected
			//["browser.startup.homepage_override.mstone"] = "ignore",
			// Disable topstories                       
			["browser.newtabpage.activity-stream.feeds.section.topstories"] = false,
			// DevTools JSONViewer sometimes fails to load dependencies with its require.js.
			// This spams console with a lot of unpleasant errors.
			// (bug 1424372)
			["devtools.jsonview.enabled"] = false,
			// Increase the APZ content response timeout in tests to 1 minute.
			// This is to accommodate the fact that test environments tends to be
			// slower than production environments (with the b2g emulator being
			// the slowest of them all), resulting in the production timeout value
			// sometimes being exceeded and causing false-positive test failures.
			//
			// (bug 1176798, bug 1177018, bug 1210465)
			["apz.content_response_timeout"] = 60000,
			// Indicate that the download panel has been shown once so that
			// whichever download test runs first doesn't show the popup
			// inconsistently.
			["browser.download.panel.shown"] = true,
			// Background thumbnails in particular cause grief, and disabling
			// thumbnails in general cannot hurt
			["browser.pagethumbnails.capturing_disabled"] = true,
			// Disable safebrowsing components.    
			["browser.safebrowsing.blockedURIs.enabled"] = false,
			["browser.safebrowsing.passwords.enabled"] = false,
			//["browser.safebrowsing.downloads.enabled"] = false,
			//["browser.safebrowsing.malware.enabled"] = false,
			//["browser.safebrowsing.phishing.enabled"] = false,
			// Disable updates to search engines.
			["browser.search.update"] = false,
			// Turn off search suggestions in the location bar so as not to trigger
			// network connections.
			["browser.urlbar.suggest.searches"] = true,
			// Do not restore the last open set of tabs if the browser has crashed
			["browser.sessionstore.resume_from_crash"] = false,
			// Don't check for the default web browser during startup.
			["browser.shell.checkDefaultBrowser"] = false,
			// Disable browser animations (tabs, fullscreen, sliding alerts)
			["toolkit.cosmeticAnimations.enabled"] = false,
			// Close the window when the last tab gets closed
			["browser.tabs.closeWindowWithLastTab"] = true,
			// Do not allow background tabs to be zombified on Android, otherwise for
			// tests that open additional tabs, the test harness tab itself might get
			// unloaded
			//pref("browser.tabs.disableBackgroundZombification", false);
			// Disable first run splash page on Windows 10
			["browser.usedOnWindows10.introURL"] = "",
			// Disable the UI tour.
			//
			// Should be set in profile.
			//["browser.uitour.enabled"] = false,
			["browser.uitour.url"] = "",
			// Do not show datareporting policy notifications which can
			// interfere with tests    
			["datareporting.healthreport.documentServerURI"] = "",
			["datareporting.healthreport.about.reportUrl"] = "",
			["datareporting.healthreport.logging.consoleEnabled"] = false,
			["datareporting.healthreport.service.enabled"] = false,
			["datareporting.healthreport.service.firstRun"] = false,
			//["datareporting.healthreport.uploadEnabled"] = false,
			// Automatically unload beforeunload alerts  
			["dom.disable_beforeunload"] = false,
			// Disable slow script dialogues    
			["dom.max_chrome_script_run_time"] = 0,
			["dom.max_script_run_time"] = 0,
			// Only load extensions from the application and user profile
			// AddonManager.SCOPE_PROFILE + AddonManager.SCOPE_APPLICATION
			//pref("extensions.autoDisableScopes", 0);
			//pref("extensions.enabledScopes", 15);
			// Disable metadata caching for installed add-ons by default
			//pref("extensions.getAddons.cache.enabled", false);
			// Disable installing any distribution extensions or add-ons.
			// pref("extensions.installDistroAddons", false);
			// Turn off extension updates so they do not bother tests
			//pref("extensions.update.enabled", false);
			// pref("extensions.update.notifyUser", false);
			// Make sure opening about:addons will not hit the network   
			["extensions.webservice.discoverURL"] = "",
			//pref("extensions.screenshots.disabled", true);
			//pref("extensions.screenshots.upload-disabled", true);
			// Disable useragent updates
			//pref("general.useragent.updates.enabled", false);   
			// Do not scan Wifi    
			["geo.wifi.scan"] = false,
			// Show chrome errors and warnings in the error console
			["javascript.options.showInConsole"] = true,
			// Disable download and usage of OpenH264: and Widevine plugins
			// pref("media.gmp-manager.updateEnabled", false);
			// Do not prompt with long usernames or passwords in URLs 
			["network.http.phishy-userpass-length"] = 255,
			// Do not prompt for temporary redirects         
			["network.http.prompt-temp-redirect"] = false,
			// Disable speculative connections so they are not reported as leaking
			// when they are hanging around
			//["network.http.speculative-parallel-limit"] = 0,
			// Do not automatically switch between offline and online  
			["network.manage-offline-status"] = false,
			// Make sure SNTP requests do not hit the network
			["network.sntp.pools"] = "",
			["security.certerrors.mitm.priming.enabled"] = false,
			// Local documents have access to all other local documents,
			// including directory listings
			["security.fileuri.strict_origin_policy"] = false,
			// Tests do not wait for the notification button security delay  
			["security.fileuri.notification_enable_delay"] = 0,
			// Do not automatically fill sign-in forms with known usernames and
			// passwords
			//pref("signon.autofillForms", false);
			// Disable password capture, so that tests that include forms are not
			// influenced by the presence of the persistent doorhanger notification
			//pref("signon.rememberSignons", false);
			// Disable first-run welcome page  
			["startup.homepage_welcome_url"] = "about:blank",
			["startup.homepage_welcome_url.additional"] = "",
			// Prevent starting into safe mode after application crashes  
			["toolkit.startup.max_resumed_crashes"] = -1,
			["toolkit.crashreporter.enabled"] = false,
			// Disable downloading the list of blocked extensions. 
			["extensions.blocklist.enabled"] = false,
			//
			["browser.startup.homepage"] = Settings.Profile.StartUrl,
			["browser.contentblocking.category"] = "strict",
			["app.update.service.enabled"] = false,
			["privacy.fingerprintingProtection.overrides"] = "+JSDateTimeUTC",
			["network.http.referer.XOriginTrimmingPolicy"] = "0",
			/* 0102: set startup page [SETUP-CHROME]
 			 * 0=blank, 1=home, 2=last visited page, 3=resume previous session
 			 * [NOTE] Session Restore is cleared with history (2811), and not used in Private Browsing mode
 			 * [SETTING] General>Startup>Restore previous session ***/
			["browser.startup.page"] = 3,
			["toolkit.legacyUserProfileCustomizations.stylesheets"] = true,
		}) {
			var up = $"user_pref(\"{p.Key}\", {p.Value.ParseValue()});";
			if (prefs.Contains(up) || SysBrowserInfoUtil.FirefoxDepricatedPrefs.Contains(p.Key)) {
				Debug.WriteLine(up);
				continue;
			}
			prefs.Add(up);
		}

		if (!File.Exists(PrefsFile)) {
			await File.WriteAllLinesAsync(PrefsFile, prefs);
			await InitializePrefsFile();
		}

		var lines = await File.ReadAllLinesAsync(PrefsFile);
		if (lines.Any(l => l.Is() && !l.StartsWith("user_pref(\"") && !l.StartsWith("//"))) {
			await File.WriteAllLinesAsync(PrefsFile, prefs);
		}

		var userJS = Path.Combine(Settings.SysBrowserProfileCachePath, "user.js");
		await EmbeddedLoader.LoadFile("js.firefox.user.js", userJS);
		//await File.WriteAllLinesAsync(userprefsFile, prefs);
	}

	public async Task InitializePrefsFile() {
		Toaster.Info("Creating Prefs file for new profile cache wait for the browser window to relaunch a second time");
		TaskCompletionSource tcs = new();
		new Thread(async () => {
			try {
				using var p = Start(new() {
					FileName = ExePath,
					Arguments = GetCommandLineArguments(),
					UseShellExecute = false,
					CreateNoWindow = true,
					EnvironmentVariables = {
						{ "MOZ_DISABLE_NETWORK", "1" } // Disable network in Firefox
					}
				});
				await Task.Delay(1800);
				p.Exited += (sender, e) => {
					_ = tcs.TrySetResult();
				};

				_ = await TaskUtil.AwaitFor(() => {
					Thread.Sleep(256);
					if (OperatingSystem.IsMacOS()) {
						if (MacOSUtil.FindWindowByPID(p.Id) == null)
							return false;

						// Use a shell command to send SIGTERM (graceful termination)
						using var killprocess = Process.Start("kill", $"-SIGTERM {p.Id}");
						_ = killprocess.WaitForExit(1); // Wait for the process to exit
					} else if (OperatingSystem.IsWindows()) {
						// Attempt to close the browser gracefully
						_ = p.CloseMainWindow();

						// Wait up to 1 second
						if (!p.WaitForExit(1000)) {
							// Use taskkill to gracefully terminate the process in Windows
							using var killprocess = Process.Start("taskkill", $"/PID {p.Id} /F");
							_ = killprocess.WaitForExit(1); // Wait for the process to exit
						}
					}
					return p.HasExited || File.Exists(PrefsFile);
				}, 18, 256);

				// Kill the process if it hasn't exited
				if (!p.HasExited) {
					// If it didn't exit gracefully, force kill it
					p.Kill();
					p.WaitForExit(); // Wait for the force kill to finish
				}
				p.Dispose();
			} catch (Exception ex) {
				_ = tcs.TrySetException(ex); // Handle or log the exception as needed
			} finally {
				_ = tcs.TrySetResult();
			}
		}) {
			IsBackground = true,
		}.Start();

		await tcs.Task;
	}
}