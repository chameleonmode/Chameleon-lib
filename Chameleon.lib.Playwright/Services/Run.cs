using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser;
using System.Diagnostics;
using Chameleon.lib.AIR.Scripts;

namespace Chameleon.lib.Playwright.Services;

public class Arguments {
  public int Port { get; set; }
  public bool Record { get; set; } = false;
  public SystemBrowserType BrowserType { get; set; } = SystemBrowserType.Chromium;
  public IScript? Script { get; set; }
  public object? Opts { get; set; }
  public ScriptDescription? Description { get; set; }
}

public class Run {
  public static async Task Script(Arguments args, CancellationToken token = default) {
		Debug.WriteLine($"Running: \n\t '{args.Port}', '{args.Script?.Title}', {JSON.Serialize(args)}");
    if (args.Record) {
      using var runner = new Runner();
      await runner.Run(args.Port, "../scripts/any/record").WaitAsync(token);
    } else {
      var savedOptions = args.Script?.TableName == null
        ? null
        : IoC.GetJsonValue<Dictionary<string, string>>(args.Script.TableName) ?? args.Description?.Parameters;
      if (args.Script is IJSScript jsScript) {
        using var runner = new Runner();
        var opts = args.Opts ?? await jsScript.GetOptions(savedOptions);
        await runner
          .Run(args.Port, jsScript.File, opts)
          .WaitAsync(token);
      } else if (args.Script is IBundledCSScript csScript) {
        using var browser = args.BrowserType switch {
          SystemBrowserType.Chrome or
          SystemBrowserType.Chromium or
          SystemBrowserType.Brave => new ChromeiumPlaywrightBrowser(),
          SystemBrowserType.Unknown => throw new NotImplementedException(),
          SystemBrowserType.Firefox => throw new NotImplementedException(),
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