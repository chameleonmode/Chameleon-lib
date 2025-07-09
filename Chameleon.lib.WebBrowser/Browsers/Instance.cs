using chameleon.assets;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.Helpers;
using Chameleon.lib.ThirdParty.GeoIp;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Services;
using System.Diagnostics;

namespace Chameleon.lib.WebBrowser.Browsers;

public enum BrowserEventType { Unknown, Error, Closed, Opened, Foreground, Background }
public record BrowserEvent(LaunchOptions OpenOptions, BrowserEventType EventType);

public interface IBrowserInstance {
	event Delegatorz.Event<BrowserEvent>? OnEvent;
	Process? Brocess { get; set; }
	BrowserSettings Settings { get; init; }
	string SessionId { get; }
	void InvokeEvent(BrowserEventType eventType);
	void Close();
	Task Closee();
	Task Ensure();
	Process Brocessor(bool args);
	TaskCompletionSource<bool> LoadedTCS { get; }
	Task Initialize(object? param = null);
}
public abstract class Browser : IBrowserInstance {
	public TaskCompletionSource<bool> LoadedTCS { get; } = new();
	public event Delegatorz.Event<BrowserEvent>? OnEvent;
	public Process? Brocess { get; set; }
	public required BrowserSettings Settings { get; init; }
	public string SessionId { get; } = Guid.NewGuid().ToString();

	public string InitUrl =>
		$"http://127.0.0.1:{AddonsServer.Instance.Port}/init?instanceId={Settings.Profile.Id}&sessionId={SessionId}";

	public void InvokeEvent(BrowserEventType eventType) {
		if (eventType == BrowserEventType.Foreground && Brocess is not null) {
			if (OperatingSystem.IsWindows() && Brocess.MainWindowHandle is nint handle && U32.IsWindow(handle)) {
				_ = U32til.BringWindowToForeground(handle);
			} else if (OperatingSystem.IsMacOS()) {
				Brocessor(false).Start();
			}
		}

		OnEvent?.Invoke(this, new(Settings.OpenOptions, eventType));
	}

	public Task Closee() => ProcessUtil.TryKillProcess(Brocess);
	public void Close() {
		if (Brocess == null) return;
		
		if (OperatingSystem.IsMacOS()) MacOSWindowListener.Instance.RemPid(Brocess?.Id);
		
		try {
			if (!LoadedTCS.Task.IsCompleted) {
				_ = LoadedTCS.TrySetResult(false);
			}
		} catch { }
		
		try {
			Brocess?.Dispose();
		} catch { }
		
		Brocess = null;
		InvokeEvent(BrowserEventType.Closed);
	}

	public async Task Initialize(object? param = null) {
		if (Brocess is not null) return;
		
		await Ensure();
		await InitializeExtensions();
		if (LoadedTCS.Task.IsCompleted) return;
		Debug.WriteLine($"Starting {ExePath} with url: {InitUrl}");

		// StartProcess
		Brocess = Brocessor(true);
		Brocess.Start();

		await Task.Delay(1800);
		await WaitForWinHandle();

		if (!Brocess.HasExited)
			_ = LoadedTCS.TrySetResult(true);
		else
			Close();

	}

	public Process Brocessor(bool args) => new() {
		StartInfo = new() {
			FileName = ExePath,
			Arguments = GetCommandLineArguments(args),
			UseShellExecute = false,
			CreateNoWindow = true,
		},
		EnableRaisingEvents = true,
	};

