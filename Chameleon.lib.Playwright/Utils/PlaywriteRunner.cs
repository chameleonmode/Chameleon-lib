using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Playwright.node;
using Chameleon.lib.Playwright.Services;

namespace Chameleon.lib.Playwright.Utils;
public class PlaywriteRunner {
  public static IPlaywrightBrowser Get(Enums.SystemBrowserType browserType) => browserType switch {
    Enums.SystemBrowserType.Chrome or
    Enums.SystemBrowserType.Chromium or
    Enums.SystemBrowserType.Brave => new ChromeiumPlaywrightBrowser(),
    Enums.SystemBrowserType.Unknown => throw new NotImplementedException(),
    Enums.SystemBrowserType.Firefox => throw new NotImplementedException(),
    _ => throw new NotImplementedException(),
  };

  public static async Task RunScript(PlaywriteRunScriptOptions options, CancellationToken token = default) {
    IPlaywrightBrowser? browser = null;
    try {
      if (options.Record) {
				await new RecordScript().Run(options.Port).WaitAsync(token);
			} else {
        var parameters = options.Description?.Parameters
            .ToDictionary(p => p.Key!, p => p.Value!);

        if (options.BundledJSScript != null) {
					await options.BundledJSScript.Run(options.Port, parameters).WaitAsync(token);
				} else if (options.Description?.FilePath != null) {
          var runner = PlaywrightTestRunner.Create(options.Description.FilePath);
          await runner.RunTestAsync(parameters, options.Port).WaitAsync(token);
        } else {
          throw new ArgumentNullException(nameof(options));
        }
      }
    } finally {
      if (browser != null)
        await browser.Close();
    }
  }

  public static void Dispose() {
    Get(Enums.SystemBrowserType.Chromium).Dispose();
  }
}