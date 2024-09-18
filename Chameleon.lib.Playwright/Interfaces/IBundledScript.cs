using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IBundledScript {
	string Title { get; }
	string Description { get; }
	IList<string> parameters { get; }
	Task Run(IBrowserContext browserContext, IDictionary<string, string>? args = null);
}

public interface IBundledJSScript {
	string Title { get; }
	string Description { get; }
	string Name { get; }
	IList<string> parameters { get; }
	Task Run(int port, IDictionary<string, string>? args = null);
}
