using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IBundledScript {
	string File { get; }
	string TableName { get; }
	string Title { get; }
	string Description { get; }
	IDictionary<string, string> Parameters { get; }
}
public interface IBundledCSScript : IBundledScript {
	Task Run(IBrowserContext browserContext, IDictionary<string, string>? options = null);
}

public interface IBundledJSScript : IBundledScript {
	Task Run(int port, IDictionary<string, string>? options = null);
}