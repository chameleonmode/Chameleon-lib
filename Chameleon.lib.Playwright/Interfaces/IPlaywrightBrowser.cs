using Chameleon.lib.Common.Interfaces.Systemics;
using Chameleon.lib.Playwright.Models;

using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Interfaces;

public interface IPlaywrightBrowserInstance {
	IBrowserContext BrowserContext { get; }
	Task Close();
}

public interface IPlaywrightBrowser : ISingletonDependency {
	IList<IPlaywrightBrowserInstance> RunningAutomationBrowsers { get; }
	Task<IPlaywrightBrowserInstance> Open(RunScriptOptions options);
	void Dispose();
	Task Close();
}

