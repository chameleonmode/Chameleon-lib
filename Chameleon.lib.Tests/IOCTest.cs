using Chameleon.lib.Common.Util;
using Chameleon.lib.Common;
using System.Text;
using System.Diagnostics;
using Chameleon.lib.Common.Types;
using Chameleon.lib.Core.Automation.Interfaces;
using Chameleon.lib.Core.Automation.Services;
using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chameleon.lib.Tests;

public class IOCTest {

	TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

	[Fact]
	public async Task TestCommonIOCInit()
	{
		// Setup
		async void setup()
		{
			await Task.Delay(2000); // 
			_tcs.SetResult(true);
		}
		IoC.Instance.Configure(() => {
			return new WritableConfiguration(new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build(), Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
		}, (services) => {
			_ = services;
		});
		// Setup IoC
		IoC.Instance.Init((on) => {
			setup();
		});

		_ = await _tcs.Task;

		// Act
		// Now you can use configManager throughout your application
		var browserPath = IoC.GetValue<string>("BrowserPath");
		Debug.WriteLine($"Browser Path: {browserPath}");

		// Set a new value
		IoC.Instance.Config?.SetValue("CustomSetting", "NewValue");
	}
}