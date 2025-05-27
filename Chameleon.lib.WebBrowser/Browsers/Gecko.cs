using System.Diagnostics;
using System.Text.RegularExpressions;
using chameleon.assets;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.Const;
using Chameleon.lib.Helpers;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Browsers;
using Chameleon.lib.WebBrowser.Services;

namespace Chameleon.lib.WebBrowser.System;
public class Gecko : Browser {
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

	protected override async Task InitializeExtensionPath() {
		await File.WriteAllTextAsync(
			Path.Combine(await IOtil.DC(OperatingSystem.IsMacOS()
			? Path.Combine(ExeDir, "Contents", "Resources", "distribution")
			: Path.Combine(ExeDir, "distribution")), "policies.json"),
			JS.Serialize(new {
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
					Preferences = new Dictionary<string, object> {
						{ "accessibility.force_disabled", new { Value = 1, Status = "default", Type = "number" } },
						{ "browser.tabs.warnOnClose", new { Value = false, Status = "locked" } },
						{ "browser.shell.checkDefaultBrowser", new { Value = false, Status = "locked" } }
					},
					ExtensionSettings = new Dictionary<string, object> {
						{
							"geckoleon@com.chameleon.mode",
							new {
								installation_mode = "normal_installed",
								default_area = "navbar",
								private_browsing = true,
								install_url = $"file:///{Project.Extensions.Geckoleon.Replace("\\", "/")}" // Correct path handling
					    }
						}
					}
				}
			}, new() {
				WriteIndented = true, // Pretty print JSON
				PropertyNamingPolicy = null // This preserves the original casing
			})
		);

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
							if (!deprecatedPrefs.Contains(prefName)) 								filteredLines.Add(line);
else {
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

		_ = await Resources.CopyFile("js.firefox", "user.js", Settings.SysBrowserProfileCachePath);
	}
	protected override string GetCommandLineArguments() {
		return string.Join(" ", [
			"-allow-downgrade",
			"-no-remote",
			#if DEBUG
			//"-devtools",
			//"-jsconsole",
			#endif
			$"-profile \"{Settings.SysBrowserProfileCachePath}\"",
			InitUrl
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
					if (Brocess?.MainWindowHandle != nint.Zero)
						break;
					Thread.Sleep(100);
				}
				if (Brocess?.MainWindowHandle == nint.Zero)
					_ = thisTcs.TrySetResult(null);
			}).Start();
			try {
				Brocess = await thisTcs.Task;
			} catch {
				Close();
			}
#pragma warning restore CA1416 // Validate platform compatibility
		} else if (OperatingSystem.IsMacOS()) {
			await base.WaitForWinHandle();
		}
	}
}