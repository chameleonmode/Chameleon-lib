using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Playwright.node;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Playwright.Scripts.JS;

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

  public static async Task RunScript(RunScriptOptions options, CancellationToken token = default) {
    if (options.Record) {
      await new RecordScript().Run(options.Port).WaitAsync(token);
    } else {
      if (options.BundledScript is IBundledJSScript jsScript) {
        await jsScript.Run(options.Port, options.Description?.Parameters).WaitAsync(token);
      } else if (options.BundledScript is IBundledCSScript csScript) {
        using var browser = Get(options.BrowserType);
        using var context = await browser.Open(options);
        await csScript.Run(context.BrowserContext, options.Description?.Parameters).WaitAsync(token);
      } else if (options.Description?.FilePath != null) {
        var runner = PlaywrightTestRunner.Create(options.Description.FilePath);
        await runner.RunTestAsync(options.Port, options.Description?.Parameters).WaitAsync(token);
      } else {
        throw new NotImplementedException();
      }
    }
  }

  public static void Dispose() {
    Get(Enums.SystemBrowserType.Chromium).Dispose();
  }
}