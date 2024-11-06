using System.Diagnostics;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Util.Mac;

using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Win;
using static Chameleon.lib.Common.Constants.Enums;
using System.Text.Json;

namespace Chameleon.lib.Common.Models;
public class EmulationOptions {
	public bool AutoTimezone { get; set; } = true;
	public bool SpoofGeoLocation { get; set; } = true;
	public bool SpoofWebGLFingerprint { get; set; } = true;
	public bool SpoofCanvasFingerprint { get; set; } = true;
	public bool SpoofClientRects { get; set; } = true;
	public bool SpoofFontFingerprint { get; set; } = true;
	public bool DisableWebRTC { get; set; } = true;
}
public record SysBrowserEvent(SysBrowserOpenOptions OpenOptions, SysBrowserEventType EventType);
public record class SysBrowserRecord(string Name, string Path) {
	public override string ToString()
	{
		return Name ?? Path;
	}
}
public record SysBrowserOpenOptions(Enums.SystemBrowserType BrowserType, SysBrowserProfile Profile);
public record SysBrowserSettings(SysBrowserOpenOptions OpenOptions, EmulationOptions Emulation, string StartUrl, int Port) {
	public Enums.SystemBrowserType BrowserType => OpenOptions.BrowserType;
	public SysBrowserProfile Profile => OpenOptions.Profile;
	public string SysBrowseUserExtDir => Path.Combine(Consts.Addons.DefaultExtensionsFolderPath, BrowserType.GetDescription());
	public string ExePath => BrowserType == Enums.SystemBrowserType.Firefox ? Consts.Browser.LocalFirefoxExePath : SysBrowserInfoUtil.FindByType(BrowserType).Path;
	public string SysBrowserProfileCachePath => IOtil.EnsureDirectoryExists(Path.Combine(Consts.AppDataLocalDir, BrowserType.ToString(), Profile.Id.ToString()));

	private string? destextPath;
	public string DestExtentionsDir {
		get {
			if (destextPath == null) {
				destextPath = Path.Combine(Consts.Addons.AddonExtentionDir, BrowserType.ToString(), Profile.Id.ToString());
				IOtil.DeleteDExists(destextPath);
				destextPath = IOtil.EnsureDirectoryExists(Path.Combine(destextPath, Guid.NewGuid().ToString()));
			}
			return destextPath;
		}
	}
	private string? cachedExtentionsDir;
	public string CachedExtentionsDir {
		get {
			cachedExtentionsDir ??= IOtil.EnsureDirectoryExists(Path.Combine(Consts.Addons.CachedExtentionDir, BrowserType.ToString(), Profile.Id.ToString()));
			return cachedExtentionsDir;
		}
	}

	public HashSet<KeyValuePair<string, string>> EmulationOptions =>
	[
			new ("webglSpoofing", Emulation.SpoofWebGLFingerprint.Tlwr()),
			new ("canvasProtection", Emulation.SpoofCanvasFingerprint.Tlwr()),
			new ("clientRectsSpoofing",Emulation.SpoofClientRects.Tlwr()),
			new ("fontsSpoofing", Emulation.SpoofFontFingerprint.Tlwr()),
			new ("geoSpoofing", Emulation.SpoofGeoLocation.Tlwr()),
			new ("timezoneSpoofing", Emulation.AutoTimezone.Tlwr()),
	];
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

	public Dictionary<Enums.ExtensionType, (string? settings, string guid, string destDir)> ExtentionsDirs { get; } = [];

	public async Task<string> BuildMeleonExtSettings(Func<Task<Ipapi>> @getimezone, string extDir)
	{
		var ipapi = await @getimezone();
		var options = EmulationOptions;
		var settingsBuilder = new StringBuilder();
		_ = settingsBuilder.AppendLine("{");
		_ = settingsBuilder.AppendLine($"\"enabled\": {options.Any(o => o.Value == "true").Tlwr()},");
		foreach (var o in options) {
			_ = settingsBuilder.AppendLine($"\"{o.Key}\": {o.Value},");
		}
		_ = settingsBuilder.AppendLine($"\"timezone\": \"{ipapi.timezone}\",");
		_ = settingsBuilder.AppendLine($"\"latitude\": {ipapi.lat},");
		_ = settingsBuilder.AppendLine($"\"longitude\":{ipapi.lon},");
		_ = settingsBuilder.AppendLine($"\"debug\":{(Debugger.IsAttached ? 5 : -1)},");
		_ = settingsBuilder.AppendLine(
"""
"myIP": false,
"dAPI": true,
"webRtcEnabled": true,
"randomizeTZ": false,
"randomizeGeo": false,
"noiseLevel": "medium",
"eMode": "disable_non_proxied_udp",
"dMode": "default_public_interface_only",
"locale": "en-US",
"accuracy": 69.96,
"bypass": [],
"history": []
""");
		_ = settingsBuilder.AppendLine("}");
		await File.WriteAllTextAsync(Path.Combine(extDir, "settings.json"), settingsBuilder.ToString());

		return settingsBuilder.ToString();
	}

	public string BuildProxyExtSettings()
	{
		var enabled = Profile.Proxy.CanUse ? "true" : "false";

		return @$"let settings = {{
			   enabled: {enabled},
			   type: 'http',
				 server: '{Profile.Proxy.Server}',
			   host: '{Profile.Proxy.Host}',
			   port: {Profile.Proxy.Port},
			   username: '{Profile.Proxy.UserName}',
			   password: '{Profile.Proxy.Password}',
			   url: '{StartUrl}',
			   debug: true,
			}};";
	}
}
