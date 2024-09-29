using System.Diagnostics;
using System.Text;
using Chameleon.lib.Common.Enums;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.Common.Util;
using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.Common;
using Chameleon.lib.ThirdParty.GeoIp;
using Chameleon.lib.Common.Managers;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.WebBrowser.Models;
using Newtonsoft.Json.Linq;
using Chameleon.lib.WebBrowser.Util;

namespace Chameleon.lib.WebBrowser.System;
public abstract class SysBrowserInstance
		: ISysBrowserInstance {
	public event EventHandler<SysBrowserLaunchOptions>? OnProcessClosed;
	public event EventHandler<SysBrowserLaunchOptions>? OnProcessOpenError;
	public event EventHandler<SysBrowserLaunchOptions>? OnBecameForeground;

	public readonly IExtensionLoaderService? _extensionLoaderService = IoC.GetService<IExtensionLoaderService>();

	public abstract SystemBrowserType BrowserType { get; set; }
	public required SysBrowserLaunchOptions Options { get; init; }

	public TaskCompletionSource<bool> LoadedTCS { get; } = new();

	public Process? Brocess { get; set; }
	public Dictionary<ExtensionType, string?> ExtentionsDirs { get; } = [];

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
		var settingsBuilder = new StringBuilder();

		_ = settingsBuilder.AppendLine("const initIt = () => {");
		if (Options.Emulation.AutoTimezone && Options.Profile.Proxy.CanUse) {
			try {
				var ipapi = await GeoIpApi.GetIpapi(Options.Profile.Proxy.ServerForRequest, e => Toaster.ShowErr(e),
						Options.Profile.Proxy.UserName, Options.Profile.Proxy.Password).ConfigureAwait(false);
				if (ipapi != null) {
					_ = settingsBuilder.AppendLine(
$@"
	chrome.storage.sync.set({{
	  timezone: '{ipapi.timezone}',
	  random: false,
	  update: false
	}}, () => {{
		OnLoad();
	}});
");
				}
			} catch (Exception ex) {
				Toaster.ShowErr($"Request for timezone failed, {Options.Profile.Proxy.Server} - {ex.Message}");
				OnProcessOpenError?.Invoke(this, Options);
				Dispose();
				return;
			}
		} else {
			_ = settingsBuilder.AppendLine("OnLoad();");
		}
		_ = settingsBuilder.AppendLine("};");
		_ = settingsBuilder.AppendLine("chrome.runtime.onInstalled.addListener(initIt);");
		_ = settingsBuilder.AppendLine("chrome.runtime.onStartup.addListener(initIt);");

		BuildExtSettings(settingsBuilder);
		ExtentionsDirs.Add(ExtensionType.chromeleon_addon, settingsBuilder.ToString());

		var enabled = Options.Profile.Proxy.CanUse ? "true" : "false";
		ExtentionsDirs.Add(ExtensionType.chromeleon_auto_ff_proxy, @$"
                let settings = {{
                    enabled: {enabled},
                    type: 'http',
                    host: '{Options.Profile.Proxy.Host}',
                    port: {Options.Profile.Proxy.Port},
                    username: '{Options.Profile.Proxy.UserName}',
                    password: '{Options.Profile.Proxy.Password}',
                    url: '{Options.StartUrl}',
                    debug: false,
                }};
            ");

		foreach (var (ext, setting) in ExtentionsDirs) {
			await _extensionLoaderService!.LoadExtension(ext, Options.DestExtentionsDir, setting);
		}
	}

	public void BuildExtSettings(StringBuilder settingsBuilder)
	{
		HashSet<KeyValuePair<string, string>> options =
		[
			new ("webglSpoofing", Options.Emulation.SpoofWebGLFingerprint.Tlwr()),
			new ("canvasProtection", Options.Emulation.SpoofCanvasFingerprint.Tlwr()),
			new ("clientRectsSpoofing",Options.Emulation.SpoofClientRects.Tlwr()),
			new ("fontsSpoofing", Options.Emulation.SpoofFontFingerprint.Tlwr()),
			new ("dAPI", Options.Emulation.DisableWebRTC.Tlwr()),
			new ("geoSpoofing", Options.Emulation.SpoofGeoLocation.Tlwr()),
			new ("timezoneSpoofing", Options.Emulation.AutoTimezone.Tlwr())
		];
		_ = settingsBuilder.AppendLine("let settings = {");
		_ = settingsBuilder.AppendLine($"enabled: {options.Any(o => o.Value == "true").Tlwr()},");
		foreach (var o in options) {
			_ = settingsBuilder.AppendLine($"{o.Key}: {o.Value},");
		}
		_ = settingsBuilder.AppendLine("eMode: ");
		_ = BrowserType == SystemBrowserType.Firefox ? settingsBuilder.Append("'proxy_only',") : settingsBuilder.Append("'disable_non_proxied_udp',");
		_ = settingsBuilder.AppendLine("dMode: 'default_public_interface_only',");
		_ = settingsBuilder.AppendLine("noiseLevel: 'medium',");
		_ = settingsBuilder.AppendLine("debug: 3");
		_ = settingsBuilder.AppendLine("};");
	}

	protected virtual string GetCommandLineArguments()
	{   // "--in-process-gpu","--disable-software-rasterizer",
		List<string> args =
		[
			"--disable-session-crashed-bubble",
			"--hide-crash-restore-bubble",
			"--restore-last-session",
			"--profile-directory=Default",
			"--ash-no-nudges",
			"--disable-domain-reliability",
			"--no-default-browser-check",
			"--no-first-run",
			"--disable-field-trial-config",
			"--silent-debugger-extension-api",
			$"--remote-debugging-port={Options.Port}",
      //$"--window-name=\"{UserProfile.Title}\"",
     ];

		if (Options.Profile.Proxy.CanUse) {
			args.Add($"--proxy-server={Options.Profile.Proxy.ServerForRequest}");
		} else {
			args.Add("--no-proxy-server");
		}

		if (Options.Emulation.DissableHyperlinkAuditing) {
			// not disable tracking totally, but disable for hyperlink
			args.Add("--disable-hyperlink-auditing");
		}

		args.Add($"--user-data-dir=\"{Options.SysBrowserProfileCachePath}\"");

		List<string> exts = [];
		if (Directory.Exists(Options.DestExtentionsDir)) {
			foreach (var item in Directory.GetDirectories(Options.DestExtentionsDir)) {
				exts.Add(item);
			}
		}
		//foreach (var dir in ExtensionDirectories) {
		//	if (Directory.Exists(dir.Value.AddonDir))
		//		exts.Add(dir.Value.AddonDir);
		//}

		if (Directory.Exists(Consts.Addons.DefaultExtensionsFolderPath))
			exts.AddRange(Directory.GetDirectories(Consts.Addons.DefaultExtensionsFolderPath));

		if (exts.Count > 0)
			args.Add($"--load-extension=\"{exts.ToCommaSeparatedString()}\"");

		args.Add($"about:blank");

		return string.Join(" ", args);
	}

	public void MakeForeground()
	{
		if (Brocess != null) {
			if (!OperatingSystem.IsMacOS()) {
				if (Handle == IntPtr.Zero)
					return;
#pragma warning disable CA1416 // Validate platform compatibility
				if (U32.IsWindow(Handle)) {
					if (U32til.BringWindowToForeground(Handle)) {
						OnBecameForeground?.Invoke(this, Options);
					}
#pragma warning restore CA1416 // Validate platform compatibility
				}
			} else {
				if (MacOSUtil.SetForegroundWindow(Brocess.Id)) {
					Brocess.Refresh();
					OnBecameForeground?.Invoke(this, Options);
				}
			}
		}
	}

	protected async Task StartProcess()
	{
			Brocess = ProUtil.Createa(BrowserType == SystemBrowserType.Firefox ? Consts.Browser.LocalFirefoxExePath : Options.ExePath, GetCommandLineArguments());
			_ = Brocess.Start();

			if (OperatingSystem.IsMacOS()) {
				Handle = Brocess.Handle;
				Brocess.Exited += (s, e) => { Dispose(); };
				var tryCount = 0;
				while (Brocess?.HasExited == false &&
								MacOSUtil.FindWindowByPID(Brocess.Id) == null &&
								tryCount++ < 36) {
					await Task.Delay(1000);
				}

				if (Brocess?.Id is int id)
					MacOSWindowListener.Instance.AddPid(id);

				MacOSWindowListener.Instance.WindowForegroundChanged += OnWindowForeground;
			} else {
#pragma warning disable CA1416 // Validate platform compatibility
				await Task.Delay(1800);

				if (BrowserType != SystemBrowserType.Firefox) {
					string? windowHandle = null;
					while (IsRunning) {
						windowHandle = await GetWebSocketDebuggerUrlAsync();
						if (windowHandle.Is())
							break;

						await Task.Delay(250);
					}
					if (windowHandle?.Is() == false) {
						Dispose();
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
			Dispose();
	}

	private void OnWindowForeground(int i)
	{
		if (i == Brocess?.Id)
			OnBecameForeground?.Invoke(this, Options);
	}

	public void Dispose()
	{
		if (OperatingSystem.IsMacOS()) {
			MacOSWindowListener.Instance.WindowForegroundChanged -= OnWindowForeground;
			if (Brocess?.Id is int id)
				MacOSWindowListener.Instance.RemPid(id);
		}

		var r = LoadedTCS.TrySetResult(false);
		Brocess = null;
		Handle = IntPtr.Zero;
		OnProcessClosed?.Invoke(this, Options);

		GC.SuppressFinalize(this);
	}

	private async Task<string?> GetWebSocketDebuggerUrlAsync()
	{
		var url = $"http://localhost:{Options.Port}/json";
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
