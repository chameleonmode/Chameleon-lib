using Chameleon.lib.Auth.Oidc;

namespace Tests.Auth;

public class OidcAuth0Tests {

	[Fact]
	public async Task BrowserAuth_SignIn_Success() {
		var result = await BrowserAuth.SignIn();
		Assert.NotNull(result);
	}

	[Fact]
	public async Task OidcAuth0Client_SignIn_Success() {
		var result = await OidcAuth0Client.SignIn();
		Assert.NotNull(result);
	}

	[Fact]
	public async Task OidcAuth0Client_ValidateLicese_Success() {
		var result = await OidcAuth0Client.SignIn();
		Assert.NotNull(result);

		await OidcAuth0Client.ValidateLicese("HHTQ-QJYS-ZMWX-CO5U", result.AccessToken);
	}
}
