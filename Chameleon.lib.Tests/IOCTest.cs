using Chameleon.lib.Common;
using System.Diagnostics;
using Chameleon.lib.Common.Types;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

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