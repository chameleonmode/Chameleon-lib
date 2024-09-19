
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.node;

using Microsoft.CodeAnalysis;

using static System.Net.Mime.MediaTypeNames;

namespace Chameleon.lib.Playwright.Scripts;
public class RecordScript : IBundledJSScript {
	public string Title => "Record";
	public string Description => "Chreate a google site";
	public string Name => "record";
	public IList<string> Parameters { get; } = ["url", "email", "password", "textContent", "textSearch", "location", "postTitle", "publishTitle", "gsiteTitle"];

	public async Task Run(int port, IDictionary<string, string>? args = null)
	{
	  var runner = PlaywrightTestRunner.Create(Name);
		await runner.RunTestAsync("{}", port);
	}
}
