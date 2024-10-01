
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.node;

namespace Chameleon.lib.Playwright.Scripts;
public class RecordScript : IBundledJSScript {
	public string Title => "Record";
	public string Description => "Record";
	public string Name => "record";
	public IDictionary<string, string> Parameters  => new Dictionary<string, string>();

	public async Task Run(int port, IDictionary<string, string>? args = null)
	{
	  var runner = PlaywrightTestRunner.Create(Name);
		await runner.RunTestAsync("{}", port);
	}
}
