using System.Diagnostics;
using System.Text;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.Common.Util;
using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.Common;
using Chameleon.lib.ThirdParty.GeoIp;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.WebBrowser.Models;
using Newtonsoft.Json.Linq;
using Chameleon.lib.Common.ServiceManagers;
using Chameleon.lib.Common.Constants;
using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.WebBrowser.System;
public abstract class SysBrowserInstance
		: ISysBrowserInstance {

	public event Delegatorz.Event<SysBrowserEvent>? OnEvent;

	public readonly IExtensionLoaderService? _extensionLoaderService = IoC.GetService<IExtensionLoaderService>();

	public required SysBrowserSettings Settings { get; init; }

	public TaskCompletionSource<bool> LoadedTCS { get; } = new();

	public Process? Brocess { get; set; }
	public Dictionary<Enums.ExtensionType, string?> ExtentionsDirs { get; } = [];

	public IntPtr Handle { get; private set; } = IntPtr.Zero;
	public bool IsRunning => Brocess?.HasExited == false;

	public async Task InitializeAsync(object? param = null)
	{
		if (Brocess is null || Handle == IntPtr.Zero) {
			await InitializeExtensionPath();
			if (LoadedTCS.Task.IsCompleted)
				return;
			await StartProcess();
		}
	}
	protected virtual async Task InitializeExtensionPath()
	{
		ExtentionsDirs.Add(Enums.ExtensionType.chromeleon, await BuildExtSettings());

		var enabled = Settings.Profile.Proxy.CanUse ? "true" : "false";
		ExtentionsDirs.Add(Enums.ExtensionType.chromeleon_auto_proxy, @$"
                let settings = {{
                    enabled: {enabled},
                    type: 'http',
                    host: '{Settings.Profile.Proxy.Host}',
                    port: {Settings.Profile.Proxy.Port},
                    username: '{Settings.Profile.Proxy.UserName}',
                    password: '{Settings.Profile.Proxy.Password}',
                    url: '{Settings.StartUrl}',
                    debug: false,
                }};
            ");

		foreach (var (ext, setting) in ExtentionsDirs) {
			await _extensionLoaderService!.LoadExtension(ext, Settings.DestExtentionsDir, setting);
		}
	}

	protected virtual string GetCommandLineArguments()
	{   // "--in-process-gpu","--disable-software-rasterizer",
		List<string> args =
		[
			"--disable-session-crashed-bubble",
			"--disable-hyperlink-auditing",
			"--hide-crash-restore-bubble",
			"--restore-last-session",
			"--profile-directory=Default",
			"--ash-no-nudges",
			"--disable-domain-reliability",
			"--no-default-browser-check",
			"--no-first-run",
			"--disable-field-trial-config",
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
		//foreach (var dir in ExtensionDirectories) {
		//	if (Directory.Exists(dir.Value.AddonDir))
		//		exts.Add(dir.Value.AddonDir);
		//}

		if (Directory.Exists(Settings.SysBrowseUserExtDir))
			exts.AddRange(Directory.GetDirectories(Settings.SysBrowseUserExtDir));

		if (exts.Count > 0)
			args.Add($"--load-extension=\"{exts.ToCommaSeparatedString()}\"");

		args.Add($"about:blank");

		return string.Join(" ", args);
	}

	public void InvokeEvent(SysBrowserEventType eventType)
	{
		if (eventType == SysBrowserEventType.Foreground)
			SetForeground();

		OnEvent?.Invoke(this, Settings.CreateEvent(eventType));
	}

	public async Task<string?> BuildExtSettings()
	{
		var timezone = "America/Los_Angeles";
		if (Settings.Emulation.AutoTimezone && Settings.Profile.Proxy.CanUse) {
			try {
				var ipapi = await GeoIpApi.GetIpapi(Settings.Profile.Proxy.ServerForRequest, e => Toaster.ShowErr(e),
						Settings.Profile.Proxy.UserName, Settings.Profile.Proxy.Password).ConfigureAwait(false);
				if (ipapi != null) {
					timezone = ipapi.timezone;
				}
			} catch (Exception ex) {
				_ = LoadedTCS.TrySetResult(false);
				throw new InvalidDataException($"Request for timezone failed, {Settings.Profile.Proxy.Server} - {ex.Message}");
			}
		}

		HashSet<KeyValuePair<string, string>> options =
		[
			new ("webglSpoofing", Settings.Emulation.SpoofWebGLFingerprint.Tlwr()),
			new ("canvasProtection", Settings.Emulation.SpoofCanvasFingerprint.Tlwr()),
			new ("clientRectsSpoofing",Settings.Emulation.SpoofClientRects.Tlwr()),
			new ("fontsSpoofing", Settings.Emulation.SpoofFontFingerprint.Tlwr()),
			new ("dAPI", Settings.Emulation.DisableWebRTC.Tlwr()),
			new ("webRtcEnabled", Settings.Emulation.DisableWebRTC.Tlwr()),
			new ("geoSpoofing", Settings.Emulation.SpoofGeoLocation.Tlwr()),
			new ("timezoneSpoofing", Settings.Emulation.AutoTimezone.Tlwr()),
			new ("myIP", (!Settings.Emulation.AutoTimezone).Tlwr()),
		];
		var settingsBuilder = new StringBuilder();
		_ = settingsBuilder.AppendLine("let BuildExtSettings = {");
		_ = settingsBuilder.AppendLine($"enabled: {options.Any(o => o.Value == "true").Tlwr()},");
		foreach (var o in options) {
			_ = settingsBuilder.AppendLine($"{o.Key}: {o.Value},");
		}
		_ = settingsBuilder.AppendLine($"timezone: '{timezone}',");
		_ = settingsBuilder.AppendLine(
"""
	randomizeTZ: false,
	randomizeGeo: false,
	noiseLevel: "medium",
	eMode: "disable_non_proxied_udp",
	dMode: "default_public_interface_only",
	locale: "en-US",
	debug: 4,
	latitude: 48.856892,
	longitude: 2.350850,
	accuracy: 69.96,
	bypass: [],
	history: [],
""");
		_ = settingsBuilder.AppendLine("};");

		return settingsBuilder.ToString();
	}

	public bool SetForeground()
	{
		if (Brocess != null) {
#pragma warning disable CA1416 // Validate platform compatibility
			if (!OperatingSystem.IsMacOS()) {
				if (Handle == IntPtr.Zero)
					return false;
				if (U32.IsWindow(Handle)) {
					if (U32til.BringWindowToForeground(Handle)) {
						return true;
					}
				}
#pragma warning restore CA1416 // Validate platform compatibility
			} else {
				if (MacOSUtil.SetForegroundWindow(Brocess.Id)) {
					Brocess.Refresh();
				} else {
					return true;
				}
			}
		}

		return false;
	}

	protected async Task StartProcess()
	{
			Brocess = ProUtil.Createa(Settings.BrowserType == Enums.SystemBrowserType.Firefox ? Consts.Browser.LocalFirefoxExePath : Settings.ExePath, GetCommandLineArguments());
			_ = Brocess.Start();

			if (OperatingSystem.IsMacOS()) {
				Handle = Brocess.Handle;
				Brocess.Exited += (s, e) => { Close(); };
				var tryCount = 0;
				while (Brocess?.HasExited == false &&
								MacOSUtil.FindWindowByPID(Brocess.Id) == null &&
								tryCount++ < 36) {
					await Task.Delay(1000);
				}

				if (Brocess?.Id is int id)
					MacOSWindowListener.Instance.AddPid(id);

			} else {
#pragma warning disable CA1416 // Validate platform compatibility
				await Task.Delay(1800);

				if (Settings.BrowserType != Enums.SystemBrowserType.Firefox) {
					string? windowHandle = null;
					while (IsRunning) {
						windowHandle = await GetWebSocketDebuggerUrlAsync();
						if (windowHandle.Is())
							break;

						await Task.Delay(250);
					}
					if (windowHandle?.Is() == false) {
					Close();
						return;
					}

					_ = await TaskUtil.AwaitFor(() => Brocess?.MainWindowHandle != IntPtr.Zero, 18);
					Handle = Brocess?.MainWindowHandle ?? IntPtr.Zero;
				} else {
					TaskCompletionSource<Process?> thisTcs = new();
					new Thread(() => {
						for (var i = 0; i < 18; i++) {
							ExUtil.TryCatch(() => {
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
							});
							if (Handle != IntPtr.Zero)
								break;
							Thread.Sleep(100);
						}
						if (Handle == IntPtr.Zero)
							_ = thisTcs.TrySetResult(null);
					}).Start();
					Brocess = await thisTcs.Task;
					Handle = Brocess?.MainWindowHandle ?? IntPtr.Zero;
				}
#pragma warning restore CA1416 // Validate platform compatibility
			}
		

		if (Brocess?.HasExited == false)
			_ = LoadedTCS.TrySetResult(true);
		else
			Close();
	}

	public void Close(bool raise = true)
	{
		if (OperatingSystem.IsMacOS()) {
			if (Brocess?.Id is int id)
				MacOSWindowListener.Instance.RemPid(id);
		}

		_ = LoadedTCS.TrySetResult(false);
		Brocess = null;
		Handle = IntPtr.Zero;
		if(raise)
			InvokeEvent(Enums.SysBrowserEventType.Closed);
	}

	private async Task<string?> GetWebSocketDebuggerUrlAsync()
	{
		var url = $"http://localhost:{Settings.Port}/json";
		using var client = new HttpClient {
			Timeout = TimeSpan.FromSeconds(5) // Set a timeout of 5 seconds
		};

		try {
			var jsonResponse = await client.GetStringAsync(url);
			var targets = JArray.Parse(jsonResponse);

			foreach (var target in targets.Cast<JObject>()) {
				if (target["type"]?.ToString() == "page") // Assuming you want to debug a page
				{
					return target["webSocketDebuggerUrl"]?.ToString();
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
