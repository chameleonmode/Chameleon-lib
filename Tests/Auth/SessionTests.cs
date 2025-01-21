using Chameleon.lib;
using Chameleon.lib.Auth;

namespace Tests.Auth;
public class SessionTests {
	[Fact]
	public async Task SignIn_Success() {
		await Session.Instance.SignIn();
		Assert.NotNull(Session.Instance.AccessToken);
	}

	[Fact]
	public async Task ValidateLicese_Success() {
		Session.Instance.LoginSetings = new LoginSettings(
			"elimdadia@gmail.com",
			"HHTQ-QJYS-ZMWX-CO5U"
		);
		await Session.Instance.SignIn();
		await Session.Instance.ValidateLicese();
	}
}
