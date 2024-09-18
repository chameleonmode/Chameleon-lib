using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IBundledScript {
	string Title { get; }
	string Description { get; }
	IList<string> Parameters { get; }
}

public interface IBundledCSScript : IBundledScript {
	Task Run(IBrowserContext browserContext, IDictionary<string, string>? args = null);
}

public interface IBundledJSScript : IBundledScript {
	string Name { get; }
	Task Run(int port, IDictionary<string, string>? args = null);
}
