using System.Data.Common;
using System.Diagnostics;
using System.Runtime.Versioning;
using chameleon.assets;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.Const;
using Chameleon.lib.Helpers;
using Chameleon.lib.WebBrowser.Services;
using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.WebBrowser.System.Firefox;
public class FirefoxSysBrowserInstance : SysBrowserInstance {
	public override string PrefsFile => Path.Combine(Settings.SysBrowserProfileCachePath, "prefs.js");
	public override string ExePath { get; } = SysBrowserInfoUtil.FindByType(SystemBrowserType.Firefox).Path;

	public override async Task Start() {
		var system = OperatingSystem.IsMacOS() 
			? "/Applications/firefox.app" 
			: SysBrowserInfoUtil.FindByType(SystemBrowserType.Firefox).Path;

		var local = OperatingSystem.IsMacOS()
		 ? Consts.Browser.LocalFirefoxDirPath 
		 : ExePath;

		if (IOtil.NeedUpdate(system, local)) {
			Toaster.Info("Updating Firefox browser...");
			IOtil.DeleteDir(Path.Combine(FilePaths.AppDataLocalDir, "Foxameleon"));
			IOtil.DeleteDir(Path.Combine(FilePaths.AppDataLocalDir, "FirefoxChameleon"));
			IOtil.DeleteDir(Consts.Browser.LocalFirefoxDirPath);
			await IOtil.CopyFolderAsync(
				OperatingSystem.IsMacOS() ? system : Path.GetDirectoryName(system)!, 
				Consts.Browser.LocalFirefoxDirPath
			);
		}
		await base.Start();
	}

