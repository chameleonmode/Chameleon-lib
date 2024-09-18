using Chameleon.lib.Common.Interfaces;
using Chameleon.lib.Playwright.Models;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IPlaywriteService
				: ISingletonDependency {
	Task RunScript(PlaywriteRunScriptOptions options, CancellationToken token);
	void Dispose();
	void Close();
}
