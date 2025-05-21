using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Interfaces;

public interface IPlaywrightBrowserInstance : IDisposable {
	IBrowserContext BrowserContext { get; }
}

public interface IPlaywrightBrowser : IDisposable {
	IList<IPlaywrightBrowserInstance> RunningAutomationBrowsers { get; }
	Task<IPlaywrightBrowserInstance> Open(RunScriptOptions options);
}

