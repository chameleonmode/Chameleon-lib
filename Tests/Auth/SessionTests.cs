using Chameleon.lib;
using Chameleon.lib.Auth;

using Microsoft.Extensions.Configuration;

namespace Tests.Auth;
public class SessionTests {
	public SessionTests() {
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
				"elimdadia@gmail.com",
				"HHTQ-QJYS-ZMWX-CO5U"
				), nameof(LoginSettings));
		});
	}
	[Fact]
	public async Task SignIn_Success() {
		await Session.Instance.SignIn();
		Assert.NotNull(Session.Instance.Token);
	}

	[Fact]
	public async Task ValidateLicese_Success() {
		await Session.Instance.SignIn();
		await Session.Instance.ValidateLicese();
	}

	[Fact]
	public async Task RefreshToken_Success() {
		await Session.Instance.SignIn();
		Assert.NotNull(Session.Instance.Token);

		await Session.Instance.RefreshToken();
	}

	[Fact]
	public async Task Logout_Success() {
		await Session.Instance.SignIn();
		Assert.NotNull(Session.Instance.Token);

		Session.Instance.Logout();
	}
}
