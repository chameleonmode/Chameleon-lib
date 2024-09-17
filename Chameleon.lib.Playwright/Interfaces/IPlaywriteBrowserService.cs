using System.Threading;
using System.Threading.Tasks;

using Chameleon.lib.Common.Interfaces;

using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IPlaywriteBrowserService
				: ISingletonDependency {
	IPlaywright? Playwright { get; set; }
	Task RunScript(IPlaywriteRunScriptOptions options, CancellationToken token);
}
