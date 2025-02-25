using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.node;

namespace Chameleon.lib.Playwright.Scripts.JS;
public class RecordScript : IBundledJSScript {
	public string Name => "record";
	public string Title => "Record";
	public string Description => "Record";
	public IDictionary<string, string> Parameters => new Dictionary<string, string>();

	public async Task Run(int port, IDictionary<string, string>? args = null) {
		var runner = PlaywrightTestRunner.Create(Name);
		await runner.RunTestAsync(port, args);
	}
}
