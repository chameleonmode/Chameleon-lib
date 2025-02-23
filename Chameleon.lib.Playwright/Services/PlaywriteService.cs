using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts;
using Chameleon.lib.Common.Constants;

namespace Chameleon.lib.Playwright.Services;
public class PlaywriteRunner {
	public static IPlaywrightBrowser Get(Enums.SystemBrowserType browserType) => browserType switch {
		Enums.SystemBrowserType.Chrome or
		Enums.SystemBrowserType.Chromium or
		Enums.SystemBrowserType.Brave => IoC.GetService<IChromeiumPlaywrightBrowser>() as IPlaywrightBrowser ?? throw new ArgumentNullException(nameof(browserType)),
		Enums.SystemBrowserType.Unknown => throw new NotImplementedException(),
		Enums.SystemBrowserType.Firefox => throw new NotImplementedException(),
		_ => throw new NotImplementedException(),
	};

	public static async Task RunScript(PlaywriteRunScriptOptions options, CancellationToken token)
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
				} 
			}
		} finally {
			if (browser != null)
				await browser.Close();
		}
	}

	public static void Dispose()
	{
		Get(Enums.SystemBrowserType.Chromium).Dispose();
	}
}

