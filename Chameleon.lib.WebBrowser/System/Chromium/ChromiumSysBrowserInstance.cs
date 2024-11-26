using System.Text.Json;
using System.Text.Json.Nodes;

using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.WebBrowser.Services;

namespace Chameleon.lib.WebBrowser.System.Chromium;
public class ChromiumSysBrowserInstance : SysBrowserInstance {
	public override string PrefsFile => Path.Combine(Settings.SysBrowserProfileCachePath, "Default", "Preferences");
	public override string ExePath => SysBrowserInfoUtil.FindByType(Settings.BrowserType).Path;
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
		_ = PreLoadedTCS.TrySetResult(true);
		var extDir = await ExtensionLoaderService.LoadExtension(Enums.ExtensionType.chromeleon, Settings.CachedExtentionsDir);
    _ = await Settings.BuildMeleonExtSettings(extDir);

    Settings.ExtentionsDirs.Add(Enums.ExtensionType.proxychromeleon, (
      Settings.BuildProxyExtSettings(),
      Guid.NewGuid().ToString(),
      Settings.DestExtentionsDir)
    );

    foreach (var (ext, (setting, guid, destDir)) in Settings.ExtentionsDirs) {
      _ = await ExtensionLoaderService.LoadExtension(ext, destDir, setting);
    }

		if (!File.Exists(PrefsFile)) {
			await Task.Factory.StartNew(InitializePrefsFile, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
		}
		if (File.Exists(PrefsFile)) {
			var document = JsonDocument.Parse(await File.ReadAllTextAsync(PrefsFile));
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
			await File.WriteAllTextAsync(PrefsFile, modifiedJson);
		}
	}

	protected override async Task<bool> StartProcess(string args)
	{
		Brocess = ProUtil.Createa(ExePath, args);
		_ = Brocess.Start();
		await Task.Delay(1800);

		if (OperatingSystem.IsMacOS()) {
			Brocess.Exited += (s, e) => { Close(); };
			if(await TaskUtil.AwaitFor(() => Brocess?.HasExited == false && MacOSUtil.FindWindowByPID(Brocess.Id) != null, 36, 1000)) {
				MacOSWindowListener.Instance.AddPid(Brocess.Id);
			}
		} else {
			_ = await TaskUtil.AwaitFor(() => Brocess?.MainWindowHandle != IntPtr.Zero, 18);
		}

		return Brocess?.HasExited == false;
	}

	private async Task<string?> GetWebSocketDebuggerUrlAsync()
	{
		var url = $"http://localhost:{Settings.Port}/json";
		using var client = new HttpClient {
			Timeout = TimeSpan.FromSeconds(5) // Set a timeout of 5 seconds
		};

		try {
			var jsonResponse = await client.GetStringAsync(url);
			using var document = JsonDocument.Parse(jsonResponse);
			var root = document.RootElement;

			foreach (var target in root.EnumerateArray()) {
				if (target.TryGetProperty("type", out var typeProperty) && typeProperty.GetString() == "page") {
					if (target.TryGetProperty("webSocketDebuggerUrl", out var webSocketDebuggerUrlProperty)) {
						return webSocketDebuggerUrlProperty.GetString();
					}
				}
			}

			return null; // No suitable debugger URL found
		} catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
			// Handle timeout
			Console.WriteLine("The request timed out.");
			return null;
		} catch (HttpRequestException ex) {
			// Handle other HTTP request exceptions
			Console.WriteLine($"HttpRequestException: {ex.Message}");
			return null;
		} catch (Exception ex) {
			// Handle any other exceptions
			Console.WriteLine($"Exception: {ex.Message}");
			return null;
		}
	}
}
