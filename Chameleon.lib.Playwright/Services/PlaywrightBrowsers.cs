using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;

using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Services;
public class ChromeiumPlaywrightBrowserInstance(IBrowser browser)
		: IPlaywrightBrowserInstance {
	public IBrowserContext BrowserContext => browser.Contexts[0];

	public async Task Close() {
		if (BrowserContext != null) await BrowserContext.CloseAsync();

		if (browser != null) {
			await browser.CloseAsync();
			await browser.DisposeAsync();
		}
	}
}

public class ChromeiumPlaywrightBrowser : IPlaywrightBrowser {
	public IPlaywright? Playwright { get; set; }
	public IList<IPlaywrightBrowserInstance> RunningAutomationBrowsers { get; } = [];

	public async Task Close() {
		foreach (var browser in RunningAutomationBrowsers) {
			await browser.Close();
		}
		RunningAutomationBrowsers.Clear();
	}
	public void Dispose() {
		Playwright?.Dispose();
		Playwright = null;
	}

	public virtual async Task<IPlaywrightBrowserInstance> Open(PlaywriteRunScriptOptions o) {
		Playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();

		var iBrowser = await TryOpenByCDP(0, o.Port);
		var browser = new ChromeiumPlaywrightBrowserInstance(iBrowser);
		RunningAutomationBrowsers.Add(browser);

		return browser;
	}
	private async Task<IBrowser> TryOpenByCDP(int trys, int port) {
		ArgumentNullException.ThrowIfNull(Playwright);

		try {
			var browser = await Playwright.Chromium.ConnectOverCDPAsync($"http://localhost:{port}");

			return browser;
		} catch {
			if (trys < 6) {
				await Task.Delay(1000);
				return await TryOpenByCDP(trys + 1, port);
			} else {
				throw;
			}
		}
	}
}