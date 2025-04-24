using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Playwright.node;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Util;
using Chameleon.AIR.Scripts.Models;

namespace Chameleon.lib.Playwright.Utils;
public class PlaywriteRunner {
  public static async Task RunScript(RunScriptOptions args, CancellationToken token = default) {
    if (args.Record) {
      using var runner = new PlaywrightTestRunner("any/record");
      await runner.RunTestAsync(args.Port).WaitAsync(token);
    } else {
      var savedOptions = args.BundledScript?.TableName == null
        ? null
        : IoC.GetJsonValue<Dictionary<string, string>>(args.BundledScript.TableName) ?? args.Description?.Parameters;

      if (args.BundledScript is IJSScript jsScript) {
        using var runner = new PlaywrightTestRunner(jsScript.File, async (question) => {
          if(!question.IsNot()) throw new ArgumentNullException(nameof(question));
          
          var res = await Service.Routes.Air.Ask(new("reddit", new {
            keyword = question,
          }));
          return res!.Payload.Response;
        });
        var options = args.Opts ?? await jsScript.GetOptions(savedOptions);
        await runner.RunTestAsync(
          args.Port, options
        ).WaitAsync(token);
      } else if (
          args.BundledScript is IBundledCSScript csScript
        ) {
        using var browser = args.BrowserType switch {
          Enums.SystemBrowserType.Chrome or
          Enums.SystemBrowserType.Chromium or
          Enums.SystemBrowserType.Brave => new ChromeiumPlaywrightBrowser(),
          Enums.SystemBrowserType.Unknown => throw new NotImplementedException(),
          Enums.SystemBrowserType.Firefox => throw new NotImplementedException(),
          _ => throw new NotImplementedException()
        };
        using var context = await browser.Open(args);
        await csScript.Run(context.BrowserContext, savedOptions).WaitAsync(token);
      } else if (
          args.Description?.FilePath != null
        ) {
        var runner = new PlaywrightTestRunner(args.Description.FilePath);
        await runner.RunTestAsync(args.Port, args.Description?.Parameters).WaitAsync(token);
      } else {
        throw new NotImplementedException();
      }
    }
  }
}