using Chameleon.lib.Playwright.Interfaces;

namespace Chameleon.lib.Playwright.Scripts.JS.Reddit;
public class Comment : IBundledJSScript
{
	public string TableName => "Reddit_" + nameof(Comment);
	public string File => "reddit/plugins/comment";
	public string Title => "Reddit Search And Comment";
	public string Description => "Search for reddit thread comment vote and reply";
	public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>() {
		{ "search" , "Search" }
	};

	public Task<IDictionary<string, string>?> GetOptions(IDictionary<string, string>? options = null)
	{
		return Task.FromResult(options);
	}
}