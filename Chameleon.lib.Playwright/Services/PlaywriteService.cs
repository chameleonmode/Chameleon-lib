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

	public async Task RunScript(PlaywriteRunScriptOptions options, CancellationToken token)
	{
		IPlaywrightBrowser? browser = null;
		try {
			browser = Get(options.BrowserType);
			var browserInstance = await browser.Open(options);

			if (options.Record) {
				await new ExternalScript().Run(browserInstance.BrowserContext).WaitAsync(token);
			} else {
				if (options.BundledScript != null) {
					await options.BundledScript.Run(browserInstance.BrowserContext, options.Description!.Parameters).WaitAsync(token);
				} else if (options.BundledScript == null && options.Description!.FilePath != null) {
					var scripBody = await File.ReadAllTextAsync(options.Description!.FilePath, token);
					var instance = await compileScriptService.CompileScript(scripBody);
					await instance.Run(browserInstance.BrowserContext, options.Description!.Parameters).WaitAsync(token);
				}
				//TODO : else
				//	var scripBody =  automationService.GetScriptBody(options.Script.Id);
			}
		} finally {
			await browser!.Close();
		}
	}

	public void Dispose()
	{
		Get(SystemBrowserType.Chromium).Dispose();
	}
	public void Close()
	{
		Get(SystemBrowserType.Chromium).Close();
	}
}

