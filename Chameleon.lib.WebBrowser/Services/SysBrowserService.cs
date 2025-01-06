using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using Chameleon.lib.Common;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Interfaces.Sys;
using Chameleon.lib.Common.Models;
using Chameleon.lib.Common.ServiceManagers;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.WebBrowser.System.Brave;
using Chameleon.lib.WebBrowser.System.Chrome;
using Chameleon.lib.WebBrowser.System.Firefox;

namespace Chameleon.lib.WebBrowser.Services;
public class SysBrowserService
	: ISysBrowserService {
	public static ISysBrowserInstance Create(Enums.SystemBrowserType browserType, SysBrowserSettings launchOptions) => browserType switch {
		Enums.SystemBrowserType.Brave => new BraveSysBrowserInstance() { Settings = launchOptions },
		Enums.SystemBrowserType.Chrome => new ChromeSysBrowserInstance() { Settings = launchOptions },
		Enums.SystemBrowserType.Firefox => new FirefoxSysBrowserInstance() { Settings = launchOptions },
		_ => throw new NotImplementedException(),
	};

	private readonly WindowEventHandler? windowEventHandler;

	public ConcurrentDictionary<SysBrowserOpenOptions, ISysBrowserInstance> Instances { get; } = [];

	private long _isBusy;
	public bool IsBusy => Interlocked.Read(ref _isBusy) > 0;

	public TaskCompletionSource<ISysBrowserInstance?>? OpenTaskCompletionSource { get; private set; }

	public SysBrowserService()
	{
		if (OperatingSystem.IsWindows()) {
			windowEventHandler = new WindowEventHandler();
			windowEventHandler.OnForeground += U32til_OnForeground;
			windowEventHandler.OnDestroy += U32til_OnClose;
			windowEventHandler.StartListening();
		} else {
			MacOSWindowListener.Instance.WindowForegroundChanged += MacOS_WindowForegroundChanged;
		}
	}

	private async void MacOS_WindowForegroundChanged(int obj)
	{
		for (var i = Instances.Count - 1; i >= 0; i--) {
			var uid = Instances.Keys.ElementAt(i);
			if (Instances.TryGetValue(uid, out var browser)) {
				_ = await browser.LoadedTCS.Task;

				if (browser.Brocess?.HasExited != true && browser.Settings.Profile.Id == obj) {
					browser.InvokeEvent(Enums.SysBrowserEventType.Foreground);
					continue;
				}

				if(browser.Brocess?.HasExited != true)
					browser.InvokeEvent(Enums.SysBrowserEventType.Background);
			}
		}
	}

	private void U32til_OnClose(nint obj)
	{
		try {
			for (var i = Instances.Count - 1; i >= 0; i--) {
				var uid = Instances.Keys.ElementAt(i);
				if (Instances.TryGetValue(uid, out var browser) && browser.Brocess?.HasExited == true) {
						browser.Close();
				}
			}
		} catch{
			//Toaster.ShowErr(e.Message);
		}
	}

	private async void U32til_OnForeground(nint obj)
	{
		try {
			for (var i = Instances.Count - 1; i >= 0; i--) {
				var uid = Instances.Keys.ElementAt(i);
				if (Instances.TryGetValue(uid, out var browser)) {
					var loaded = await browser.LoadedTCS.Task;

					if (loaded && browser.Brocess?.HasExited == false && browser.Brocess?.MainWindowHandle == obj) {
						browser.InvokeEvent(Enums.SysBrowserEventType.Foreground);
						continue;
					}

					browser.InvokeEvent(Enums.SysBrowserEventType.Background);
				}
			}
		} catch {
			//Toaster.ShowErr(e.Message);
		}
	}

	public async Task<ISysBrowserInstance?> Open(SysBrowserOpenOptions options)
	{
		if (!Instances.TryGetValue(options, out var browser)) {
			OpenTaskCompletionSource = new TaskCompletionSource<ISysBrowserInstance?>();
			if (options.BrowserType == Enums.SystemBrowserType.Firefox) {
				var systempath = SysBrowserInfoUtil.FindByType(Enums.SystemBrowserType.Firefox).Path;
				if (IOtil.IsNeedUpdate(systempath, Consts.Browser.LocalFirefoxExePath)) {
					Toaster.Info("Updating Firefox browser. Please wait...");

					await IOtil.DeleteDExistsAsync(Consts.Browser.LocalFirefoxDirPath);

					await IOtil.CopyFolderAsync(OperatingSystem.IsMacOS()
						? "Applications/firefox.app"
						: Path.GetDirectoryName(systempath)!, Consts.Browser.LocalFirefoxDirPath);

					await Task.Delay(1000);
					Toaster.Success("Firefox browser updated successfully.");
				}
			}
			try {
				var emulations = IoC.GetJsonValue<EmulationOptions>(nameof(EmulationOptions)) ?? new();
				var urls = IoC.GetJsonValue<string[]>("DefaultHomePageSettings");
				if(urls is null || urls.Length == 0)
					urls = [Consts.DefaultHomePage];

				var starturl = urls[new Random().Next(urls.Length)];
				starturl = Uri.TryCreate(starturl, UriKind.Absolute, out var uriResult) 
					&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
					? starturl
					: "https://" + starturl;

				var launchOptions = new SysBrowserSettings(options, emulations, starturl, Netil.NextFreePort(9613));
				//
				browser = Create(options.BrowserType, launchOptions);
				browser.OnEvent += Browser_OnEvent;

				Instances[options] = browser;
			  var initTask = browser.InitializeAsync();
				_ = await browser.PreLoadedTCS.Task.WaitAsync(TimeSpan.FromSeconds(8));
				if (await browser.LoadedTCS.Task.WaitAsync(TimeSpan.FromSeconds(16))) {
					browser.InvokeEvent(Enums.SysBrowserEventType.Foreground);
					browser.InvokeEvent(Enums.SysBrowserEventType.Opened);
				} else {
					throw new Exception("Browser Load Failed");
				}
			} catch (Exception e) {
				browser?.InvokeEvent(Enums.SysBrowserEventType.Error);
				Toaster.Error(e.Message);
				if(e is InvalidDataException or TimeoutException) {
					_ = Instances.TryRemove(options, out _);
					_ = (OpenTaskCompletionSource?.TrySetResult(null));
					_ = (browser?.LoadedTCS.TrySetResult(false));
					return null;
				}
			} finally {
				_ = Interlocked.Exchange(ref _isBusy, 0);
			}
		} else {
			if (browser.Brocess?.HasExited == true) {
				browser.Close();
				await Task.Delay(250);
				_ = Open(options);
			} else {
				browser.InvokeEvent(Enums.SysBrowserEventType.Foreground);
			}
		}

		_ = (OpenTaskCompletionSource?.TrySetResult(browser));
		return browser;
	}

	private async void Browser_OnEvent(object sender, SysBrowserEvent args)
	{
		switch (args.EventType) {
			case Enums.SysBrowserEventType.Closed:
				do {
					if (Instances.TryGetValue(args.OpenOptions, out var browser)) {
						_ = await browser.LoadedTCS.Task;
						_ = Instances.TryRemove(args.OpenOptions, out _);
						break;
					}

					await Task.Delay(250);
				}
				while (IsBusy);
				break;

				default:
					break;
		}
	}
}

