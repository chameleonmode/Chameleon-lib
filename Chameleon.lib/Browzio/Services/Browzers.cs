using System.Collections.Concurrent;
using Chameleon.lib.Browzio.Services.Browzas;
using Chameleon.lib.Helpers;
using Chameleon.lib.Playwright;
using Chameleon.lib.Util;

namespace Chameleon.lib.Browzio.Services;

public class Browzers {
	public enum Event { Unknown, Error, Closed, Opened, Foreground, Background }
	private readonly SemaphoreSlim semaphore = new(1, 1);
	public ConcurrentDictionary<(BrowserType bt, int id), IBrowserInstance> Browsers { get; } = [];
	public ConcurrentDictionary<int, List<Action<object, IBrowserInstance.EventArgs>>> Observers { get; } = [];
	internal Browzers() { }

	public async Task<IBrowserInstance> Launch(BrowserSetting settings) {
		if (settings.WithExtensions) {
			await Playwrightio.I.Initialized.Task;
			settings.Browser.OnEvent += (sender, args) => {
				if (args.Event == Event.Closed) Browsers.TryRemove((settings.BrowserType, settings.Profile.Id), out _);
				if (Observers.TryGetValue(settings.Profile.Id, out var observer))
					observer.ForEach(x => x.Invoke(sender, args));
			};
			await settings.Browser.Initialize();
			return Browsers[(settings.BrowserType, settings.Profile.Id)] = settings.Browser;
		} else {
			_ = settings.Browser.Initialize();
			await settings.Browser.LoadedTCS.Task;
			return settings.Browser;
		}
	}

	public async Task<IBrowserInstance?> Open(BrowserSetting settings) {
		await semaphore.WaitAsync();
		// To wait
		if (
			Browsers.TryGetValue((settings.BrowserType, settings.Profile.Id), out var browser) &&
			browser?.Brocess?.HasExited == false
		) return browser;
		try {
			if (browser != null) await browser.Closee();
			return await EX.Catch(
				async () => browser = await Launch(settings),
				e => {
					if (settings.WithExtensions) Toaster.Error(e.Message);
					_ = Browsers.TryRemove((settings.BrowserType, settings.Profile.Id), out _);
					settings.Browser.InvokeEvent(Event.Error);
				});
		} finally {
			// Signal
			_ = semaphore.Release();
		}
	}

	public void AddObserver(int id, Action<object, IBrowserInstance.EventArgs> action) {
		if (Observers.TryGetValue(id, out var value)) value.Add(action);
		else Observers[id] = [action];
	}
}
