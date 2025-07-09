using Chameleon.lib.Auth.Oidc;

namespace Tests.Auth;

public class Auth0Tests(int dictionary = 0) : TestSetup(dictionary) {
	static Client Auth => new();
	[Fact]
	public async Task OidcAuth0Client_Login_Success() {
		await Auth.Login();
		Assert.NotNull(Auth.Token);
	}

	[Fact]
	public async Task OidcAuth0Client_RefreshToken_Success() {
		await Auth.RefreshToken();
		Assert.NotNull(Auth.Token);
	}

	[Fact]
	public async Task OidcAuthClient_Logout_Success() {
		await Auth.Logout();
		Assert.Null(Auth.Token);
	}
}
