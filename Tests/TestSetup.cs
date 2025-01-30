using Chameleon.lib;

using Microsoft.Extensions.Configuration;

namespace Tests;
public abstract class TestSetup {
	public TestSetup(int dictionary = 3) {
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
}
