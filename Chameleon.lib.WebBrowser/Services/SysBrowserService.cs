using System.Collections.Concurrent;
using Chameleon.lib.Common;
using Chameleon.lib.Common.Enums;
using Chameleon.lib.Common.Managers;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.WebBrowser.Models;
using Chameleon.lib.WebBrowser.System.Brave;
using Chameleon.lib.WebBrowser.System.Chrome;
using Chameleon.lib.WebBrowser.System.Firefox;

namespace Chameleon.lib.WebBrowser.Services;
public class SysBrowserService
	: ISysBrowserService {
	public static ISysBrowserInstance Create(SystemBrowserType browserType, SysBrowserLaunchOptions launchOptions) => browserType switch {
		SystemBrowserType.Brave => new BraveSysBrowserInstance() { Options = launchOptions },
		SystemBrowserType.Chrome => new ChromeSysBrowserInstance() { Options = launchOptions },
		SystemBrowserType.Firefox => new FirefoxSysBrowserInstance() { Options = launchOptions },
		_ => throw new NotImplementedException(),
	};

	private readonly WindowEventHandler? windowEventHandler;

	public ConcurrentDictionary<int, ISysBrowserInstance> Instances { get; } = [];

	private long _isBusy;
	public bool IsBusy => Interlocked.Read(ref _isBusy) > 0;

	public SysBrowserService()
	{
		if (OperatingSystem.IsWindows()) {
			windowEventHandler = new WindowEventHandler();
			windowEventHandler.OnForeground += U32til_OnForeground;
			windowEventHandler.OnDestroy += U32til_OnClose;
			windowEventHandler.StartListening();
		}
	}

	private void U32til_OnClose(nint obj)
	{
		for (var i = Instances.Count - 1; i >= 0; i--) {
			var uid = Instances.Keys.ElementAt(i);
			if (Instances.TryGetValue(uid, out var browser) && browser.Brocess?.HasExited == true) 				
				browser.Dispose();
		}
	}

	private async void U32til_OnForeground(nint obj)
	{
		for (var i = Instances.Count - 1; i >= 0; i--) {
			var uid = Instances.Keys.ElementAt(i);
			if (Instances.TryGetValue(uid, out var browser)) {
				_ = await browser.LoadedTCS.Task;

				if (browser.Brocess?.HasExited != true && browser.Brocess?.MainWindowHandle == obj) {
					//EventAggregator.Pub<ForegroundUserSystemBrowserEvent>(browser.GetArgs);
				}
			}
		}
	}

	public async Task<ISysBrowserInstance?> Open(SysBrowserOpenOptions options)
	{
		if (!Instances.TryGetValue(options.Profile.Id, out var browser)) {
			_ = await TaskUtil.AwaitFor(() => !IsBusy, 18, 256);
			_ = Interlocked.Increment(ref _isBusy);
			try {
				var emulations = IoC.GetValue<EmulationOptions>(nameof(EmulationOptions)) ?? new EmulationOptions {
					DisableWebRTC = true,
					SpoofClientRects = true,
					SpoofFontFingerprint = true,
					SpoofCanvasFingerprint = true,
					SpoofWebGLFingerprint = true,
					SpoofGeoLocation = true,
					AutoTimezone = true,
					DissableHyperlinkAuditing = true,
				};
				var urls = IoC.GetValue<string[]>("DefaultHomePageSettings") ?? ["duckduckgo.com"];
				var starturl = urls[new Random().Next(urls.Length)];
					starturl = starturl.Contains(Consts.Http.UrlSchemeEnd) ?
					starturl : $"{Consts.Http.HttpsScheme}{starturl}";
				var launchOptions = new SysBrowserLaunchOptions(options, emulations, starturl, Netil.NextFreePort(9613));
				browser = Create(options.BrowserType, launchOptions);
				browser.OnProcessClosed += Browser_OnProcessClosed;
				browser.OnBecameForeground += Browser_OnBecameForeground;
				Instances[options.Profile.Id] = browser;

				_ = browser.InitializeAsync(options);

				if (await browser.LoadedTCS.Task) {
					//var args = browser.GetArgs;
					//EventAggregator.Pub<ForegroundUserSystemBrowserEvent>(args);
					//EventAggregator.Pub<OpenedUserSystemBrowserEvent>(args);
				}
			} catch (Exception e) {
				Toaster.ShowErr(e.Message);
			} finally {
				_ = Interlocked.Exchange(ref _isBusy, 0);
			}
		} else {
			if (browser.Brocess?.HasExited == true) {
				browser.Dispose();
				await Task.Delay(250);
				_ = Open(options);
			} else {
				browser.MakeForeground();
			}
		}

		return browser;
	}

	private void Browser_OnBecameForeground(object? sender, SysBrowserLaunchOptions e) 
	{

	}
	private async void Browser_OnProcessClosed(object? sender, SysBrowserLaunchOptions o)
	{
		do {
			if (Instances.TryGetValue(o.Profile.Id, out var browser)) {
				_ = await browser.LoadedTCS.Task;

				//EventAggregator
				//	 .GetEvent<ClosedUserSystemBrowserEvent>()
				//	 .Publish(browser.GetArgs);

				_ = Instances.TryRemove(o.Profile.Id, out _);

				break;
			}

			await Task.Delay(250);
		}
		while (IsBusy);
	}
}

