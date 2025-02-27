using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;
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

  public static async Task RunScript(RunScriptOptions args, CancellationToken token = default) {
    if (args.Record) {
      using var runner = PlaywrightTestRunner.Create("record");
      await runner.RunTestAsync(args.Port).WaitAsync(token);
    } else {
      var savedOptions = args.BundledScript?.TableName == null ? null : IoC.GetJsonValue<Dictionary<string, string>>(args.BundledScript.TableName);
      if (args.BundledScript is IBundledJSScript jsScript) {
        var options = await jsScript.GetOptions(savedOptions).WaitAsync(token);
        using var runner = PlaywrightTestRunner.Create(jsScript.File);
        await runner.RunTestAsync(args.Port, options).WaitAsync(token);
      } else if (args.BundledScript is IBundledCSScript csScript) {
        using var browser = Get(args.BrowserType);
        using var context = await browser.Open(args);
        await csScript.Run(context.BrowserContext, savedOptions).WaitAsync(token);
      } else if (args.Description?.FilePath != null) {
        var runner = PlaywrightTestRunner.Create(args.Description.FilePath);
        await runner.RunTestAsync(args.Port, args.Description?.Parameters).WaitAsync(token);
      } else {
        throw new NotImplementedException();
      }
    }
  }

  public static void Dispose() {
    Get(Enums.SystemBrowserType.Chromium).Dispose();
  }
}