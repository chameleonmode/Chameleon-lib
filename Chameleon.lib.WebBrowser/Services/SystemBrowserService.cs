using System.Collections.Concurrent;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.Helpers;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.WebBrowser.Models;
using Chameleon.lib.WebBrowser.System.Brave;
using Chameleon.lib.WebBrowser.System.Chrome;
using Chameleon.lib.WebBrowser.System.Firefox;
using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.WebBrowser.Services;
public class SystemBrowserService {
	SystemBrowserService() {
		if (OperatingSystem.IsWindows()) {
			windowEventHandler = new WindowEventHandler();
			windowEventHandler.OnForeground += U32til_OnForeground;
			windowEventHandler.OnDestroy += U32til_OnClose;
			windowEventHandler.StartListening();
		} else {
			MacOSWindowListener.Instance.WindowForegroundChanged += MacOS_WindowForegroundChanged;
		}
	}
	public int TimeOut { get; } = 36;

	private readonly WindowEventHandler? windowEventHandler;
	// TODO:
	// public ConcurrentDictionary<int, Dictionary<SystemBrowserType, IBrowserInstance?>> Instances { get; } = [];
	public ConcurrentDictionary<SysBrowserOpenOptions, IBrowserInstance?> Instances { get; } = [];
	public ConcurrentDictionary<int, List<Delegatorz.Event<SysBrowserEvent>>> Observers { get; } = [];

	private long _isBusy;
	public bool IsBusy => Interlocked.Read(ref _isBusy) > 0;

	public TaskCompletionSource<IBrowserInstance?>? OpenTaskCompletionSource { get; private set; }

	#region Hwnd
	private async void MacOS_WindowForegroundChanged(int obj) {
		for (var i = Instances.Count - 1; i >= 0; i--) {
			var uid = Instances.Keys.ElementAt(i);
			if (Instances.TryGetValue(uid, out var browser) && browser != null) {
				_ = await browser.LoadedTCS.Task;

				if (browser.Brocess?.HasExited != true && browser.Settings.Profile.Id == obj) {
					browser.InvokeEvent(SysBrowserEventType.Foreground);
					continue;
				}

				if (browser.Brocess?.HasExited != true)
					browser.InvokeEvent(SysBrowserEventType.Background);
			}
		}
	}
	private void U32til_OnClose(nint obj) {
		try {
			for (var i = Instances.Count - 1; i >= 0; i--) {
				var uid = Instances.Keys.ElementAt(i);
				if (Instances.TryGetValue(uid, out var browser) && browser != null && browser.Brocess?.MainWindowHandle != IntPtr.Zero && browser.Brocess?.HasExited == true) {
					browser.Close();
				}
			}
		} catch {
			//Toaster.ShowErr(e.Message);
		}
	}
	private async void U32til_OnForeground(nint obj) {
		try {
			for (var i = Instances.Count - 1; i >= 0; i--) {
				var uid = Instances.Keys.ElementAt(i);
				if (Instances.TryGetValue(uid, out var browser) && browser != null) {
					var loaded = await browser.LoadedTCS.Task;

					if (loaded && browser.Brocess?.HasExited == false && browser.Brocess?.MainWindowHandle == obj) {
						browser.InvokeEvent(SysBrowserEventType.Foreground);
						continue;
					}

					browser.InvokeEvent(SysBrowserEventType.Background);
				}
			}
		} catch {
			//Toaster.ShowErr(e.Message);
		}
	}
	#endregion

