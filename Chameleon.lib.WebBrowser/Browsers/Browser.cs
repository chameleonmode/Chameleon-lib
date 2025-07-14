using Chameleon.lib.Util;
using Chameleon.lib.ThirdParty.GeoIp;
using Chameleon.lib.WebBrowser.Services;
using System.Diagnostics;

namespace Chameleon.lib.WebBrowser.Browsers;

public enum Event { Unknown, Error, Closed, Opened, Foreground, Background }
public record BrowserEvent(BrowserSetting Settings, Event Event);

public interface IBrowserInstance {
	Process? Brocess { get; set; }
	BrowserSetting Settings { get; init; }
	string SessionId { get; }
	void InvokeEvent(Event @event);
	void Close();
	Task Closee();
	Task Ensure();
	Process Brocessor(string url);
	TaskCompletionSource<bool> LoadedTCS { get; }
	Task Initialize(object? param = null);
	event Action<object, BrowserEvent>? OnEvent;
}
public abstract class Browser : IBrowserInstance {
	public TaskCompletionSource<bool> LoadedTCS { get; } = new();
	public Process? Brocess { get; set; }
	public required BrowserSetting Settings { get; init; }
	public string SessionId { get; } = Guid.NewGuid().ToString();

	public string InitUrl =>
		$"http://127.0.0.1:{AddonsServer.I.Port}/init?instanceId={Settings.Profile.Id}&sessionId={SessionId}";

	public void InvokeEvent(Event @event) {
		if (@event == Event.Foreground && Brocess is not null) {
			if (OperatingSystem.IsWindows() && Brocess.MainWindowHandle is nint handle && U32.IsWindow(handle)) {
				_ = U32til.BringWindowToForeground(handle);
			} else if (OperatingSystem.IsMacOS()) {
				Brocessor().Start();
			}
		}

		var args = new BrowserEvent(Settings, @event);
		OnEvent?.Invoke(this, args);
	}

	public async Task Closee() => await Processez.TryKillProcess(Brocess);
	public void Close() {
		_ = LoadedTCS.TrySetResult(false);
		InvokeEvent(Event.Closed);
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
		EX.Try(() => Brocess.Start(), e => throw e);

		await WaitForWinHandle();
		await Task.Delay(1000);

		if (!Brocess.HasExited) _ = LoadedTCS.TrySetResult(true);
		else Close();
	}

	public Process Brocessor(string? url = null) {
		Process process = new() {
			StartInfo = new() {
				FileName = ExePath,
				Arguments = GetCommandLineArguments(url),
				UseShellExecute = false,
				CreateNoWindow = true,
			},
			EnableRaisingEvents = true,
		};

		process.Exited += (sender, e) => {
			// Only close if the process exited with an error or during initialization
			if (LoadedTCS.Task.IsCompleted) Close();
			else if (process.ExitCode == 0) _ = LoadedTCS.TrySetResult(false);
			// // {
			// // 	_ = LoadedTCS.TrySetResult(false);
			// // 	// _ = LoadedTCS.TrySetResult(Brocessor(url).Start());
			// // 	// Brocessor(false).Start();
			// // }
		};

		return process;
	}

	public virtual Task Ensure() => Task.CompletedTask;
	public virtual string ExeDir => Path.GetDirectoryName(ExePath) ?? string.Empty;
	public abstract string PrefsFile { get; }
	public abstract string ExePath { get; }
	protected abstract string GetCommandLineArguments(string? url);
	protected virtual async Task InitializeExtensions() {
		if (!Settings.Profile.Extensions) return;

		var ipapi = await Api.GeoIp(Settings.Profile.Proxy.WebProxy) ?? throw new InvalidTimeZoneException("Unable to get geo ip data");
		// set the extension settings
		AddonsServer.I.AddonInstances[SessionId] = (
			new {
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
			},
			Settings.Profile.Port
		);
	}

	// Non-Windows platforms use base implementation
	protected virtual async Task WaitForWinHandle() {
		await Task.Delay(1000);
		if (OperatingSystem.IsWindows()) return;
		var result = await EX.Poly(async () => {
			await Task.Delay(54);
			Brocess!.HasExited.ThrowTrue();
			MacOSWindowListener.Instance.AddPid(Brocess!.Id);
			return (MacOSUtil.FindWindowByPID(Brocess.Id) == null).ThrowIfTrue();
		},
		new(sleep: 100, retries: 6));
		result.ThrowTrue();
	}

	public event Action<object, BrowserEvent>? OnEvent;
}
