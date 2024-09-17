using Chameleon.lib.Common.Enums;
using Chameleon.lib.Core.Automation.Interfaces;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IPlaywriteRunScriptOptions {
	int Port { get; set; }
	bool Record { get; set; }
	SystemBrowserType BrowserType { get; set; }
	IAutomationScriptDescription? Script { get; set; }
	IBundledScript? BundledScript { get; set; }
}
