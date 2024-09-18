using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IBundledScript {
	string Title { get; }
	string Description { get; }
	IList<string> parameters { get; }
	Task Run(IBrowserContext browserContext, IDictionary<string, string>? args = null);
}
