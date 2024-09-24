using Chameleon.lib.Common.Interfaces.Systemics;

namespace Chameleon.lib.Playwright.Interfaces;
public interface ICompileScriptService
		: ISingletonDependency {
	Task<IExternalScript> CompileScript(string script);
}
