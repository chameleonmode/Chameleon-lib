using Chameleon.lib;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.WebBrowser.Models;
using Chameleon.lib.WebBrowser.Services;
using Microsoft.Extensions.Configuration;

namespace Tests;

public abstract class TestSetup {
	public readonly TaskCompletionSource<bool> _tcs = new();
	public TestSetup(int dictionary = 1) {
		IoC.Instance.Configure(() => {
			return new WritableConfiguration(new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build(), Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
		}, (services) => {
			_ = services;
		});

		IoC.Instance.Init(_ => {
			IoC.SetJsonValue(new LoginSettings(
				Environment.Directory[dictionary].email,
				Environment.Directory[dictionary].license
				), nameof(LoginSettings));
		});
	}

	public async Task<IBrowserInstance> LaunchBrowserFromSettings(SysBrowserOpenOptions? options) {
		return await SystemBrowserService.Instance.OpenWithSettings(
			new SysBrowserSettings(options ?? new SysBrowserOpenOptions(Enums.SystemBrowserType.Chrome, new BrowserProfile {
				Id = 0
			}))
		) ?? throw new Exception("Browser instance is null");
	}
}
