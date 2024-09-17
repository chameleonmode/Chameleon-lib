using System.Threading.Tasks;

using Chameleon.lib.Common.Interfaces;
using Chameleon.lib.Core.Automation.Interfaces;
using Chameleon.lib.Playwright.Interfaces;

namespace Chameleon.lib.Playwright.Interfaces;
public interface ICompileScriptService
		: ISingletonDependency {
	Task<IExternalScript?> CompileScript(string script);
}
