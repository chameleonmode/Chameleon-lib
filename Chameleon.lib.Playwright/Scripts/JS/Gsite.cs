using Chameleon.lib.Playwright.Interfaces;

namespace Chameleon.lib.Playwright.Scripts.JS;
public class Gsites : IBundledJSScript
{
	public string TableName => "Google_" + nameof(Gsites);
	public string File => "google/plugins/gsites";
	public string Title => "Google Site Creator";
	public string Description => "Chreate a google site";
	public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>() {
		{ "name" , "Site Name" },
		{ "title" , "Title" },
		{ "content" , "Content" },
		{ "youtube" , "Youtube Search" },
		//{ "link", "Link" },
		//{ "linkText", "Link Text" },
		//{ "locationSearch" , "Location Search" }
	};

	public Task<IDictionary<string, string>?> GetOptions(IDictionary<string, string>? options = null)
	{
		return Task.FromResult(options);
	}
}
