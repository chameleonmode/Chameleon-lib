using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts;
using Chameleon.lib.Common;
using Chameleon.lib.Common.Enums;
using Chameleon.lib.Common.Interfaces;

using Microsoft.Playwright;
using System.Linq;
using System.IO;
using Chameleon.lib.Playwright.node;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;

namespace Chameleon.lib.Playwright.Services;
public class PlaywriteService(ICompileScriptService compileScriptService)
	: IPlaywriteService {

	public static IPlaywrightBrowser Get(SystemBrowserType browserType) => browserType switch {
		SystemBrowserType.Chrome or
		SystemBrowserType.Chromium or
		SystemBrowserType.Brave => IoC.GetService<IChromeiumPlaywrightBrowser>() as IPlaywrightBrowser ?? throw new ArgumentNullException(nameof(browserType)),
		SystemBrowserType.Unknown => throw new NotImplementedException(),
		SystemBrowserType.Firefox => throw new NotImplementedException(),
		_ => throw new NotImplementedException(),
	};

	// ... other code ...

	public async Task RunScript(PlaywriteRunScriptOptions options, CancellationToken token)
	{
		IPlaywrightBrowser? browser = null;
		try {
			if (options.Record) {
				await new RecordScript().Run(options.Port).WaitAsync(token);
			} else {
				var parameters = options.Description!.Parameters
						.Where(p => p.Key != null && p.Value != null)
						.ToDictionary(p => p.Key!, p => p.Value!);

				if (options.BundledJSScript != null) {
					await options.BundledJSScript.Run(options.Port, parameters).WaitAsync(token);
				} else {
					browser = Get(options.BrowserType);
					var browserInstance = await browser.Open(options);

					if (options.BundledCSScript != null) {
						await options.BundledCSScript.Run(browserInstance.BrowserContext, parameters).WaitAsync(token);
					} else if (options.BundledJSScript != null) {
						await options.BundledJSScript.Run(options.Port, parameters).WaitAsync(token);
					} else if (options.BundledCSScript == null && options.Description!.FilePath != null) {
						var scripBody = await File.ReadAllTextAsync(options.Description!.FilePath, token);
						var instance = await compileScriptService.CompileScript(scripBody);
						await instance.Run(browserInstance.BrowserContext, parameters).WaitAsync(token);
					}
				}
			}
		} finally {
			if (browser != null)
				await browser.Close();
		}
	}

	public void Dispose()
	{
		Get(SystemBrowserType.Chromium).Dispose();
	}
}

