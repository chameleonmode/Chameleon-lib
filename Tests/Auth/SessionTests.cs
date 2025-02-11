using Chameleon.lib;
using Chameleon.lib.Auth;

namespace Tests.Auth;
public class SessionTests : TestSetup {
	public SessionTests() : base(1) { }
	[Fact]
	public async Task SignIn_Success() {
		_ = await Session.Instance.Authenticate();
		Assert.NotNull(Session.Instance.Auth0Client.Token);
	}

	[Fact]
	public async Task Logout_Success() {
		await Session.Instance.Logout();
	}
}
