using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IExternalScript {
	Task Run(IBrowserContext browserContext, IDictionary<string, string>? pargs = null);
}
