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
	public int TimeOut { get; } = 18;

	private readonly WindowEventHandler? windowEventHandler;
	// TODO:
	// public ConcurrentDictionary<int, Dictionary<SystemBrowserType, IBrowserInstance?>> Instances { get; } = [];
	public ConcurrentDictionary<SysBrowserOpenOptions, IBrowserInstance?> Instances { get; } = [];
	public ConcurrentDictionary<int, List<Delegatorz.Event<SysBrowserEvent>>> Observers { get; } = [];

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
		_ = await Project.Initialized.Task;
		// TODO: test node console standard server launcher vs tcp server 
		// await NodeServerLauncher.Instance.StartServer();
		// TODO: move to app startup or possibly add a lib startup module
		// await AddonsServer.Instance.Start();

		// 
		// TODO: implement a system browser instance factory or for the bigger picture a state machine factory
		// var browser = Instances[settings.OpenOptions.Profile.Id] = new() {
		// 	[SystemBrowserType.Chrome] = new ChromeSysBrowserInstance() { Settings = settings },
		// 	[SystemBrowserType.Brave] =  new BraveSysBrowserInstance() { Settings = settings },
		// 	[SystemBrowserType.Firefox] = new FirefoxSysBrowserInstance() { Settings = settings }
		// };
		// 
		var browser = Instances[settings.OpenOptions] = settings.BrowserType switch {
			SystemBrowserType.Brave => new Brave() { Settings = settings },
			SystemBrowserType.Chrome => new Chrome() { Settings = settings },
			SystemBrowserType.Firefox => new Firefox() { Settings = settings },
			_ => throw new NotImplementedException(),
		};
		await browser.Ensure();
		browser.OnEvent += async (sender, args) => {
			switch (args.EventType) {
				case SysBrowserEventType.Closed:
					if (Instances.TryGetValue(settings.OpenOptions, out var browser) && browser != null) {
						_ = await browser.LoadedTCS.Task;
						_ = Instances.TryRemove(settings.OpenOptions, out _);
						break;
					}
					break;
				default:
					break;
			}

			if (Observers.TryGetValue(settings.Profile.Id, out var events)) {
				events.ForEach(x => x.Invoke(sender, args));
			}
		};
		_ = browser.InitializeAsync();
		if (await browser.LoadedTCS.Task.WaitAsync(TimeSpan.FromSeconds(TimeOut))) {
			browser.InvokeEvent(SysBrowserEventType.Foreground);
			browser.InvokeEvent(SysBrowserEventType.Opened);
			// TODO: ?  await AddonsServer.Instance.WaitListener();
		} else {
			throw new Exception("Browser needs to be restarted to apply changes. Please close and reopen your browser.");
		}
		return Instances[settings.OpenOptions];
	}
	public async Task<IBrowserInstance?> Open(SysBrowserOpenOptions options) {
		var browser = Instances.FirstOrDefault(x => x.Key.Profile.Id == options.Profile.Id && x.Key.BrowserType == options.BrowserType).Value;
		if (browser == null) {
			try {
				browser = await OpenWithSettings(new(options, TcpUtil.NextFreePort(9613)));
			} catch (Exception e) {
				browser?.InvokeEvent(SysBrowserEventType.Error);
				Toaster.Error(e.Message);
				if (e is InvalidDataException or TimeoutException) {
					_ = Instances.TryRemove(options, out _);
					_ = (browser?.LoadedTCS.TrySetResult(false));
				}
				return null;
			}
		} else {
			if (browser.Brocess?.HasExited == true) {
				browser.Close();
				await Task.Delay(256);
				_ = Open(options);
			} else {
				browser.InvokeEvent(SysBrowserEventType.Foreground);
			}
		}
		return browser;
	}

	public IEnumerable<SystemBrowserType> HasInstanceOf(int id, Delegatorz.Event<SysBrowserEvent> action) {
		if (Observers.TryGetValue(id, out var value)) value.Add(action);
		else Observers[id] = [action];
		
		return Instances
			.Where(x => x.Value?.Settings.Profile.Id == id)
			.Select(b => b.Value?.Settings.BrowserType ?? SystemBrowserType.Unknown);
	}

	// Singleton
	public static SystemBrowserService Instance { get; } = new();
}
