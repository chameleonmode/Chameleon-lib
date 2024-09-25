using Chameleon.lib.Common;
using Chameleon.lib.Common.Types;
using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.WebBrowser.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chameleon.lib.Tests.WebBrowser;
public class WebBroswserTestsBase {
	public readonly TaskCompletionSource<bool> _tcs = new();
	public IExtensionLoaderService? ExtensionLoaderService;
	public ISysBrowserService? SysBrowserServiceBase;
	public WebBroswserTestsBase()
	{
		void setup(bool init)
		{
			ExtensionLoaderService = IoC.GetService<IExtensionLoaderService>();
			SysBrowserServiceBase = IoC.GetService<ISysBrowserService>();
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
			.AddSingleton<IExtensionLoaderService, ExtensionLoaderService>()
			.AddSingleton<ISysBrowserService, SysBrowserService>();
		});
		// Setup IoC
		IoC.Instance.Init(action: setup);
	}
}
