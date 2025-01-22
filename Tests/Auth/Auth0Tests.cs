using Chameleon.lib.Auth;
using Chameleon.lib.Auth.Oidc;

namespace Tests.Auth;

public class Auth0Tests {
	[Fact]
	public async Task BrowserAuth_GetCode_Success() {
		var codeResult = await new BrowserAuth().GetCode();
		Assert.NotNull(codeResult);
	}

	[Fact]
	public async Task BrowserAuth_GetToken_Success() {
		var bauth = new BrowserAuth();
		var codeResult = await bauth.GetCode();
		Assert.NotNull(codeResult);

		var tokenResult = await bauth.GetToken(codeResult);
		Assert.NotNull(tokenResult);
	}
}