	public async Task<IBrowserInstance?> OpenWithSettings(SysBrowserSettings settings) {
		//await NodeServerLauncher.Instance.StartServer();
		await AddonsServer.Instance.Start();

		// 
		var browser = Instances[settings.OpenOptions] = settings.BrowserType switch {
			SystemBrowserType.Brave => new BraveSysBrowserInstance() { Settings = settings },
			SystemBrowserType.Chrome => new ChromeSysBrowserInstance() { Settings = settings },
			SystemBrowserType.Firefox => new FirefoxSysBrowserInstance() { Settings = settings },
			_ => throw new NotImplementedException(),
		};
		// TODO:
		// var browser = Instances[settings.OpenOptions.Profile.Id] = new() {
		// 	[SystemBrowserType.Chrome] = new ChromeSysBrowserInstance() { Settings = settings },
		// 	[SystemBrowserType.Brave] =  new BraveSysBrowserInstance() { Settings = settings },
		// 	[SystemBrowserType.Firefox] = new FirefoxSysBrowserInstance() { Settings = settings }
		// };
		// 
		await browser.Ensure();
		browser.OnEvent += async (sender, args) => {
			// if(args.OpenOptions.BrowserType != settings.OpenOptions.BrowserType) return;
			switch (args.EventType) {
				case SysBrowserEventType.Closed:
					do {
						if (Instances.TryGetValue(settings.OpenOptions, out var browser) && browser != null) {
							_ = await browser.LoadedTCS.Task;
							_ = Instances.TryRemove(settings.OpenOptions, out _);
							break;
						}
						await Task.Delay(250);
					}
					while (IsBusy);
					break;
				default:
					break;
			}

			if (Observers.TryGetValue(settings.Profile.Id, out var events)) {
				events.ForEach(x => x.Invoke(sender, args));
				
				// var check = Instances.FirstOrDefault(x => x.Value.Settings.Profile.Id == settings.Profile.Id);
				// if(check.Value != null) events.ForEach(x => x.Invoke(sender, args));
				// else events.Clear();
			}
		};
		_ = browser.InitializeAsync();
		if (await browser.LoadedTCS.Task.WaitAsync(TimeSpan.FromSeconds(TimeOut))) {
			browser.InvokeEvent(SysBrowserEventType.Foreground);
			browser.InvokeEvent(SysBrowserEventType.Opened);
		} else {
			throw new Exception("Browser Load Context Connection Failed");
		}
		return Instances[settings.OpenOptions];
	}
	public async Task<IBrowserInstance?> Open(SysBrowserOpenOptions options) {
		var browser = Instances.FirstOrDefault(x => x.Key.Profile.Id == options.Profile.Id && x.Key.BrowserType == options.BrowserType).Value;
		if (browser == null) {
			OpenTaskCompletionSource = new TaskCompletionSource<IBrowserInstance?>();
			try {
				browser = await OpenWithSettings(new(
					options, 
					TcpUtil.NextFreePort(9613))
				);
			} catch (Exception e) {
				browser?.InvokeEvent(SysBrowserEventType.Error);
				Toaster.Error(e.Message);
				if (e is InvalidDataException or TimeoutException) {
					_ = Instances.TryRemove(options, out _);
					_ = (OpenTaskCompletionSource?.TrySetResult(null));
					_ = (browser?.LoadedTCS.TrySetResult(false));
				}
				return null;
			} finally {
				_ = Interlocked.Exchange(ref _isBusy, 0);
			}
		} else {
			if (browser?.Brocess?.HasExited == true) {
				browser.Close();
				await Task.Delay(256);
				_ = Open(options);
			} else {
				//browser.InvokeEvent(SysBrowserEventType.Foreground);
			}
		}

		_ = (OpenTaskCompletionSource?.TrySetResult(browser));
		return browser;
	}

	public async Task<(bool, IEnumerable<SystemBrowserType>)> HasInstanceOf(int id,  Delegatorz.Event<SysBrowserEvent> action) {
		if (Observers.TryGetValue(id, out var value)) {
			value.Add(action);
		} else {
			Observers[id] = [action];
		}

		if (OpenTaskCompletionSource != null) {
			var opening = await OpenTaskCompletionSource.Task;
			if (opening != null && opening.Settings.Profile.Id == id) {
				return (true, [opening.Settings.BrowserType]);
			}
		}
		var browsers = Instances.Where(x => x.Value?.Settings.Profile.Id == id);
		if (browsers?.Count() > 0) {
			return (true, browsers.Select(b=>b.Value?.Settings.BrowserType ?? SystemBrowserType.Unknown));
		}
		return (false, [SystemBrowserType.Unknown]);
	}
	// Singleton
	public static SystemBrowserService Instance { get; } = new();
}
