using Microsoft.Playwright;
using Chameleon.lib.Playwright.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Chameleon.lib.Playwright.Scripts;
public class KeepGmailAlive : IBundledCSScript {
	public string Title => "Keep Gmail Alive";
	public string Description => "Reads a random email in Gmail.";
	public IList<string> Parameters => [];
	public async Task Run(IBrowserContext context, IDictionary<string, string>? args = null)
	{
		ArgumentNullException.ThrowIfNull(args, nameof(args));
		var page = await context.NewPageAsync();
		try {
			// Navigate to Gmail
			_ = await page.GotoAsync("https://mail.google.com/");
			await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

			try {
				// Wait for the element to be visible
				_ = await page.WaitForSelectorAsync("//*[@id='gb']/div[2]/div[2]", new PageWaitForSelectorOptions {
					Timeout = 10000, // Timeout in milliseconds (10 seconds)
					State = WaitForSelectorState.Visible // Wait until the element is visible
				});
			} catch (TimeoutException) {
				throw new Exception("User is not logged in to Gmail.");
			}

			_ = await page.WaitForSelectorAsync("tr.zA");
			// Get all email rows
			var emailRows = await page.QuerySelectorAllAsync("tr.zA");
			if (emailRows.Count <= 0) {
				throw new Exception("No emails found.");
			}

			// Select a random email and click it
			var randomIndex = new Random().Next(emailRows.Count);
			await emailRows[randomIndex].ClickAsync();

			// Wait for a few seconds to simulate reading the email
			await Task.Delay(5000);
		} catch (Exception ex) {
			// Event handler for dialogs
			page.Dialog += async (_, dialog) => {
				Console.WriteLine($"Dialog message: {dialog.Message}");
				await dialog.DismissAsync();
			};
			_ = await page.EvaluateAsync($"(() => {{ alert('{ex.Message}'); }})();");
			await Task.Delay(-1);
		}
	}
}