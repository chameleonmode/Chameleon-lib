using Chameleon.lib.Core.Automation.Interfaces;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IPlaywrightScriptRepository {
	IList<IBundledScript> BundledScripts { get; }
	Task<List<IAutomationScriptDescription>> GetAll(string filepath);
}
