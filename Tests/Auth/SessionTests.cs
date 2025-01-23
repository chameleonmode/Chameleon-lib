using Chameleon.lib;
using Chameleon.lib.Auth;

namespace Tests.Auth;
public class SessionTests(int dictionary = 0) : TestSetup(dictionary) {

	[Fact]
	public async Task SignIn_Success() {
		await Session.Instance.SignIn();
		Assert.NotNull(Session.Instance.Auth0Client.Token);
	}

	[Fact]
	public async Task ValidateLicese_Success() {
		await Session.Instance.ValidateLicese();
	}

	[Fact]
	public async Task Logout_Success() {
		await Session.Instance.Logout();
	}
}
