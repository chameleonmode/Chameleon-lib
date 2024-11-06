using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

using Chameleon.lib.Common;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Models;
using Chameleon.lib.Common.ServiceManagers;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.WebBrowser.Services;

using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.WebBrowser.System.Chromium;
public class ChromiumSysBrowserInstance : SysBrowserInstance {
	protected override string GetCommandLineArguments()
	{
		//https://niek.github.io/chrome-features/
		//https://github.com/GoogleChrome/chrome-launcher/blob/main/src/flags.ts
		List<string> args =
		[
			//BackgroundFetch

			"--enable-features=NetworkServiceInProcess2,WebContentsDiscard,SkiaGraphite,CooperativeScheduling,DeferSpeculativeRFHCreation",
			//
			"--disable-features=InstalledApp,InstalledAppProvider,FedCm,DIPS,OptimizationHints,GlobalMediaControls,AvoidUnnecessaryBeforeUnloadCheckSync,MediaRouter,DialMediaRouteProvider,CalculateNativeWinOcclusion,InterestFeedContentSuggestions,CertificateTransparencyComponentUpdater,PrivacySandboxSettings4",
			// Disable all chrome extensions
			//'--disable-extensions',
			// Disable some extensions that aren't affected by --disable-extensions
			//'--disable-component-extensions-with-background-pages',
			// Disable various background network services, including extension updating,
			//   safe browsing service, upgrade detector, translate, UMA
			"--disable-background-networking",
			// Don't update the browser 'components' listed at chrome://components/
			"--disable-component-update",
			// Disables client-side phishing detection.
			"--disable-client-side-phishing-detection",
			// Disable syncing to a Google account
			//'--disable-sync',
			// Disable reporting to UMA, but allows for collection
			"--metrics-recording-only",
			// Disable installation of default apps on first run
			"--disable-default-apps",
			// Mute any audio
			//'--mute-audio',
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
			//
			"--bypass-app-banner-engagement-checks",
			//
			"--disable-field-trial-config",
			"--disable-session-crashed-bubble",
			"--disable-hyperlink-auditing",
			"--disable-domain-reliability",
			"--hide-crash-restore-bubble",
			"--restore-last-session",
			"--profile-directory=Default",
			"--ash-no-nudges",
			"--silent-debugger-extension-api",
			$"--remote-debugging-port={Settings.Port}",
      //$"--window-name=\"{UserProfile.Title}\"",
     ];

		if (Settings.Profile.Proxy.CanUse) {
			args.Add($"--proxy-server={Settings.Profile.Proxy.ServerForRequest}");
		} else {
			args.Add("--no-proxy-server");
		}

		args.Add($"--user-data-dir=\"{Settings.SysBrowserProfileCachePath}\"");

		List<string> exts = [];
		if (Directory.Exists(Settings.DestExtentionsDir)) {
			foreach (var item in Directory.GetDirectories(Settings.DestExtentionsDir)) {
				exts.Add(item);
			}
		}
		if (Directory.Exists(Settings.CachedExtentionsDir)) {
			foreach (var item in Directory.GetDirectories(Settings.CachedExtentionsDir)) {
				exts.Add(item);
			}
		}

		if (Directory.Exists(Settings.SysBrowseUserExtDir))
			exts.AddRange(Directory.GetDirectories(Settings.SysBrowseUserExtDir));

		if (exts.Count > 0)
			args.Add($"--load-extension=\"{exts.ToCommaSeparatedString()}\"");

		args.Add($"about:blank");

		return string.Join(" ", args);
	}

	// ...

	protected override async Task InitializeExtensionPath()
	{
    var extDir = await ExtensionLoaderService.LoadExtension(Enums.ExtensionType.chromeleon, Settings.CachedExtentionsDir);
    _ = await Settings.BuildMeleonExtSettings(GetTimezone, extDir);

    Settings.ExtentionsDirs.Add(Enums.ExtensionType.proxychromeleon, (
      Settings.BuildProxyExtSettings(),
      Guid.NewGuid().ToString(),
      Settings.DestExtentionsDir)
    );

    foreach (var (ext, (setting, guid, destDir)) in Settings.ExtentionsDirs) {
      _ = await ExtensionLoaderService.LoadExtension(ext, destDir, setting);
    }

		if (!File.Exists(Settings.PrefsFile)) {
			Toaster.ShowInf("Creating Prefs file for new browser instance");
			TaskCompletionSource tcs = new();
			new Thread(new ThreadStart(() => {
				try {
					using (var p = Process.Start(new ProcessStartInfo {
						FileName = Settings.ExePath,
						Arguments = GetCommandLineArguments(),
						UseShellExecute = false,
						CreateNoWindow = true
					})) {
						p.EnableRaisingEvents = true;
						p.Exited += (sender, e) => {
							_ = tcs.TrySetResult();
						};
						_ = p.Start();
						var tries = 0;
						do {
							Thread.Sleep(256);
							// Attempt to close the browser gracefully
							_ = p.CloseMainWindow();
							p.WaitForExit(TimeSpan.FromSeconds(1)); // Ensure the process has fully exited
						} while (!p.HasExited && tries++ < 36);
						if (!p.HasExited) {
							// Kill the process if it hasn't exited after 2 seconds
							p.Kill();
						}
					}
				} catch (Exception ex) {
					// Handle or log the exception as needed
					Console.WriteLine($"An error occurred: {ex.Message}");
					_ = tcs.TrySetException(ex);
				} finally {
					_ = tcs.TrySetResult();
				}
			})) {
				IsBackground = true,
			}.Start();

			await Task.Factory.StartNew(() => tcs.Task, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).Unwrap();
		}
		if (File.Exists(Settings.PrefsFile)) {

			var document = JsonDocument.Parse(await File.ReadAllTextAsync(Settings.PrefsFile));
			var root = document.RootElement.Clone();

			// Convert the root element to a JsonObject
			var mutableRoot = JsonNode.Parse(root.GetRawText())?.AsObject();
			if (mutableRoot != null) {
				if (mutableRoot["extensions"] is JsonObject extensions) {
					if (extensions["ui"] is JsonObject ui) {
						ui["developer_mode"] = true;
					} else {
						extensions["ui"] = new JsonObject {
							["developer_mode"] = true
						};
					}
				} else {
					mutableRoot["extensions"] = new JsonObject {
						["ui"] = new JsonObject {
							["developer_mode"] = true
						}
					};
				}
			}

			// Serialize the modified JsonObject back to JSON
			var modifiedJson = JsonSerializer.Serialize(mutableRoot);
			await File.WriteAllTextAsync(Settings.PrefsFile, modifiedJson);
		}
	}
}
