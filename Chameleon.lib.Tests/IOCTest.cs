using Chameleon.lib.Common;
using System.Diagnostics;
using Chameleon.lib.Common.Types;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using Chameleon.lib.WebBrowser.Models;
using System.Text.Json;

namespace Chameleon.lib.Tests;

public class IOCTest {
	private readonly TaskCompletionSource<bool> _tcs = new();

	public IOCTest()
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
	}

	[Fact]
	public async Task Test_Appsettings()
	{
		_ = await _tcs.Task;

		// Now you can use configManager throughout your application
		var browserPath = IoC.GetValue<string>("BrowserPath");
		Debug.WriteLine($"Browser Path: {browserPath}");

		// Set a new value
		IoC.SetValue("NewValue", "CustomSetting");
		var customSetting = IoC.GetValue<string>("CustomSetting");
		Assert.True(customSetting == "NewValue");

		// Set a new Type value
		IoC.SetJsonValue(new EmulationOptions {
			DisableWebRTC = true,
			SpoofClientRects = true,
			SpoofFontFingerprint = true,
			SpoofCanvasFingerprint = true,
			SpoofWebGLFingerprint = true,
			SpoofGeoLocation = true,
			AutoTimezone = true,
		}, nameof(EmulationOptions));
		var emulations = IoC.GetJsonValue<EmulationOptions>(nameof(EmulationOptions));
		Assert.NotNull(emulations);

		// Set a new arrat Type value
		IoC.SetValue<string[]>(["duckduckgo.com", "1", "2" ], "arrr");
		var arr = IoC.GetValue<string[]>("arrr");
		Assert.NotNull(arr);
	}
}