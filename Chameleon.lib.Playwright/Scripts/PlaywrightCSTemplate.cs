using System.Collections.Generic;
using System.Threading.Tasks;

using Chameleon.lib.Playwright.Interfaces;

using Microsoft.Playwright;

public class ExternalScript : IExternalScript {
	public async Task Run(IBrowserContext context, IDictionary<string, string>? pargs = null)
	{
		// use to run script in a new tab 
		// IPage page = await context.NewPageAsync(); 
		// use to run script in the first tab 
		var page = context.Pages[0];

		// __________paste the recorded content under here____________________
		// For Example: 
		// Go to Google
		_ = await page.GotoAsync("https://www.google.com");
		await Task.Delay(1000);
		// End of example
		// __________paste the recorded content above here____________________

		// use this anywhere in the script to pause the script 
		// await page.PauseAsync();
		// use this anywhere in the script to add delay
		// await Task.Delay(1000); 
	}
}