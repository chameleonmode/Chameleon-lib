using Chameleon.lib.Playwright.Models;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IPlaywrightScriptRepository {
	IDictionary<string, IBundledScript> BundledScripts { get; }
	IDictionary<string, IBundledJSScript> BundledJSScripts { get; }
	Task<IList<PlaywrightScriptDescription>> GetAll(string filepath);
}
