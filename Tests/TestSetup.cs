using Chameleon.lib;
using Chameleon.lib.Auth;
using Microsoft.Extensions.Configuration;

namespace Tests;

public abstract class TestSetup : IAsyncLifetime {
	public readonly TaskCompletionSource<bool> tcs = new();
	public int Env { get; }
	public TestSetup(int env = 0) {
		Env = env;
		IoC.I.Configure(
			(c) => {
			_ = c.SetBasePath(Directory.GetCurrentDirectory());
			},
			(services) => {
				_ = services;
		});
		tcs.SetResult(true);
	}
	public Task DisposeAsync() => Task.CompletedTask;
	public virtual async Task InitializeAsync() {
		await tcs.Task;
		IoC.SetJsonValue(nameof(LoginSettings), new LoginSettings(
			TestEnvironment.Directory[Env].email,
			TestEnvironment.Directory[Env].license
		));
	}
}
