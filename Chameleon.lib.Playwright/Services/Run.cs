using Chameleon.lib.Common.Constants;
using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Util;
using Chameleon.lib.AIR.Scripts.Models;

namespace Chameleon.lib.Playwright.Services;

public class Arguments {
  public int Port { get; set; }
  public bool Record { get; set; } = false;
  public Enums.SystemBrowserType BrowserType { get; set; } = Enums.SystemBrowserType.Chromium;
  public IScript? Script { get; set; }
  public object? Opts { get; set; }
  public ScriptDescription? Description { get; set; }
}

public class Run {
  public static async Task Script(Arguments args, CancellationToken token = default) {
    if (args.Record) {
      using var runner = new Runner();
      await runner.Run(args.Port, "any/record").WaitAsync(token);
    } else {
      var savedOptions = args.Script?.TableName == null
        ? null
        : IoC.GetJsonValue<Dictionary<string, string>>(args.Script.TableName) ?? args.Description?.Parameters;
      if (args.Script is IJSScript jsScript) {
        using var runner = new Runner(async (question) => {
          if (!question.IsNot()) throw new ArgumentNullException(nameof(question));

          var res = await Service.Routes.Air.Ask(new("reddit", new {
            keyword = question,
          }));
          return res!.Payload.Response;
        });
        var opts = args.Opts ?? await jsScript.GetOptions(savedOptions);
        await runner
          .Run(args.Port, jsScript.File, opts)
          .WaitAsync(token);
      } else if (args.Script is IBundledCSScript csScript) {
        using var browser = args.BrowserType switch {
          Enums.SystemBrowserType.Chrome or
          Enums.SystemBrowserType.Chromium or
          Enums.SystemBrowserType.Brave => new ChromeiumPlaywrightBrowser(),
          Enums.SystemBrowserType.Unknown => throw new NotImplementedException(),
          Enums.SystemBrowserType.Firefox => throw new NotImplementedException(),
          _ => throw new NotImplementedException()
        };
        using var context = await browser.Open(args);
        await csScript
          .Run(context.BrowserContext, savedOptions)
          .WaitAsync(token);
      } else if(args.Description?.FilePath != null) {
        using var runner = new Runner();
        await runner
          .Run(args.Port, args.Description.FilePath, args.Description?.Parameters)
          .WaitAsync(token);
      } else {
        throw new NotImplementedException();
      }
    }
  }
}