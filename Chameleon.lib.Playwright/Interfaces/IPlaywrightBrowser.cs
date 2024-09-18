using System.Collections.Generic;
using System.Threading.Tasks;

using Chameleon.lib.Common.Interfaces;
using Chameleon.lib.Playwright.Models;

using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Interfaces;

public interface IPlaywrightBrowserInstance {
	IBrowserContext BrowserContext { get; }
	Task Close();
}

public interface IPlaywrightBrowser : ISingletonDependency {
	IList<IPlaywrightBrowserInstance> RunningAutomationBrowsers { get; }
	Task<IPlaywrightBrowserInstance> Open(PlaywriteRunScriptOptions options);
	void Dispose();
	Task Close();
}

public interface IChromeiumPlaywrightBrowser
		: IPlaywrightBrowser {
}

