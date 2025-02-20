using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.node;

namespace Chameleon.lib.Playwright.Scripts;
public class GsiteJsScript : IBundledJSScript {
	public string Title => "Google Site Creator";
	public string Description => "Chreate a google site";
	public string Name => "gsites";
	public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>() {
		{ "gsiteTitle" , "Google Site Title" },
		{ "publishTitle" , "Publish Title" },
		{ "postTitle" , "Post Title" },
		{ "textContent" , "Post Content" },
		{ "link", "HyperLink Link" },
		{ "textWithLink", "HyperLink Text" },
		{ "textSearch" , "Youtube KW Search" },
		{ "location" , "Post Location Pin" },
		{ "email" , "Email" },
		{ "password" , "Password" },
	};

	public async Task Run(int port, IDictionary<string, string>? args = null) {
		ArgumentNullException.ThrowIfNull(args, nameof(args));

		var data = new {
			url = "https://sites.google.com/new",
			email = args["email"] ?? Parameters["email"],
			password = args["password"] ?? Parameters["password"],
			textContent = args["textContent"],
			textSearch = args["textSearch"],
			location = args["location"],
			postTitle = args["postTitle"],
			publishTitle = args["publishTitle"],
			gsiteTitle = args["gsiteTitle"],
			link = args["link"],
			textWithLink = args["textWithLink"],
		};

		using var runner = PlaywrightTestRunner.Create(Name);
		await runner.RunTestAsync(data, port);
	}
}