	public virtual Task Ensure() => Task.CompletedTask;
	public virtual string ExeDir => Path.GetDirectoryName(ExePath) ?? string.Empty;
	public abstract string PrefsFile { get; }
	public abstract string ExePath { get; }
	protected virtual async Task InitializeExtensions() {
		if(!Settings.Profile.Extensions) return;
		
		async Task<Ipapi> Ipapi() {
			// Ipapi? ipapi = null;

			// var dir = Resources.Assert(
			// 	Settings.Cached, "geo"
			// );
			// var file = Path.Combine(dir, "ipapi.json");
			// if (
			// 	 File.Exists(file) && await File.ReadAllTextAsync(file) is { } json &&
			// 	 JSON.Parse<BrowserProxy>((ipapi = JSON.Parse<Ipapi>(json)).proxy) is { } proxy &&
			// 	 proxy.Host == Settings.Profile.Proxy.Host &&
			// 	 proxy.Port == Settings.Profile.Proxy.Port &&
			// 	 proxy.UserName == Settings.Profile.Proxy.UserName &&
			// 	 proxy.Password == Settings.Profile.Proxy.Password
			// ) {
			// 	Toaster.Info($"Using cached timezone/geo data for {Settings.Profile.Proxy.Host}");
			// 	return ipapi;
			// }
			// Toaster.Info($"Requesting timezone/geo data for {Settings.Profile.Proxy.WebProxy?.Address?.Host ?? "local"}");
			// ipapi = await GeoIpApi.GetIpapi(Settings.Profile.Proxy.WebProxy, e => Toaster.Error(e)) ?? new() {
			// 	timezone = "Pacific/Honolulu",
			// 	lat = 34.052235,
			// 	lon = -118.243683,
			// 	tzSystem = true
			// };
			// ipapi.proxy = JSON.Serialize(Settings.Profile.Proxy);
			// await File.WriteAllTextAsync(file, JSON.Serialize(ipapi));
			// return ipapi;
			return await Api.GeoIp(Settings.Profile.Proxy.WebProxy) ?? new() {
				timezone = "Pacific/Honolulu",
				lat = 34.052235,
				lon = -118.243683,
				tzSystem = true
			};
		}
		var ipapi = await Ipapi();

		// set the extension settings
		AddonsServer.Instance.AddonInstances[SessionId] = new {
			proxy = new {
				enabled = Settings.Profile.Proxy.CanUse,
				type = "http",
				server = Settings.Profile.Proxy.Server,
				host = Settings.Profile.Proxy.Host,
				port = Settings.Profile.Proxy.Port,
				username = Settings.Profile.Proxy.UserName,
				password = Settings.Profile.Proxy.Password,
			},
			urls = new {
				start = Settings.Profile.StartUrl,
				homePages = Settings.Profile.DefaultHomePageSettings,
			},
			tz = new {
				enabled = Settings.Profile.Emulations.AutoTimezone,
				zone = ipapi.timezone,
				system = ipapi.tzSystem,
				locale = "en-" + ipapi.countryCode,
			},
			geo = new {
				enabled = Settings.Profile.Emulations.SpoofGeoLocation,
				ipapi.lat,
				ipapi.lon,
			},
			canvas = new {
				enabled = Settings.Profile.Emulations.SpoofCanvasFingerprint,
			},
			webgl = new {
				enabled = Settings.Profile.Emulations.SpoofWebGLFingerprint,
			},
			rects = new {
				enabled = Settings.Profile.Emulations.SpoofClientRects,
			},
			fonts = new {
				enabled = Settings.Profile.Emulations.SpoofFontFingerprint,
			},
			audio = new {
				enabled = Settings.Profile.Emulations.SpoofAudio,
			},
			navi = new {
				enabled = Settings.Profile.Emulations.SpoofNavigator,
			},
			};
		
		await InitializeExtensionPath();
		if (LoadedTCS.Task.IsCompleted) return;

		Brocess = Brocessor(true);
		var started = Brocess.Start();

		await Task.Delay(1800);
		await WaitForWinHandle();

		if (Brocess != null) {
			try {
				var processId = Brocess.Id;
				await Task.Delay(2000);
				
				try {
					using var testProcess = Process.GetProcessById(processId);
					if (testProcess.HasExited) {
						Close();
						return;
					}
				} catch (ArgumentException) {
					Close();
					return;
				}
				
				try {
					var hasExited = Brocess.HasExited;
					if (!hasExited && !LoadedTCS.Task.IsCompleted) {
						_ = LoadedTCS.TrySetResult(true);
					} else {
						Close();
					}
				} catch (InvalidOperationException) {
					Close();
				}
			} catch (Exception) {
				Close();
			}
		} else {
			Close();
		}
	}

	protected abstract Task InitializeExtensionPath();
	protected abstract string GetCommandLineArguments(bool args);

	protected virtual async Task WaitForWinHandle() {
		Brocess!.Exited += (s, e) => { Close(); };
		if (await TaskUtil.AwaitFor(() =>
		Brocess?.HasExited == false && MacOSUtil.FindWindowByPID(Brocess.Id) != null,
				36,
				1000
			)) {
			MacOSWindowListener.Instance.AddPid(Brocess!.Id);
		}
	}
}
