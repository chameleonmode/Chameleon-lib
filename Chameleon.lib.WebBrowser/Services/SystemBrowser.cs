using System.Collections.Concurrent;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.Helpers;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Browsers;
using Chameleon.lib.WebBrowser.System.Brave;
using Chameleon.lib.WebBrowser.System.Chrome;
using Chameleon.lib.WebBrowser.System.Firefox;

namespace Chameleon.lib.WebBrowser.Services;

public class SystemBrowser {
	private readonly WindowEventHandler? windowEventHandler;
	public int TimeOut { get; } = 14;
	public ConcurrentDictionary<LaunchOptions, IBrowserInstance> Instances { get; } = [];
	public ConcurrentDictionary<int, List<Delegatorz.Event<BrowserEvent>>> Observers { get; } = [];
	SystemBrowser() {
		if (OperatingSystem.IsWindows()) {
			windowEventHandler = new WindowEventHandler();
			windowEventHandler.OnDestroy += (obj) => {
				EX.Try(() => {
					for (var i = Instances.Count - 1; i >= 0; i--) {
						if (!Instances.TryGetValue(Instances.Keys.ElementAt(i), out var browser) ||
								browser == null ||
								browser.Brocess?.MainWindowHandle != IntPtr.Zero ||
								browser.Brocess?.HasExited == true
						) { continue; } else { browser.Close(); }
					}
				});
			};
			windowEventHandler.StartListening();
		}
	}

	public async Task<IBrowserInstance> Open(BrowserSettings settings) {
		if (settings.Profile.Extensions) _ = await Project.Initialized.Task;
		// TODO: test node console standard server launcher vs tcp server 
		// await NodeServerLauncher.Instance.StartServer();
		// TODO: move to app startup or possibly add a lib startup module
		// await AddonsServer.Instance.Start();
		var browser = Instances[settings.OpenOptions] = settings.BrowserType switch {
			SystemBrowserType.Brave => new Brave() { Settings = settings },
			SystemBrowserType.Chrome => new Chrome() { Settings = settings },
			SystemBrowserType.Firefox => new Firefox() { Settings = settings },
			_ => throw new NotImplementedException(),
		};
		if(
			!Instances.ContainsKey(settings.OpenOptions) &&
			!Instances.TryAdd(settings.OpenOptions, browser) && !settings.Profile.Extensions
		) throw new Exception("Browser instance already exists. Please close the browser before opening a new one."); 
		
		browser.OnEvent += async (sender, args) => {
			if (args.EventType == BrowserEventType.Closed) {
				_ = await browser.LoadedTCS.Task;
				_ = Instances.TryRemove(settings.OpenOptions, out _);
			}
			//if(Observers.TryGetValue(settings.Profile.Id, out var value)) value.ForEach(x => x.Invoke(sender, args));
			if(Observers.TryGetValue(settings.Profile.Id, out var value)) value.ForEach(x => x.Invoke(sender, args));
		};
		_ = browser.InitializeAsync();
		if (await browser.LoadedTCS.Task.WaitAsync(TimeSpan.FromSeconds(settings.Profile.Extensions ? TimeOut : 6))) browser.InvokeEvent(BrowserEventType.Opened);
		else if(!settings.Profile.Extensions) throw new Exception("Browser needs to be restarted to apply changes. Please close and reopen your browser.");
		return Instances[settings.OpenOptions];
	}
	public async Task<IBrowserInstance?> Open(LaunchOptions options) {
		var browser = Instances.FirstOrDefault(x => x.Key.Profile.Id == options.Profile.Id && x.Key.BrowserType == options.BrowserType).Value;
		if (browser == null) {
			options.Profile.Port = options.Profile.Port == 0 ? TcpUtil.NextFreePort(9613) : options.Profile.Port;
			var settings = new BrowserSettings(options);
      try {
				return browser = await Open(settings);
			} catch (Exception e) {
				Toaster.Error(e.Message);
				if (browser != null) browser.InvokeEvent(BrowserEventType.Error);
				else if (Observers.TryGetValue(settings.Profile.Id, out var events)) events.ForEach(x => x.Invoke(this, new(options, BrowserEventType.Error)));
				
				if (e is InvalidDataException or TimeoutException && Instances.ContainsKey(options)) _ = Instances.TryRemove(options, out _);
				_ = browser?.LoadedTCS.TrySetResult(false);
			}
		} else if (browser.Brocess?.HasExited == true) {
				browser.Close();
				await Task.Delay(256);
				return await Open(options);
		} else {
				browser.InvokeEvent(BrowserEventType.Foreground);
		}
		return browser;
	}

	public IEnumerable<SystemBrowserType> HasInstanceOf(int id, Delegatorz.Event<BrowserEvent> action) {
		if (Observers.TryGetValue(id, out var value)) value.Add(action);
		else Observers[id] = [action];

		return Instances
			.Where(x => x.Value?.Settings.Profile.Id == id)
			.Select(b => b.Value?.Settings.BrowserType ?? SystemBrowserType.Unknown);
	}

	// Singleton
	public static SystemBrowser I { get; } = new();
}
