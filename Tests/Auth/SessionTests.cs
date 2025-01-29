using Chameleon.lib;
using Chameleon.lib.Auth;

namespace Tests.Auth;
public class SessionTests(int dictionary = 0) : TestSetup(dictionary) {

	[Fact]
	public async Task SignIn_Success() {
		await Session.Instance.Authenticate();
		Assert.NotNull(Session.Instance.Auth0Client.Token);
	}

	[Fact]
	public async Task Logout_Success() {
		await Session.Instance.Logout();
	}
}
