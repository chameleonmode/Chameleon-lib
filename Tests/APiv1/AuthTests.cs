using Chameleon.lib.Api;

namespace Tests.APiv1;
public class AuthTests {
	const int dictionary = 3;

	[Fact]
	public async Task Login_success() {
		await Auther.LoginAsync(
			TestEnvironment.Directory[dictionary].email,
			TestEnvironment.Directory[dictionary].license
		);

		Assert.NotNull(Auther.AuthSession);
		Assert.NotNull(Auther.AuthSession.AccessToken);
		Assert.NotNull(Auther.AuthSession.RefreshToken);
	}

	[Fact]
	public async Task IsLicenseActive_success() {
		var active = await Auther.IsLicenseActiveAsync(TestEnvironment.Directory[dictionary].license);
		Assert.True(active);

		await Auther.RefreshTokenAsync();
		Assert.NotNull(Auther.AuthSession?.RefreshedToken?.NewAccessToken);
		Assert.NotNull(Auther.AuthSession.RefreshedToken.NewRefreshToken);
	}

	[Fact]
	public async Task RefreshToken_success() {
		await Login_success();

		await Auther.RefreshTokenAsync();
		Assert.NotNull(Auther.AuthSession?.RefreshedToken?.NewAccessToken);
		Assert.NotNull(Auther.AuthSession.RefreshedToken.NewRefreshToken);
	}
}
