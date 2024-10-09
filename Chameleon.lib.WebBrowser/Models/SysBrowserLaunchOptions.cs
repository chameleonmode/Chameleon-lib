using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;

using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Models;
using Chameleon.lib.Common.ServiceManagers;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.ThirdParty.GeoIp;
using Chameleon.lib.WebBrowser.Util;

using Newtonsoft.Json.Linq;

using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.WebBrowser.Models;
public record SysBrowserOpenOptions(Enums.SystemBrowserType BrowserType, UserProfileModel Profile);
public record SysBrowserSettings(SysBrowserOpenOptions OpenOptions, EmulationOptions Emulation, string StartUrl, int Port) {
	public Enums.SystemBrowserType BrowserType => OpenOptions.BrowserType;
	public UserProfileModel Profile => OpenOptions.Profile;
	public string SysBrowseUserExtDir => Path.Combine(Consts.Addons.DefaultExtensionsFolderPath, BrowserType.GetDescription());
	public string ExePath => BrowserType == Enums.SystemBrowserType.Firefox ? Consts.Browser.LocalFirefoxExePath : SysBrowserInfoUtil.FindByType(BrowserType).Path;
	public string SysBrowserProfileCachePath => IOtil.EnsureDirectoryExists(Path.Combine(Consts.AppDataLocalDir, BrowserType.ToString(), Profile.Id.ToString()));

	private string? _destextPath;
	public string DestExtentionsDir {
		get {
			if (_destextPath == null) {
				_destextPath = Path.Combine(Consts.Addons.AddonExtentionDir, BrowserType.ToString(), Profile.Id.ToString());
				IOtil.DeleteDExists(_destextPath);
				_destextPath = IOtil.EnsureDirectoryExists(Path.Combine(_destextPath, Guid.NewGuid().ToString()));
			}
			return _destextPath;
		}
	}
	public SysBrowserEvent CreateEvent(Enums.SysBrowserEventType sysBrowserEventType) => new(OpenOptions, sysBrowserEventType);

	public Process? Brocess { get; set; }
	public async Task<bool> StartProcess(string args, Action @close)
	{
		Brocess = ProUtil.Createa(ExePath, args);
		_ = Brocess.Start();

		if (OperatingSystem.IsMacOS()) {
			Brocess.Exited += (s, e) => { @close(); };
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

			if (BrowserType != Enums.SystemBrowserType.Firefox) {
				string? windowHandle = null;
				while (Brocess?.HasExited == false) {
					windowHandle = await GetWebSocketDebuggerUrlAsync();
					if (windowHandle.Is())
						break;

					await Task.Delay(250);
				}
				if (windowHandle?.Is() == false) {
					return false;
				}

				_ = await TaskUtil.AwaitFor(() => Brocess?.MainWindowHandle != IntPtr.Zero, 18);
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
						if (Brocess?.MainWindowHandle != IntPtr.Zero)
							break;
						Thread.Sleep(100);
					}
					if (Brocess?.MainWindowHandle == IntPtr.Zero)
						_ = thisTcs.TrySetResult(null);
				}).Start();
				Brocess = await thisTcs.Task;
			}
#pragma warning restore CA1416 // Validate platform compatibility
		}

		return Brocess?.HasExited == false;
	}
	private async Task<string?> GetWebSocketDebuggerUrlAsync()
	{
		var url = $"http://localhost:{Port}/json";
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

	public Dictionary<Enums.ExtensionType, (string? settings, string guid)> ExtentionsDirs { get; } = [];
	public async Task<string?> BuildExtSettings()
	{
		var timezone = "America/Los_Angeles";
		if (Emulation.AutoTimezone && Profile.Proxy.CanUse) {
			try {
				var ipapi = await GeoIpApi.GetIpapi(Profile.Proxy.ServerForRequest, e => Toaster.ShowErr(e),
					Profile.Proxy.UserName, Profile.Proxy.Password).ConfigureAwait(false);
				if (ipapi != null) {
					timezone = ipapi.timezone;
				}
			} catch (Exception ex) {
				Toaster.ShowErr($"Request for timezone failed, {Profile.Proxy.Server} - {ex.Message}");
			}
		}

		HashSet<KeyValuePair<string, string>> options =
		[
			new ("webglSpoofing", Emulation.SpoofWebGLFingerprint.Tlwr()),
			new ("canvasProtection", Emulation.SpoofCanvasFingerprint.Tlwr()),
			new ("clientRectsSpoofing",Emulation.SpoofClientRects.Tlwr()),
			new ("fontsSpoofing", Emulation.SpoofFontFingerprint.Tlwr()),
			new ("dAPI", Emulation.DisableWebRTC.Tlwr()),
			new ("webRtcEnabled", Emulation.DisableWebRTC.Tlwr()),
			new ("geoSpoofing", Emulation.SpoofGeoLocation.Tlwr()),
			new ("timezoneSpoofing", Emulation.AutoTimezone.Tlwr()),
			new ("myIP", (!Emulation.AutoTimezone).Tlwr()),
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


}
