using Chameleon.lib.Common.Constants;
using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.WebBrowser.Models;
using Chameleon.lib.WebBrowser.Services;
using Microsoft.Playwright;

namespace Tests.HtmlProcessingPipeline;
public class Base : TestSetup, IAsyncLifetime {
	internal IPlaywright? playwright;
	internal IBrowser? headlessBrowser;
	internal IBrowser? browser;
	internal IBrowserInstance? browserInstance;
	internal int Port => browserInstance?.Settings.Profile.Port ?? 0;

	// Proxy credentials
	// SGP6J3fr
	// CYpEvUqY
	// vUp6cZAY
	// mzBorsdy
	// N2Vb4Jvy
	public virtual async Task InitializeAsync() {
		browserInstance = await SystemBrowserService.Instance.OpenWithSettings(
			new SysBrowserSettings(new SysBrowserOpenOptions(Enums.SystemBrowserType.Chrome, new BrowserProfile {
				Id = 28296,
				Proxy = new BrowserProxy() {
					Host = "proxy.chameleonmode.com",
					Port = 31112,
					UserName = "elimdadia_gmail_com",
					Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-vUp6cZAY"
				}
			}))
		);
		playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		browser = await playwright.Chromium.ConnectOverCDPAsync($"http://localhost:{Port}");
		headlessBrowser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
			Headless = true,
			ExecutablePath = SysBrowserInfoUtil.Find(Enums.SystemBrowserType.Chrome).Path,
		});
	}

	public virtual async Task DisposeAsync() {
		if (headlessBrowser is not null) await headlessBrowser.DisposeAsync();
		playwright?.Dispose();
	}
}
