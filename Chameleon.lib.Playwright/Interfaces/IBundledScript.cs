using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IBundledScript {
	string Name { get; }
	//Display Title
	string Title { get; }
	//Display Description
	string Description { get; }
	IDictionary<string, string> Parameters { get; }
}

public interface IBundledCSScript : IBundledScript {
	Task Run(IBrowserContext browserContext, IDictionary<string, string>? args = null);
}

public interface IBundledJSScript : IBundledScript {
	//Script Name
	Task Run(int port, IDictionary<string, string>? args = null);
}
