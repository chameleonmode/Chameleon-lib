using System.Diagnostics;
using System.Runtime.Versioning;
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
	public override string PrefsFile => Path.Combine(Settings.SysBrowserProfileCachePath, "prefs.js");
	public override string ExeDir { get; } = OperatingSystem.IsMacOS()
		? Path.Combine(FilePaths.AppDataLocalDir, "gecko", "firefox.app")
		: Path.Combine(FilePaths.AppDataLocalDir, "gecko");
	public override string ExePath => OperatingSystem.IsMacOS()
		? Path.Combine(ExeDir, "Contents", "MacOS", "firefox")
		: Path.Combine(ExeDir, "firefox.exe");

	public string AddonPath => "/Users/dev/Downloads/938fc3dd55a44188ab6b-2025.3.26.xpi";//Path.Combine(FilePaths.AppDataLocalDir, "ext", "gecko");

	public override async Task Start() {
		// clean old copies
		IOtil.DeleteDir(Path.Combine(FilePaths.AppDataLocalDir, "Foxameleon"));
		IOtil.DeleteDir(Path.Combine(FilePaths.AppDataLocalDir, "FirefoxChameleon"));
		IOtil.DeleteDir(Path.Combine(FilePaths.AppDataLocalDir, "Geckoleon"));

		bool NeedsUpdate(string path) {
			if (!Path.Exists(ExePath)) return true;

			if (OperatingSystem.IsMacOS()) {
				var local = UMacFileVersionInfo.GetVersionInfo(ExeDir);
				var system = UMacFileVersionInfo.GetVersionInfo(path);
				return local.ProductVersion != system.ProductVersion;
			} else {
				var local = FileVersionInfo.GetVersionInfo(ExePath);
				var system = FileVersionInfo.GetVersionInfo(path);
				return local.ProductVersion != system.ProductVersion;
			}
		}

		var system = OperatingSystem.IsMacOS()
			? "/Applications/firefox.app" 
			: SysBrowserInfoUtil.Find(Enums.SystemBrowserType.Firefox).Path;

		if (NeedsUpdate(system)) {
			Toaster.Info("Updating Firefox browser...");
			IOtil.DeleteDir(ExeDir);
			await IOtil.CopyDirectory(
				OperatingSystem.IsMacOS() ? system : Path.GetDirectoryName(system)!, ExeDir
			);
		}
		await base.Start();
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
		await File.WriteAllTextAsync(Path.Combine(dir, "policies.json"), 
"""
{
"policies": {
	"3rdparty": {
	  "Extensions": {
	    "greckoleon@chameleonmode.com": {
				"x": {
    			"sessionId": "null-nuller-nullish",
    			"instanceId": 1
  			}
	    }
	  }
	},
  "AppAutoUpdate": false,
	"BackgroundAppUpdate": false,
	"DisableAppUpdate": true,
	"DisableProfileRefresh": true,
	"DisableSystemAddonUpdate": true,
	"DisableTelemetry": true,
	"EnableTrackingProtection": {
    "Value": true,
    "Locked": true,
    "Cryptomining": true,
    "Fingerprinting": true,
		"EmailTracking": false
  },
	"ExtensionUpdate": false,
	"FirefoxSuggest": {
    "WebSuggestions": false,
    "SponsoredSuggestions": false,
    "ImproveSuggest": false,
    "Locked": false
  },
	"HardwareAcceleration": true,
	"ManualAppUpdateOnly": true,
	"NewTabPage": false,
	"NoDefaultBookmarks": true,
	"OverrideFirstRunPage": "",
	"OverridePostUpdatePage": "",
	"PopupBlocking": {
    "Default": true,
    "Locked": false
  },
	"UserMessaging": {
    "ExtensionRecommendations": false,
    "FeatureRecommendations": false,
    "UrlbarInterventions": false,
    "SkipOnboarding": true,
    "MoreFromMozilla": false,
    "FirefoxLabs": false,
    "Locked": false
  },
	"Preferences": {
    "accessibility.force_disabled": {
      "Value": 1,
      "Status": "default",
      "Type": "number"
    },
    "browser.tabs.warnOnClose": {
      "Value": false,
      "Status": "locked"
    },
		"browser.shell.checkDefaultBrowser": {
			"Value": false,
			"Status": "locked"
		}
  },
"""
+ @$"
	""ExtensionSettings"": {{
		""greckoleon@chameleonmode.com"": {{
			""installation_mode"": ""normal_installed"",
			""default_area"": ""navbar"",
			""private_browsing"": true,
			""install_url"": ""file:///{AddonPath.Replace("\\", "/")}""
		}}
  }},
	""Homepage"": {{
    ""URL"": ""{Settings.Profile.StartUrl}"",
    ""Locked"": false,
    ""StartPage"": ""homepage""
  }}
}}
}}
"
//user_pref("extensions.webextensions.uuids", "{greckoleon@chameleonmode.com\":\"3d228c2a-5b97-4630-9002-2100a597436e\",\"addons-search-detection@mozilla.com\":\"89dbe7f7-1c70-4326-978b-b63d1a6b13b3\"}");

		);

		await InitializePrefsJs();

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

		//
		// var groxyDir = await ExtensionLoader.LoadExtension(ExtensionType.foxyproxy, Settings.DestExtentionsDir);
		// await File.WriteAllTextAsync(Path.Combine(groxyDir,"settings.js"),
		// 	@$"export const settings = {{
		// 	   	type: 'http',
		// 		 	server: '{Settings.Profile.Proxy.Server}',
		// 	   	host: '{Settings.Profile.Proxy.HostForRequest}',
		// 	   	port: {Settings.Profile.Proxy.Port},
		// 	   	username: '{Settings.Profile.Proxy.UserName}',
		// 	   	password: '{Settings.Profile.Proxy.Password}',
		// 	   	enabled: {(Settings.Profile.Proxy.CanUse ? "true" : "false")}
		// 	}};"
		// );
		// await IOtil.CreateZipAsync(Path.Combine(inDir, Guid.NewGuid().ToString() + ".xpi"), groxyDir);
		// await IOtil.DeleteDExistsAsync(Settings.DestExtentionsDir);

		//var policy =
		//@$"
		//{{
		//   ""policies"": {{
		//		""ExtensionSettings"": {{
		//		  ""uBlock0@raymondhill.net"": {{
		//		    ""installation_mode"": ""normal_installed"",
		//		    ""install_url"": ""https://addons.mozilla.org/firefox/downloads/latest/ublock-origin/latest.xpi""  
		//		  }},
		//		  ""adguardadblocker@adguard.com"": {{
		//		    ""installation_mode"": ""normal_installed"",
		//		    ""install_url"": ""https://addons.mozilla.org/firefox/downloads/latest/adguardadblocker@adguard.com/latest.xpi"" 
		//		  }}
		//		}}
		//	}}
		//}}";
		//-Place the `policies.json` file in the appropriate directory based on the operating system:
		//   -**Windows:** `C:\Program Files\Mozilla Firefox\distribution\policies.json`
		//   -**macOS:** `/ Applications / Firefox.app / Contents / Resources / distribution / policies.json`
		//   -**Linux:** `/ usr / lib / firefox / distribution / policies.json` or similar.
		//var distributionDir = Path.Combine(Consts.Browser.LocalFirefoxDirPath, "distribution");
		//Directory.CreateDirectory(distributionDir);
		//File.WriteAllText(Path.Combine(distributionDir, "policies.json"), policy);
	}
	protected override string GetCommandLineArguments() {
		return string.Join(" ", [
			"-allow-downgrade",
			"-no-remote",
			#if DEBUG
			//"-devtools",
			"-jsconsole",
			#endif
			$"-profile \"{Settings.SysBrowserProfileCachePath}\""
		]);
	}

	[SupportedOSPlatform("windows")]
	protected override async Task WaitForWinHandle() {
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
			["dom.security.https_only_mode"] = false,
			["privacy.fingerprintingProtection"] = true,
			["privacy.resistFingerprinting"] = true,
			["privacy.resistFingerprinting.autoDeclineNoUserInputCanvasPrompts"] = true,
			//["privacy.resistFingerprinting.block_mozAddonManager"] = true,
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
			// Turn off the authentication dialog blocking 
			["network.negotiate-auth.allow-proxies"] = true,
			//["network.auth.subresource-http-auth-allow"] = 1,
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
			["app.update.service.enabled"] = false,
			["browser.startup.homepage"] = Settings.Profile.StartUrl,
			["browser.contentblocking.category"] = "strict",
			["privacy.fingerprintingProtection.overrides"] = Settings.Profile.Emulations.AutoTimezone ? "+JSDateTimeUTC" : "",
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
		var userprefsFile = Path.Combine(Settings.SysBrowserProfileCachePath, "user.js");
		await File.WriteAllLinesAsync(userprefsFile, prefs);
	}

	public async Task InitializePrefsFile() {
		Toaster.Info("Creating Prefs file for new profile cache wait for the browser window to relaunch a second time");
		TaskCompletionSource tcs = new();
		new Thread(async () => {
			try {
				using var p = ProUtil.Start(ExePath, GetCommandLineArguments());
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
					} else {
						// Attempt to close the browser gracefully
						_ = p.CloseMainWindow();
						_ = p.WaitForExit(TimeSpan.FromSeconds(1)); // Ensure the process has fully exited			
					}
					return p.HasExited || File.Exists(PrefsFile);
				}, 18, 36);

				// Kill the process if it hasn't exited
				if (!p.HasExited) {
					p.Kill();
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