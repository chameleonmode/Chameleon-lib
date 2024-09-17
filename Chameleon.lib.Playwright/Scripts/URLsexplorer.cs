using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Core.Automation.Interfaces;

using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Scripts;
public class URLsexplorer : IBundledScript {
  public const string ProtocolDelimiter = "://";

  public async Task Run(IBrowserContext context, IList<IAutomationParameterValue>? pargs = null)
  {
		var args = pargs.ParseArguments();

		var urlsString = args["urls"];
		ArgumentException.ThrowIfNullOrEmpty(urlsString, "Argument <urls> is not valid");

    if (!int.TryParse(args["timeout"], out var timeout))
      throw new ArgumentException("Argument <timeout> is not valid");
    timeout *= 1000;

		var page = context.Pages[0];

		var urls = urlsString.Split(',');
    foreach (var url in urls)
    {
      if (string.IsNullOrWhiteSpace(url))
      {
        continue;
      }

      var link = url.Contains(ProtocolDelimiter)
          ? url
          : $"https://{url.Trim()}";
      try
      {
				_ = await page.GotoAsync(link); // Navigate to url
        await page.WaitForTimeoutAsync(timeout); // Wait for N seconds
      }
      catch
      {
        // go to next url ignoring errors
      }
    }
  }
}

