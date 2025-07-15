using Chameleon.lib.WebBrowser;
using System.Diagnostics;
using Chameleon.lib.AIR.Scripts;
using Chameleon.lib.AIR.Actors;
using Chameleon.lib.Util;

namespace Chameleon.lib.Playwright.Services;

public class Arguments {
  public int Port { get; set; }
  public bool Record { get; set; } = false;
  public BrowserType BrowserType { get; set; } = BrowserType.Chrome;
  public IScript? Script { get; set; }
  public Opts? Opts { get; set; }
  public ScriptDescription? Description { get; set; }
}

public static class Run {
  public static async Task Script(Arguments args, CancellationToken cts = default) {
		Debug.WriteLine($"Running: \n\t '{args.Port}', '{args.Script?.Title}', {JSON.Serialize(args)}");
    if (args.Record) args.Script ??= new JSScript("../scripts/any/record", "Record Script", "Records the browser session for later playback");
    var opts = args.Script?.TableName is { } table ? IoC.GetJsonValue<Dictionary<string, string>>(table) : args.Description?.Parameters;
    if (args.Script is IJSScript jsScript) {
      using var runner = new Runner();
      await runner
         .Run(args.Port, jsScript.File, args.Opts is { } ? args.Opts : opts)
         .WaitAsync(cts);
    } else if (args.Script is IBundledCSScript csScript) {
      using var browser = Factorially.CreateBrowser(args.BrowserType);
      using var context = await browser.Open(args);
      await csScript
        .Run(context.BrowserContext, opts)
        .WaitAsync(cts);
    } else if(args.Description?.FilePath != null) {
      using var runner = new Runner();
      await runner
        .Run(args.Port, args.Description.FilePath, args.Description?.Parameters)
        .WaitAsync(cts);
    } else {
      throw new NotImplementedException();
    }
  }
}