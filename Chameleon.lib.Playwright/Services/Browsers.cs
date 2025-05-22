using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Services;
public class ChromeiumPlaywrightBrowserInstance(IBrowser browser) : IPlaywrightBrowserInstance {
	public IBrowserContext BrowserContext => browser.Contexts[0];

	public async void Dispose()
	{
		if (browser != null)
			await browser.DisposeAsync();
		GC.SuppressFinalize(this);
	}
}

public class ChromeiumPlaywrightBrowser : IPlaywrightBrowser {
	public IPlaywright? Playwright { get; set; }
	public IList<IPlaywrightBrowserInstance> RunningAutomationBrowsers { get; } = [];

	
	public void Dispose() {
		RunningAutomationBrowsers.Clear();
		Playwright?.Dispose();
		Playwright = null;
		GC.SuppressFinalize(this);
	}

	public virtual async Task<IPlaywrightBrowserInstance> Open(Arguments o) {
		Playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();
		RunningAutomationBrowsers.Add(new ChromeiumPlaywrightBrowserInstance(await TryOpenByCDP(o.Port)));
		return RunningAutomationBrowsers.Last();
	}
		private async Task<IBrowser> TryOpenByCDP(int port, int trys = 0) {
		try {
			return await Playwright!.Chromium.ConnectOverCDPAsync($"http://localhost:{port}");
		} catch {
			await Task.Delay(1000);
			return trys++ < 3
				? await TryOpenByCDP(trys++, port)
				: throw new InvalidOperationException("Failed to connect a browser context check your currently running browsers and try again");
		}
	}
}