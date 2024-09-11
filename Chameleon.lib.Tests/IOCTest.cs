using Chameleon.lib.Common.Util;
using Chameleon.lib.Common;
using System.Text;
using System.Diagnostics;

namespace Chameleon.lib.Tests;

public class IOCTest {

		TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

		[Fact]
		public async Task TestCommonIOCInit() {
				// Setup
				async void setup() {
						await Task.Delay(2000); // 
						_tcs.SetResult(true);
				}
				// Setup IoC
				IoC.Instance.Init(() => {
						setup();
				});

				_ = await _tcs.Task;

				// Act
				// Now you can use configManager throughout your application
				var browserPath = IoC.GetValue<string>("BrowserPath");
				Debug.WriteLine($"Browser Path: {browserPath}");

				// Set a new value
				IoC.Instance.Config.SetValue("CustomSetting", "NewValue");
		}
}