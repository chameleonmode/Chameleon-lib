using Chameleon.Interfaces.Ioc;

namespace Chameleon.lib.Playwright.Interfaces;
public interface ICompileScriptService
		: ISingletonDependency {
	Task<IExternalScript> CompileScript(string script);
}
