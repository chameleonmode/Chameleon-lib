using Chameleon.lib.Playwright.node;
using Chameleon.lib.Common;

using System.Diagnostics;

namespace Chameleon.lib.Tests.Playwright;

public class PlaywrightTestRunnerTests : PlaywrightTestsBase, IDisposable {
	private PlaywrightTestRunner? runner;

	public PlaywrightTestRunnerTests() : base()
	{
		void setup(bool init)
		{

			_tcs.SetResult(true);
		}
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
				await runner.RunTestAsync(test.testName, test.testData);
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

		runner = new PlaywrightTestRunner(IoC.Instance.Config);
		try {
			runner.TestOutputReceived += (sender, output) => Debug.WriteLine($"Test output: {output}");
			runner.TestErrorReceived += (sender, error) => Debug.WriteLine($"Test error: {error}");
			var data = new
			{
				url = "https://sites.google.com/",
				testEmail = "testjosh11011900@gmail.com",
				testPW = "testjosh11011900@123",
				textContent = "Anti-detect browser is capable of creating and running multiple digital identities that are not recognized by social platforms. This requires a lot of custom developer work, so such tools are generally not available for free. They are created to fight against tracking and analytics so that you can carry out your activities in private. In other words, an anti-fingerprint browser enhances privacy, keeps your data and web activities anonymous, and helps your web crawling tools avoid being blocked",
				textSearch = "What is anti detect browser",
				washington = "washington",
				antidetect = "antidetectbrowsersexplanied5",
				gsiteTitle = "GsiteTitle"
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
