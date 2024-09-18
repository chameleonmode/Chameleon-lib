using Chameleon.lib.Playwright.Models;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IPlaywrightScriptRepository {
	IDictionary<string, IBundledCSScript> BundledCSScripts { get; }
	IDictionary<string, IBundledJSScript> BundledJSScripts { get; }
	IList<PlaywriteRunScriptOptions> GetBundledScrits();
	Task<IList<PlaywriteRunScriptOptions>> GetUserScripts(string filepath);
	Task<IList<PlaywriteRunScriptOptions>> GetAll(string filepath);
}
