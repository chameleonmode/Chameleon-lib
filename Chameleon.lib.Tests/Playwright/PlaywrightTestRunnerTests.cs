using Chameleon.lib.Playwright.node;
using Chameleon.lib.Common;

using System.Diagnostics;
using Chameleon.lib.Common.Types;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace Chameleon.lib.Tests.Playwright;

public class PlaywrightTestRunnerTests : PlaywrightTestsBase, IDisposable {
	private PlaywrightTestRunner? runner;

	public PlaywrightTestRunnerTests() : base()
	{
	  void setup(bool init)
		{
			// Setup code
			Port = Netil.NextFreePort(Port);
			CachePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			BrowserProcess = GrowserProcess(CachePath, [$"--remote-debugging-port={Port}"]);
			_tcs.SetResult(true);
		}
		IoC.Instance.Configure(() => {
			return new WritableConfiguration(new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build(), Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
		}, (services) => {
			_ = services
			//app.Playwright
			.AddSingleton<ICompileScriptService, CompileScriptService>()
			.AddSingleton<IPlaywriteService, PlaywriteService>()
			.AddSingleton<IPlaywrightScriptRepository, PlaywrightScriptRepository>()
			.AddSingleton<IChromeiumPlaywrightBrowser, ChromeiumPlaywrightBrowser>();
		});
		// Setup IoC
		IoC.Instance.Init(action: setup);
	}

	private async Task RunTestsInParallelAsync(IEnumerable<(string testName, int port, object testData)> tests, int maxConcurrency = 3)
	{
		if (runner == null) throw new InvalidOperationException("Runner is not initialized.");

		var semaphore = new SemaphoreSlim(maxConcurrency);
		var tasks = tests.Select(async test => {
			await semaphore.WaitAsync();
			try {
				TaskCompletionSource<bool> tcs = new();
				runner.TestOutputReceived += (sender, output) => {
					if (output == $"Test {test.testName} completed finally block") tcs.SetResult(true);
				};
				await runner.RunTestAsync(test.testData, test.port);
				_ = await tcs.Task;
			} finally {
				await Task.Delay(1000);
				_ = semaphore.Release();
			}
		});

		await Task.WhenAll(tasks);
	}

	[Fact]
	public async Task TestGsite()
	{
		await LaunchBrowser();

		runner = PlaywrightTestRunner.Create("gsites");
		try {
			runner.TestOutputReceived += (sender, output) => Debug.WriteLine($"Test output: {output}");
			runner.TestErrorReceived += (sender, error) => Debug.WriteLine($"Test error: {error}");
			var data = new
			{
			urlGsite = "https://sites.google.com/",
			email = "testjosh11011900@gmail.com",
			password = "testjosh11011900@1234",
			textContent = "Anti-detect browser is capable of creating and running digital multiple identities that are not recognized by social platforms. This requires a lot of custom developer work, so such tools are generally not available for free. They are created to fight against tracking and analytics so that you can carry out your activities in private. In other words, running digital an anti-fingerprint browser enhances privacy, keeps your data and web activities anonymous, and helps your web crawling tools avoid being blocked",
			textSearch = "What is anti detect browser",
			location = "washington",
				publishTitle = "anti Detect Browser",
			gsiteTitle = "GsiteTitle",
			link = "www.google.com",
			textWithLink = "running digital multiple",
				postTitle = "Gsite",
			};
			await RunTestsInParallelAsync(new List<(string testName, int port, object testData)>() { new("gsites", Port, data) });
			//DisposeBrowser();
			//if (BrowserProcess != null)
			//	await BrowserProcess.WaitForExitAsync();
		} catch (Exception ex) {
			Debug.WriteLine($"Error running test: {ex.Message}");
			throw;
		} finally {
			runner.Dispose();
		}
	}

	[Fact]
	public async Task TestStartProcess()
	{
		if (BrowserProcess != null) {
			await LaunchBrowser();
			await BrowserProcess.WaitForExitAsync();
		}
	}

	public async void Dispose()
	{
		runner?.Dispose();
		await DisposeBrowser();
		GC.SuppressFinalize(this);
	}
}
