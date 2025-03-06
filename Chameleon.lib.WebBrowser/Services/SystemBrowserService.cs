using System.Collections.Concurrent;

using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Interfaces.Sys;
using Chameleon.lib.Common.Models;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.Const;
using Chameleon.lib.Helpers;
using Chameleon.lib.Util;
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
	public int TimeOut { get; } = 26;
	public static ISysBrowserInstance Create(SysBrowserSettings launchOptions) => launchOptions.BrowserType switch {
		SystemBrowserType.Brave => new BraveSysBrowserInstance() { Settings = launchOptions },
		SystemBrowserType.Chrome => new ChromeSysBrowserInstance() { Settings = launchOptions },
		SystemBrowserType.Firefox => new FirefoxSysBrowserInstance() { Settings = launchOptions },
		_ => throw new NotImplementedException(),
	};

	private readonly WindowEventHandler? windowEventHandler;

	public ConcurrentDictionary<SysBrowserOpenOptions, ISysBrowserInstance> Instances { get; } = [];

	private long _isBusy;
	public bool IsBusy => Interlocked.Read(ref _isBusy) > 0;

	public TaskCompletionSource<ISysBrowserInstance?>? OpenTaskCompletionSource { get; private set; }

	#region Hwnd
	private async void MacOS_WindowForegroundChanged(int obj) {
		for (var i = Instances.Count - 1; i >= 0; i--) {
			var uid = Instances.Keys.ElementAt(i);
			if (Instances.TryGetValue(uid, out var browser)) {
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
				if (Instances.TryGetValue(uid, out var browser) && browser.Brocess?.HasExited == true) {
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
				if (Instances.TryGetValue(uid, out var browser)) {
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

	public async Task<ISysBrowserInstance?> OpenWithSettings(SysBrowserSettings launchSettings) {
		var browser = Create(launchSettings);
		browser.OnEvent += Browser_OnEvent;
		Instances[launchSettings.OpenOptions] = browser;
		var initTask = browser.InitializeAsync();
		if (await browser.LoadedTCS.Task.WaitAsync(TimeSpan.FromSeconds(TimeOut))) {
			browser.InvokeEvent(SysBrowserEventType.Foreground);
			browser.InvokeEvent(SysBrowserEventType.Opened);
		} else {
			throw new Exception("Browser Load Failed");
		}

		return browser;
	}
	public async Task<ISysBrowserInstance?> Open(SysBrowserOpenOptions options, Func<string> @startUrl, EmulationOptions? emulations = null) {
		if (!Instances.TryGetValue(options, out var browser)) {
			OpenTaskCompletionSource = new TaskCompletionSource<ISysBrowserInstance?>();
			try {
				if (options.BrowserType == SystemBrowserType.Firefox) {
					var systempath = SysBrowserInfoUtil.FindByType(SystemBrowserType.Firefox).Path;
					if (IOtil.IsNeedUpdate(systempath, Consts.Browser.LocalFirefoxExePath)) {
						Toaster.Info("Updating Firefox browser...");
						IOtil.DeleteDir(Path.Combine(FilePaths.AppDataLocalDir, "Foxameleon"));
						IOtil.DeleteDir(Path.Combine(FilePaths.AppDataLocalDir, "FirefoxChameleon"));
						IOtil.DeleteDir(Consts.Browser.LocalFirefoxDirPath);
						await IOtil.CopyFolderAsync(OperatingSystem.IsMacOS() ? "/Applications/firefox.app"
						: Path.GetDirectoryName(systempath)!, Consts.Browser.LocalFirefoxDirPath);
					}
				}
				browser = await OpenWithSettings(new(options, emulations ?? IoC.GetJsonValue<EmulationOptions>(nameof(EmulationOptions)) ?? new(), startUrl(), TcpUtil.NextFreePort(9613)));
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
			if (browser.Brocess?.HasExited == true) {
				browser.Close();
				await Task.Delay(250);
				_ = Open(options, @startUrl);
			} else {
				browser.InvokeEvent(SysBrowserEventType.Foreground);
			}
		}

		_ = (OpenTaskCompletionSource?.TrySetResult(browser));
		return browser;
	}

	private async void Browser_OnEvent(object sender, SysBrowserEvent args) {
		switch (args.EventType) {
			case SysBrowserEventType.Closed:
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

	// Singleton
	public static SystemBrowserService Instance { get;} = new();
}
