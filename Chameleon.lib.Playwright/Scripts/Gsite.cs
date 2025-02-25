using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.node;

namespace Chameleon.lib.Playwright.Scripts;
public class Gsites : IBundledJSScript {
	public string Title => "Google Site Creator";
	public string Description => "Chreate a google site";
	public string Name => "gsites";
	public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>() {
		{ "name" , "Site Name" },
		{ "title" , "Title" },
		{ "content" , "Content" },
		{ "textContent" , "Post Content" },
		{ "link", "Link" },
		{ "linkText", "Link Text" },
		{ "youtubeSearch" , "Youtube Search" },
		{ "locationSearch" , "Location Search" }
	};

	public async Task Run(int port, IDictionary<string, string>? args = null) {
		using var runner = PlaywrightTestRunner.Create(Name);
		await runner.RunTestAsync(args, port);
	}
}