	protected override async Task InitializeExtensionPath() {
		//await SysBrowserInfoUtil.AddAutoloadTemporaryAddonFF();
		 var chrome = Path.Combine(Settings.SysBrowserProfileCachePath, "chrome");
		 await IOtil.DC(chrome);

		 var userChromecss = Path.Combine(chrome, "userChrome.css");
		 await File.WriteAllTextAsync(userChromecss, @$"@import url(./userChrome.js.css);");

		 var serChromejscss = Path.Combine(chrome, "userChrome.js.css");
		 await File.WriteAllTextAsync(serChromejscss, @$"
		 	@charset ""UTF-8"";
@namespace url(""http://www.mozilla.org/keymaster/gatekeeper/there.is.only.xul"");

#userChrome-js {{
  display: none !important;
}}
");

var userChromexml = Path.Combine(chrome, "userChrome.js");
		 await File.WriteAllTextAsync(userChromexml, 
"""
<?xml version="1.0"?>
<bindings xmlns="http://www.mozilla.org/xbl" xmlns:xul="http://www.mozilla.org/keymaster/gatekeeper/there.is.only.xul">
  <binding id="userChrome">
    <implementation>
      <constructor>
        <![CDATA[
          if(window.userChromeJsMod) return;
          window.userChromeJsMod = true;
          
          const MY_SCRIPT = "userChrome.js";
          
          try {
            const profD = Components.classes["@mozilla.org/file/directory_service;1"]
                        .getService(Components.interfaces.nsIProperties)
                        .get("ProfD", Components.interfaces.nsIFile);
            
            const chromeDir = profD.clone();
            chromeDir.append("chrome");
            
            if(!chromeDir.exists() || !chromeDir.isDirectory()) return;
            
            const scriptFile = chromeDir.clone();
            scriptFile.append(MY_SCRIPT);
            
            if(!scriptFile.exists() || scriptFile.isDirectory()) return;
            
            const loader = Components.classes["@mozilla.org/moz/jssubscript-loader;1"]
                        .getService(Components.interfaces.mozIJSSubScriptLoader);
                        
            loader.loadSubScript(`chrome://userchrome/content/${MY_SCRIPT}?${Math.random()}`, window);
          } catch(ex) {
            Components.utils.reportError(ex);
          }
        ]]>
      </constructor>
    </implementation>
  </binding>
</bindings>
""");
 var userChromejs = Path.Combine(chrome, "userChrome.js");
		 await File.WriteAllTextAsync(userChromejs,
"""
// ==UserScript==
// @name           Extension Installer
// @version        1.0
// @description    Install and manage extensions for Firefox 133+
// ==/UserScript==

(function() {
  const { classes: Cc, interfaces: Ci, utils: Cu } = Components;
  
  try {
    const Services = Cu.import("resource://gre/modules/Services.jsm").Services;
    
    // Import required modules
    let FileUtils;
    try {
      FileUtils = ChromeUtils.importESModule("resource://gre/modules/FileUtils.sys.mjs");
    } catch (e) {
      // Fallback for older versions
      FileUtils = ChromeUtils.import("resource://gre/modules/FileUtils.jsm", {}).FileUtils;
    }
    
    let AddonManager;
    try {
      AddonManager = ChromeUtils.importESModule("resource://gre/modules/AddonManager.sys.mjs").AddonManager;
    } catch (e) {
      AddonManager = ChromeUtils.import("resource://gre/modules/AddonManager.jsm", {}).AddonManager;
    }
    
    let ExtensionPermissions;
    try {
      ExtensionPermissions = ChromeUtils.importESModule("resource://gre/modules/ExtensionPermissions.sys.mjs").ExtensionPermissions;
    } catch (e) {
      ExtensionPermissions = ChromeUtils.import("resource://gre/modules/ExtensionPermissions.jsm", {}).ExtensionPermissions;
    }
    
    // Define private browsing permissions
    const PRIVATE_BROWSING_PERMS = {
      permissions: ["internal:privateBrowsingAllowed"],
      origins: [],
    };
    
    function log(text) {
      Services.console.logStringMessage("[Extension Installer] " + text);
    }
    
    // We need to modify the installation approach to work with newer Firefox
    async function installExtension(path, temporary = true) {
      try {
        // Use nsIFile for compatibility
        let file = Cc["@mozilla.org/file/local;1"].createInstance(Ci.nsIFile);
        file.initWithPath(path);
        
        if (!file.exists()) {
          log(`No such file or directory: ${path}`);
          return null;
        }

        log(`Installing addon from: ${path}`);
        
        try {
          let addon = await AddonManager.installTemporaryAddon(file);
          log(`Temporary add-on installed: ${addon.name} (ID: ${addon.id})`);
          return addon;
        } catch (tempEx) {
          log(`Temporary installation failed: ${tempEx.message}`);
          
          if (!temporary) {
            let install = await AddonManager.getInstallForFile(file);
            await install.install();
            log(`Regular installation successful`);
            return await AddonManager.getAddonByID(install.addon.id);
          }
        }
        
        return null;
      } catch (ex) {
        log(`Could not install add-on: ${path} - ${ex.message}`);
        return null;
      }
    }
    
    async function setPermission(addonId) {
      try {
        const addon = await AddonManager.getAddonByID(addonId);
        if (!addon) {
          log(`Addon not found: ${addonId}`);
          return false;
        }
        
        await ExtensionPermissions.add(addon.id, PRIVATE_BROWSING_PERMS);
        log(`Permission set for: ${addon.id}`);
        
        if (addon.isActive) {
          addon.reload();
          log(`Addon reloaded: ${addon.id}`);
        }
        
        return true;
      } catch (ex) {
        log(`Error setting permission for ${addonId}: ${ex.message}`);
        return false;
      }
    }
    
    async function installFromDirectory(dirPath) {
      try {
        let dir = Cc["@mozilla.org/file/local;1"].createInstance(Ci.nsIFile);
        dir.initWithPath(dirPath);
        
        if (!dir.exists() || !dir.isDirectory()) {
          log(`Directory not found or not a directory: ${dirPath}`);
          return 0;
        }
        
        let entries = dir.directoryEntries;
        let installedCount = 0;
        
        while (entries.hasMoreElements()) {
          let entry = entries.getNext().QueryInterface(Ci.nsIFile);
          if (entry.isFile() && (entry.leafName.endsWith('.xpi') || entry.leafName.endsWith('.zip'))) {
            log(`Attempting to install: ${entry.leafName}`);
            let addon = await installExtension(entry.path, true);
            if (addon) {
              installedCount++;
              await setPermission(addon.id);
            }
          }
        }
        
        return installedCount;
      } catch (ex) {
        log(`Error installing from directory ${dirPath}: ${ex.message}`);
        return 0;
      }
    }
    
    // Install extensions when browser is fully loaded
    if (!Services.appinfo.inSafeMode) {
      window.addEventListener("load", function() {
        setTimeout(async function() {
          try {
            log("Starting extension installation process");
            
            // Update these paths to match your environment
            const paths = [
              `/Users/dev/src/Chameleon-lib/Tests/bin/Debug/net8.0/..Resources/BrowserExtensions/firefox`,
              `/Users/dev/Library/Application Support/Chameleon/gecko`,
              `${Services.dirsvc.get("ProfD", Ci.nsIFile).path}/Geckoleon`
            ];
            
            let totalInstalled = 0;
            
            for (const path of paths) {
              log(`Looking for extensions in: ${path}`);
              const count = await installFromDirectory(path);
              log(`Installed ${count} extensions from: ${path}`);
              totalInstalled += count;
            }
            
            // Try to enable permissions for specific extensions
            const extensionsToEnable = [
              "geckoleon@chameleonmode.com",
              "foxyproxy@chameleonmode.com"
            ];
            
            for (const extId of extensionsToEnable) {
              await setPermission(extId);
            }
            
            log(`Extension installation complete. Total installed: ${totalInstalled}`);
          } catch (ex) {
            log(`Error in extension installation: ${ex.message}`);
          }
        }, 3000);
      }, { once: true });
    }
  } catch (ex) {
    Cu.reportError(`Error in userChrome.js: ${ex.message}`);
  }
})();
""");

		//await File.WriteAllTextAsync(Path.Combine(dir, "firefox.cfg");

		await InitializePrefsJs();

		//
		await IOtil.CreateZipAsync(
			await ExtensionLoader.LoadExtension(ExtensionType.geckoleon, Settings.CachedExtentionsDir),
			Path.Combine(FilePaths.AppDataLocalDir, "gecko")
		);

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
		await IOtil.DeleteDExistsAsync(Settings.DestExtentionsDir);

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
			["privacy.fingerprintingProtection.overrides"] = Settings.Profile.Emulations.AutoTimezone && Settings.Profile.Proxy.CanUse ? "+JSDateTimeUTC" : "",
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