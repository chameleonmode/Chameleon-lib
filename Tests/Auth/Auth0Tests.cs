using Chameleon.lib.Auth.Oidc;

namespace Tests.Auth;

public class Auth0Tests(int dictionary = 0) : TestSetup(dictionary) {
	[Fact]
	public async Task OidcAuth0Client_Login_Success() {
		var auth = new OidcAuth0Client();
		await auth.Login();
		Assert.NotNull(auth.Token);
	}

	[Fact]
	public async Task OidcAuth0Client_RefreshToken_Success() {
		var auth = new OidcAuth0Client();
		await auth.RefreshToken();
		Assert.NotNull(auth.Token);
	}

	[Fact]
	public async Task OidcAuthClient_Logout_Success() {
		var auth = new OidcAuth0Client();
		await auth.Logout();
		Assert.Null(auth.Token);
	}

}
