using Chameleon.lib;
using Chameleon.lib.Auth;
using Microsoft.Extensions.Configuration;

namespace Tests;

public abstract class TestSetup : IAsyncLifetime {
	public readonly TaskCompletionSource<bool> _tcs = new();
	public TestSetup(int dictionary = 0) {
		IoC.I.Configure((c) => {
			_ = c.SetBasePath(Directory.GetCurrentDirectory());
		}, (services) => {
			_ = services;
		});
			IoC.SetJsonValue(new LoginSettings(
				Environment.Directory[dictionary].email,
				Environment.Directory[dictionary].license
				), nameof(LoginSettings));
			_tcs.SetResult(true);

	}

	public Task DisposeAsync() => Task.CompletedTask;
	public virtual async Task InitializeAsync() => await _tcs.Task;
}
