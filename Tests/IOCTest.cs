using Microsoft.Extensions.Configuration;
using Chameleon.lib;
using Chameleon.lib.WebBrowser;
using Chameleon.lib.Browzer;

namespace Tests;

public class IOCTest {
	private readonly TaskCompletionSource<bool> _tcs = new();

	public IOCTest() {
		// Setup
		async void setup() {
			await Task.Delay(20); // 
			_tcs.SetResult(true);
		}
		IoC.I.Configure((builder) => {
			_ = builder.SetBasePath(Directory.GetCurrentDirectory());
		}, (services) => {
			_ = services;
		});
			setup();
	}

	[Fact]
	public async Task Test_Appsettings() {
		_ = await _tcs.Task;

		// Set a new value
		IoC.SetValue("NewValue", "CustomSetting");
		var customSetting = IoC.GetValue("NewValue");
		Assert.True(customSetting == "CustomSetting");

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
	}
}