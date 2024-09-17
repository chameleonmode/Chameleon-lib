using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts;
using Chameleon.lib.Common;
using Chameleon.lib.Common.Enums;
using Chameleon.lib.Common.Interfaces;
using Chameleon.lib.Core.Automation.Interfaces;

using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Services;
public class PlaywriteBrowserService(ICompileScriptService compileScriptService, IAutomationService automationService)
	: IPlaywriteBrowserService {

	public IPlaywright? Playwright { get; set; }

	private List<IPlaywrightBrowserInstance> RunningAutomationBrowsers { get; } = [];
	public static IPlaywrightBrowser? Get(SystemBrowserType browserType) => browserType switch {
		SystemBrowserType.Chrome or
		SystemBrowserType.Chromium or
		SystemBrowserType.Brave => IoC.GetService<IChromeiumPlaywrightBrowser>() as IPlaywrightBrowser,
		SystemBrowserType.Unknown => throw new NotImplementedException(),
		SystemBrowserType.Firefox => throw new NotImplementedException(),
		_ => throw new NotImplementedException(),
	};

	private async Task<IPlaywrightBrowserInstance> GetBrowserInstance(IPlaywriteRunScriptOptions options)
	{
		Playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();
		var launchOptions = new PlaywrightBrowserLaunchOptions {
			ScriptOptions = options,
			Playwright = Playwright
		};
		var browser = Get(launchOptions.ScriptOptions?.BrowserType ?? SystemBrowserType.Chrome);
		ArgumentNullException.ThrowIfNull(browser);

		var browserInstance = await browser.Open(launchOptions);
		ArgumentNullException.ThrowIfNull(browserInstance);
		RunningAutomationBrowsers.Add(browserInstance);

		return browserInstance;
	}
	public async Task RunScript(IPlaywriteRunScriptOptions options, CancellationToken token)
	{
		try {
			var browserInstance = await GetBrowserInstance(options);
			ArgumentNullException.ThrowIfNull(browserInstance.BrowserContext);

			if (options.Record) {
				await new ExternalScript().Run(browserInstance.BrowserContext).WaitAsync(token);
			} else {
				if (options.BundledScript != null) {
					await options.BundledScript.Run(browserInstance.BrowserContext, options.Script?.Parameters).WaitAsync(token);
				} else if (options.BundledScript == null && options.Script?.FilePath != null) {
					var scripBody = await automationService.GetScriptBody(options.Script.FilePath);
					var instance = await compileScriptService.CompileScript(scripBody);
					ArgumentNullException.ThrowIfNull(instance);
					await instance.Run(browserInstance.BrowserContext, options.Script.Parameters.ParseArguments()).WaitAsync(token);
				}
				//TODO : else
				//	var scripBody =  automationService.GetScriptBody(options.Script.Id);
			}

			//TODO: move to IUserProfileActionsViewModel
			//foreach (IUserProfileActionsViewModel profile in userProfiles) {
			//	var browserWasNotOpened = profile.SBI == null;
			//	if (browserWasNotOpened) {
			//		await profile.OpenSystemBrowser(browserType).WaitAsync(token);
			//		if (!await profile.SBI.OPtcs.Task.WaitAsync(token))
			//			continue;
			//	}
			//	// Check if the browser process is not null and hasn't exited
			//	if (browserWasNotOpened &&
			//			profile.SBI != null &&
			//			profile.SBI.Brocess != null &&
			//			!profile.SBI.Brocess.HasExited) {
			//		try {
			//			await browserInstance.Close();
			//			// Attempt to close the browser gracefully
			//			profile.SBI.Brocess.CloseMainWindow();
			//			// Give the process some time to exit gracefully
			//			bool exitedGracefully = profile.SBI.Brocess.WaitForExit(2500); // Wait for 2.5 seconds
			//			if (!exitedGracefully) {
			//				// If the process hasn't exited within 5 seconds, kill it
			//				profile.SBI.Brocess.Kill();
			//				// Wait for the process to be killed
			//				profile.SBI.Brocess.WaitForExit();
			//			}
			//		} catch (Exception ex) {
			//			// Log or handle the exception if closing the process fails
			//			toastNotificationService.ShowError($"Failed to close the browser process: {ex.Message}");
			//		} finally {
			//			// Ensure the process is disposed
			//			//profile.SBI.Brocess.Dispose();
			//			//profile.SBI.Cleanup();
			//			RunningAutomationBrowsers.RemoveBrowser(browserInstance);
			//		}
			//	}
			//	// Stop loop if canceled
			//	if (token.IsCancellationRequested) {
			//		break;
			//	}
			//}
		} catch (Exception) {
			//TODO: toastNotificationService.ShowError(ex.Message);
			throw;
		} finally {
			// RiseFinishScriptExecutionEvent();
		}
	}
}

