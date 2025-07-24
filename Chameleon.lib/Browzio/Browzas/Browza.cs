using System.Diagnostics;
using Chameleon.lib.Util;
using Chameleon.lib.ThirdParty.GeoIp;
using static Chameleon.lib.Browzio.Browzers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Chameleon.lib.Browzio.Services.Browzas;

public interface IBrowserInstance {
	public record EventArgs(BrowserSetting Settings, Event Event);
	event Action<object, EventArgs>? OnEvent;
	BrowserSetting Settings { get; init; }
	Process? Brocess { get; set; }
	string SessionId { get; }
	void Close();
	Task Closee();
	Task Ensure();
	Process Brocessor();
	void InvokeEvent(Event @event);
	Task Initialize(object? param = null);
	TaskCompletionSource<bool> LoadedTCS { get; }
}

public abstract class Browza : IBrowserInstance {
	public event Action<object, IBrowserInstance.EventArgs>? OnEvent;
	public required BrowserSetting Settings { get; init; }
	public Process? Brocess { get; set; }
	public string SessionId { get; } = Guid.NewGuid().ToString();
	public TaskCompletionSource<bool> LoadedTCS { get; } = new();
	public string InitUrl => Settings.WithExtensions
		? $"http://127.0.0.1:{Browzio.I.Loopback.Port}/init?instanceId={Settings.Profile.Id}&sessionId={SessionId}" 
		: Settings.Profile.StartPage;

	public void InvokeEvent(Event @event) {
		var args = new IBrowserInstance.EventArgs(Settings, @event);
		if (@event == Event.Foreground) {
			Browzio.I.Browzas.Observers.ForEach(kvp => kvp.Value.ForEach(x => x.Invoke(this, args)));
			if (Brocess is not null) Brocessor().Start();
		} else if (@event == Event.Opened) Browzio.I.Browzas.Observers.ForEach(
			kvp => kvp.Value.ForEach(x => x.Invoke(this, new(Settings, Event.Foreground)))
		);
		else OnEvent?.Invoke(this, args);
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

		Settings.Port = Processez.NextFreePort(9613);
		await Ensure();
		await InitializeExtensions();
		// StartProcess
		Brocess = Brocessor();
		Brocess.Start();

		await WaitForWinHandle();
		await Task.Delay(1000);

		Brocess.EnableRaisingEvents = true;
		Brocess.Exited += (sender, e) => {
			// Only close if the process exited with an error or during initialization
			if (LoadedTCS.Task.IsCompleted) Close();
			else if (Brocess.ExitCode == 0) _ = LoadedTCS.TrySetResult(false);
		};

		Brocess.HasExited.ThrowTrue("Browser process has already exited");
		_ = LoadedTCS.TrySetResult(true);
		InvokeEvent(Event.Opened);
	}

	public Process Brocessor() => new() {
		EnableRaisingEvents = true,
		StartInfo = new() {
			FileName = ExePath,
			Arguments = GetCommandLineArguments(),
			UseShellExecute = false,
		  CreateNoWindow = true,
		},
	};
	public virtual string ExeDir => Path.GetDirectoryName(ExePath) ?? string.Empty;
	public abstract string PrefsFile { get; }
	public abstract string ExePath { get; }

	public virtual async Task Ensure() {
		await Task.Delay(600);
	}
	protected virtual async Task InitializeExtensions() {
		if (!Settings.WithExtensions) return;

		// set the extension settings
		var ipapi = await Api.GeoIp(Settings.Profile.Proxy.WebProxy) ?? throw new InvalidTimeZoneException("Unable to get geo ip data");
		Settings.ProxioPort = Browzio.I.Loopback.AddSession(SessionId, Settings, new {
			proxy = Settings.Profile.Proxy.AddonObject,
			urls = new {
				start = Settings.Profile.StartPage,
				homePages = Settings.Profile.Bookmarks,
			},
			tz = new {
				enabled = Settings.Profile.Emulations.Timezone,
				locale = "en-" + ipapi.countryCode,
				system = ipapi.tzSystem,
				zone = ipapi.timezone,
			},
			geo = new {
				enabled = Settings.Profile.Emulations.Geo,
				ipapi.lat,
				ipapi.lon,
			},
			canvas = new { enabled = Settings.Profile.Emulations.Canvas },
			webgl = new { enabled = Settings.Profile.Emulations.WebGL },
			fonts = new { enabled = Settings.Profile.Emulations.Font },
			rects = new { enabled = Settings.Profile.Emulations.Rects },
			navi = new { enabled = Settings.Profile.Emulations.Navigator },
			audio = new { enabled = Settings.Profile.Emulations.Audio },
		});
	}
	protected virtual async Task WaitForWinHandle() {
		await Task.Delay(600);
		if (OperatingSystem.IsWindows()) return;
		await EX.Poly(async () => {
			await Task.Delay(54);
			Brocess!.HasExited.ThrowTrue();
			MacOSWindowListener.Instance.AddPid(Brocess!.Id);
			return (MacOSUtil.FindWindowByPID(Brocess.Id) == null).ThrowIfTrue();
		},
		new(sleep: 96, retries: 3));
	}

	protected abstract string GetCommandLineArguments();
}

