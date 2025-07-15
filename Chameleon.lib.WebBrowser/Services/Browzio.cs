using Chameleon.lib.Util;
using Chameleon.lib.ThirdParty.GeoIp;
using System.Diagnostics;
using Chameleon.lib.Helpers;
using System.Collections.Concurrent;
using static Chameleon.lib.WebBrowser.IBrowserInstance;

namespace Chameleon.lib.WebBrowser.Services;

public abstract class Browser : IBrowserInstance {
	public TaskCompletionSource<bool> LoadedTCS { get; } = new();
	public Process? Brocess { get; set; }
	public required BrowserSetting Settings { get; init; }
	public string SessionId { get; } = Guid.NewGuid().ToString();

	public string InitUrl =>
		$"http://127.0.0.1:{AddonsServer.I.Port}/init?instanceId={Settings.Profile.Id}&sessionId={SessionId}";

	public void InvokeEvent(Event @event) {
		var args = new IBrowserInstance.EventArgs(Settings, @event);
		if (@event == Event.Foreground){
			Browzio.I.Observers.ForEach(kvp => kvp.Value.ForEach(x => x.Invoke(this, args)));
			if(Brocess is not null) Brocessor().Start();
		}
		else if (@event == Event.Opened) Browzio.I.Observers.ForEach(kvp => kvp.Value.ForEach(x => x.Invoke(this, new(Settings, Event.Foreground))));
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

		await Ensure();
		await InitializeExtensions();

		// StartProcess
		Brocess = Brocessor();
		EX.Try(() => Brocess.Start(), e => throw e);

		await WaitForWinHandle();
		await Task.Delay(1000);

		Brocess.EnableRaisingEvents = true;
		Brocess.Exited += (sender, e) => {
			// Only close if the process exited with an error or during initialization
			if (LoadedTCS.Task.IsCompleted) Close();
			else if (Brocess.ExitCode == 0) _ = LoadedTCS.TrySetResult(false);
		};

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
			Settings.Profile.Port,
			Settings.BrowserType
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

	public event Action<object, IBrowserInstance.EventArgs>? OnEvent;
}

public class Browzio {
	public int TimeOut { get; } = 14;
	public ConcurrentDictionary<(BrowserType bt, int id), IBrowserInstance> Browsers { get; } = [];
	public ConcurrentDictionary<int, List<Action<object, IBrowserInstance.EventArgs>>> Observers { get; } = [];
	Browzio() { }

	public async Task<IBrowserInstance> Launch(BrowserSetting settings) {
		if (settings.Profile.Extensions) _ = await Project.Initialized.Task;
		settings.Profile.Port = Processez.NextFreePort(9613);
		settings.Browser.OnEvent += (sender, args) => {
			if (args.Event == Event.Closed) Browsers.TryRemove((settings.BrowserType, settings.Profile.Id), out _);
			if (Observers.TryGetValue(settings.Profile.Id, out var observer))
				observer.ForEach(x => x.Invoke(sender, args));
		};
		var init = settings.Browser.Initialize;
		if (settings.Profile.Extensions) await init().WaitAsync(TimeSpan.FromSeconds(TimeOut));
		else _ = init();
		await settings.Browser.LoadedTCS.Task.WaitAsync(TimeSpan.FromSeconds(3));
		settings.Browser.InvokeEvent(Event.Opened);
		return Browsers[(settings.BrowserType, settings.Profile.Id)] = settings.Browser;
	}
	
	public async Task<IBrowserInstance> Open(BrowserSetting settings) {
		if (!Browsers.TryGetValue((settings.BrowserType, settings.Profile.Id), out var browser) || browser == null) {
			return await EX.Catch(
				async () => browser = await Launch(settings),
				e => {
					if (settings.Profile.Extensions) Toaster.Error(e.Message);
					_ = Browsers.TryRemove((settings.BrowserType, settings.Profile.Id), out _);
					settings.Browser.InvokeEvent(Event.Error);
				}) ?? throw new InvalidOperationException();
		} else if (browser.Brocess is null || browser.Brocess.HasExited) {
			await browser.Closee();
			browser.Close();
			await Task.Delay(256);
			return await Open(settings);
		}
		return browser;
	}

	public IEnumerable<BrowserType> HasInstanceOf(int id, Action<object, IBrowserInstance.EventArgs> action) {
		if (Observers.TryGetValue(id, out var value)) value.Add(action);
		else Observers[id] = [action];

		return Browsers
			.Where(x => x.Value.Settings.Profile.Id == id)
			.Select(b => b.Value.Settings.BrowserType);
	}

	public void CleanupStaleInstances() {
		var staleBrowsers = new List<(BrowserType, int)>();

		foreach (var (key, browser) in Browsers) {
			if (
				browser.Brocess != null && (
				browser.Brocess.HasExited == false || (
				OperatingSystem.IsWindows() &&
				browser.Brocess.MainWindowHandle == IntPtr.Zero)
			)) continue;
			staleBrowsers.Add(key);
		}

		foreach (var options in staleBrowsers) {
			if (Browsers.TryRemove(options, out var staleBrowser)) {
				EX.Try(staleBrowser.Close);
			}
		}
	}

	// Singleton
	public static Browzio I { get; } = new();
}
