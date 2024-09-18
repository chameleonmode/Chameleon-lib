
using System.Threading;

using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.node;

using static System.Net.Mime.MediaTypeNames;

namespace Chameleon.lib.Playwright.Scripts;
public class GsiteJsScript : IBundledJSScript {
	public string Title => "Google Site Creator";
	public string Description => "Chreate a google site";
	public string Name => "gsites";
	public IList<string> parameters { get; } = ["url", "email", "password", "textContent", "textSearch", "location", "postTitle", "publishTitle", "gsiteTitle"];

	public async Task Run(int port, IDictionary<string, string>? args = null)
	{
		ArgumentNullException.ThrowIfNull(args, nameof(args));

		var data = new
		{
			url = args["url"],
			email = args["email"],
			password = args["password"],
			textContent = args["textContent"],
			textSearch = args["textSearch"],
			location = args["location"],
			postTitle = args["postTitle"],
			publishTitle = args["publishTitle"],
			gsiteTitle = args["gsiteTitle"]
		};

		using var runner = new PlaywrightTestRunner();
		try {
			TaskCompletionSource<bool> tcs = new();
			runner.TestOutputReceived += (sender, output) => {
				if (output == $"Test {Name} completed finally block") tcs.SetResult(true);
			};
			await runner.RunTestAsync(Name, data, port);
			_ = await tcs.Task;
		} finally {
			await Task.Delay(1000);
		}
	}
}
