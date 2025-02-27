using Chameleon.lib;
using Chameleon.lib.WebBrowser.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Tests.WebBrowser;
public class WebBroswserTestsBase {
	public readonly TaskCompletionSource<bool> _tcs = new();
	public WebBroswserTestsBase() {
		void setup(bool init) {
			_tcs.SetResult(true);
		}
		IoC.Instance.Configure(() => {
			return new WritableConfiguration(new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build(), Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
		}, (_) => {
		});
		// Setup IoC
		IoC.Instance.Init(action: setup);
	}
}
