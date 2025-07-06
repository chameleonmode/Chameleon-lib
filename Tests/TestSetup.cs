using Chameleon.lib;
using Chameleon.lib.Auth;
using Chameleon.lib.WebBrowser.Services;
using Microsoft.Extensions.Configuration;

namespace Tests;

public abstract class TestSetup : IAsyncLifetime {
	public readonly TaskCompletionSource<bool> _tcs = new();
	public TestSetup(int dictionary = 0) {
		IoC.Instance.StartUps.Add(AddonsServer.Instance);
		IoC.Instance.Configure(() => {
			return new WritableConfiguration(new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build(), Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
		}, (services) => {
			_ = services;
		});

		IoC.Instance.Init( _ => {
			IoC.SetJsonValue(new LoginSettings(
				Environment.Directory[dictionary].email,
				Environment.Directory[dictionary].license
				), nameof(LoginSettings));
			_tcs.SetResult(true);
		});
	}

	public Task DisposeAsync() => Task.CompletedTask;
	public virtual async Task InitializeAsync() => await _tcs.Task;
}
