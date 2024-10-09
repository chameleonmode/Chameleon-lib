using System.Diagnostics;
using System.Xml.Linq;

using Chameleon.lib.Common;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util;
using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.WebBrowser.Models;
using Chameleon.lib.WebBrowser.Services;
using Chameleon.lib.WebBrowser.Util;

using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.WebBrowser.System.Firefox;
public class FirefoxSysBrowserInstance : SysBrowserInstance {
	private async Task CreateChameleonFirefoxCopy()
	{
		if (IOtil.IsNeedUpdate(Settings.ExePath, Consts.Browser.LocalFirefoxExePath)) {
			await IOtil.DeleteDExistsAsync(Consts.Browser.LocalFirefoxDirPath);

			await IOtil.CopyFolderAsync(OperatingSystem.IsMacOS()
				? "Applications/firefox.app"
				: Path.GetDirectoryName(Settings.ExePath)!, Consts.Browser.LocalFirefoxDirPath);

			await Task.Delay(1000);
		}

		await SysBrowserInfoUtil.AddAutoloadTemporaryAddonFF(Settings.SysBrowserProfileCachePath);
	}

	protected override async Task InitializeExtensionPath()
	{
		await CreateChameleonFirefoxCopy();
		await InitializePrefsJs();
		await InitializeExtensions();
	}

	private async Task InitializeExtensions()
	{
		var inDir = Path.Combine(Settings.SysBrowserProfileCachePath, Consts.Browser.Foxameleon);
		var versionFile = Path.Combine(inDir, "version.txt");
		var version = "2024.1.7.2";
		if (File.Exists(versionFile)) {
			var fileVersion = await File.ReadAllTextAsync(versionFile);
			if (fileVersion.Is()) version = IOtil.IncrementVersion(fileVersion);
		}
		await IOtil.DC(inDir);
		await File.WriteAllTextAsync(versionFile, version);

		//
		Settings.ExtentionsDirs.Add(Enums.ExtensionType.foxameleon, (await Settings.BuildExtSettings(), Guid.NewGuid().ToString()));

		//
		Settings.ExtentionsDirs.Add(Enums.ExtensionType.foxameleon_proxy,
			(@$"let settings = {{
                enabled: {Settings.Profile.Proxy.CanUse.Tlwr()},
                type: 'http',
                host: '{Settings.Profile.Proxy.Host}',
                port: {Settings.Profile.Proxy.Port},
						    server: '{Settings.Profile.Proxy.Server}',
                username: '{Settings.Profile.Proxy.UserName}',
                password: '{Settings.Profile.Proxy.Password}',
                url: '{Settings.StartUrl}',
                debug: false,
             }};", Guid.NewGuid().ToString()));

		foreach (var (ext, (setting, guid)) in Settings.ExtentionsDirs) {
			await ExtensionLoaderService.Instance.LoadExtension(ext, Settings.DestExtentionsDir, setting, version).ConfigureAwait(true);
			var extDir = Path.Combine(Settings.DestExtentionsDir, ext.ToString());
			if (Directory.Exists(extDir)) {
				await IOtil.CreateZipAsync(Path.Combine(inDir, guid + ".xpi"), extDir);
			}
		}

		//		var policy =
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

	// TODO:
	private async Task InitializePrefsJs()
	{
		//"https://arkenfox.github.io/TZP/tzp.html"
		var prefs = new List<string>(SysBrowserInfoUtil.FirefoxUserPrefs.Where(p => !SysBrowserInfoUtil.FirefoxDepricatedPrefs.Contains(p.Key)).Select(p => p.Value).ToList());
		foreach (var p in new Dictionary<string, object>() {
			// =================================================================
			// THESE ARE THE PROPERTIES FROM https://mullvad.net/en/browser/hard-facts
			// =================================================================
			["privacy.fingerprintingProtection"] = true,
			["privacy.resistFingerprinting"] = true,
			["privacy.resistFingerprinting.autoDeclineNoUserInputCanvasPrompts"] = true,
			//["privacy.resistFingerprinting.block_mozAddonManager"] = true,
			["privacy.resistFingerprinting.exemptedDomains"] = "*.example.invalid",
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
			["hangmonitor.timeou"] = 0,
			// Allow the application to have focus even it runs in the background 
			["focusmanager.testmode"] = true,
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
			["app.update.checkInstallTim"] = false,
			// Disable automatically upgrading Firefox     
			["app.update.disabledForTesting"] = true,
			// make absolutely sure it is really off
			["app.update.auto"] = false,
			["app.update.mode"] = 0,
			["app.update.service.enabled"] = false,
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
			["browser.startup.homepage"] = Settings.StartUrl,
			["browser.contentblocking.category"] = "strict",
			["privacy.fingerprintingProtection.overrides"] = Settings.Emulation.AutoTimezone && Settings.Profile.Proxy.CanUse ? "+JSDateTimeUTC" : "",
			["network.http.referer.XOriginTrimmingPolicy"] = "0",
			["browser.startup.page"] = Debugger.IsAttached ? 3 : 1,
			//SysBrowserInfoUtil.user_pref("extensions.webextensions.uuids", ""),
			//SysBrowserInfoUtil.user_pref("browser.uiCustomization.state", ""),
		}) {
			var up = $"user_pref(\"{p.Key}\", {p.Value.ParseValue()});";
			if (prefs.Contains(up) || SysBrowserInfoUtil.FirefoxDepricatedPrefs.Contains(p.Key)) {
				Debug.WriteLine(up);
				continue;
			}
			prefs.Add(up);
		}
		//dupe
		//user_pref("network.connectivity-service.enabled", false);
		//user_pref("browser.startup.homepage_override.mstone", "ignore");
		//user_pref("browser.uitour.enabled", false);
		//user_pref("network.http.speculative-parallel-limit", 0);
		//dep/ree
		//user_pref("privacy.resistFingerprinting.testGranularityMask", 0);
		//user_pref("browser.newtab.preload", false);
		//user_pref("browser.tabs.warnOnClose", false);
		//user_pref("browser.region.network.url", "");
		//user_pref("network.captive-portal-service.enabled", false);
		//user_pref("dom.disable_beforeunload", false);
		var prefsFile = Path.Combine(Settings.SysBrowserProfileCachePath, "prefs.js");
		if (!File.Exists(prefsFile)) {
			await File.WriteAllLinesAsync(prefsFile, prefs);
			var bs = IoC.GetService<ISysBrowserService>();
			if (bs != null) {
				var bi = await bs.Open(new SysBrowserOpenOptions(SystemBrowserType.Firefox, new Common.Models.UserProfileModel() 
				{ Id = Settings.Profile.Id }));
				await Task.Delay(613);
				await ProUtil.TryKillProcess(bi?.Settings.Brocess);
			}
		} else {
			var userprefsFile = Path.Combine(Settings.SysBrowserProfileCachePath, "user.js");
			await File.WriteAllLinesAsync(userprefsFile, prefs);
		}
	}

	protected override string GetCommandLineArguments()
	{
		return Debugger.IsAttached
			? string.Join(" ", new List<string> {
			"-jsconsole",
			"-no-remote",
			"-wait-for-browser",
			$"-profile \"{Settings.SysBrowserProfileCachePath}\""
		})
			: string.Join(" ", new List<string> {
			"-no-remote",
			"-wait-for-browser",
			$"-profile \"{Settings.SysBrowserProfileCachePath}\""
		});
	}
}