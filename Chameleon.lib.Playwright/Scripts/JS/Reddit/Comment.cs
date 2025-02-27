using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.node;

namespace Chameleon.lib.Playwright.Scripts.JS.Reddit;
public class Comment : IBundledJSScript
{
	public string TableName => "Reddit_" + nameof(Comment);
	public string File => "reddit/comment.plugin";
	public string Title => "Reddit Search And Comment";
	public string Description => "Search for reddit thread comment vote and reply";
	public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>() {
		{ "search" , "Search" }
	};

	public async Task<IDictionary<string, string>?> GetOptions(IDictionary<string, string>? options = null)
	{
		var res = await Plair.Instance.Ask(new(
			"reddit",
				new
				{
					keyword = options!["search"],
				}
			)
		);
		options.Add("comment", res!.Payload.Response);
		return options;
	}
}