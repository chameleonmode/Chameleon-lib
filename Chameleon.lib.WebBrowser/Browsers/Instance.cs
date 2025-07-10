using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.ThirdParty.GeoIp;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Services;
using System.Diagnostics;

namespace Chameleon.lib.WebBrowser.Browsers;

public enum Event { Unknown, Error, Closed, Opened, Foreground, Background }
public record BrowserEvent(BrowserSetting OpenOptions, Event Event);

public interface IBrowserInstance {
	event Delegatorz.Event<BrowserEvent>? OnEvent;
	Process? Brocess { get; set; }
	BrowserSetting Settings { get; init; }
	string SessionId { get; }
	void InvokeEvent(Event @event);
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
	public required BrowserSetting Settings { get; init; }
	public string SessionId { get; } = Guid.NewGuid().ToString();

	public string InitUrl =>
		$"http://127.0.0.1:{AddonsServer.Instance.Port}/init?instanceId={Settings.Profile.Id}&sessionId={SessionId}";

	public void InvokeEvent(Event @event) {
		if (@event == Event.Foreground && Brocess is not null) {
			if (OperatingSystem.IsWindows() && Brocess.MainWindowHandle is nint handle && U32.IsWindow(handle)) {
				_ = U32til.BringWindowToForeground(handle);
			} else if (OperatingSystem.IsMacOS()) {
				Brocessor(false).Start();
			}
		}

		OnEvent?.Invoke(this, new(Settings, @event));
	}

	public Task Closee() => ProcessUtil.TryKillProcess(Brocess);
	public void Close() {
		InvokeEvent(Event.Closed);
		if (Brocess == null) return;
		_ = LoadedTCS.TrySetResult(false);
		
		MacOSWindowListener.Instance.RemPid(Brocess?.Id);
    Brocess?.Dispose();
		Brocess = null;
	}

	public async Task Initialize(object? param = null) {
		if (Brocess is not null) return;
		
		await Ensure();
		await InitializeExtensions();

		// StartProcess
		Brocess = Brocessor();
		EX.Try(()=> Brocess.Start(), e => throw e);

		await Task.Delay(2000);
		await WaitForWinHandle();
		await Task.Delay(1000);

		if (!Brocess!.HasExited) _ = LoadedTCS.TrySetResult(true);
		else Close();
	}

	public Process Brocessor(bool args = true) {
		Process process = new () {
			StartInfo = new() {
				FileName = ExePath,
				Arguments = GetCommandLineArguments(args),
				UseShellExecute = false,
				CreateNoWindow = true,
			},
			EnableRaisingEvents = true,
		};

		process.Exited += (sender, e) => {
			// Only close if the process exited with an error or during initialization
			if (process.ExitCode != 0) Close();
			else _ = LoadedTCS.TrySetResult(false);
		};

		return process;
	}

	public virtual Task Ensure() => Task.CompletedTask;
	public virtual string ExeDir => Path.GetDirectoryName(ExePath) ?? string.Empty;
	public abstract string PrefsFile { get; }
	public abstract string ExePath { get; }
	protected abstract string GetCommandLineArguments(bool args);
	protected virtual async Task InitializeExtensions() {
		if(!Settings.Profile.Extensions) return;
		
		async Task<Ipapi> Ipapi() {
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
				homePages = Settings.Profile.Bookmarks,
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
	}

	// Non-Windows platforms use base implementation
	protected virtual async Task WaitForWinHandle() {
		Brocess!.Exited += (s, e) => { if(LoadedTCS.Task.IsCompleted) Close(); };
		if (OperatingSystem.IsWindows()) return;
		var result = await EX.Poly(async () => {
			await Task.Delay(54);
			if (!Brocess!.HasExited) throw new InvalidOperationException("Window handle not found.");
			else MacOSWindowListener.Instance.AddPid(Brocess.Id);
			return MacOSUtil.FindWindowByPID(Brocess.Id) != null;
		},
		new(sleep: 100, retries: 6));
	}
}
