using System.Threading.Tasks;

using Chameleon.lib.Common.Interfaces;

namespace Chameleon.lib.Playwright.Interfaces;
public interface ICompileScriptService
		: ISingletonDependency {
	Task<IExternalScript> CompileScript(string script);
}
