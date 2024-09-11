using Chameleon.lib.Common.Enums;

namespace Chameleon.lib.Core.Automation.Interfaces;
public interface IAutomationRunScriptOptions {
		int Port { get; set; }
		bool Record { get; set; }
		SystemBrowserType BrowserType { get; set; }
		IAutomationScriptDescription Script { get; set; }
}
