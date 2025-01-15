using System.Diagnostics;

using Chameleon.lib.Api;

namespace Chameleon.lib.Tests.Api;
public class AuthTests {
	const int dictionary = 2;

	[Fact]
	public async Task Login_success()
	{
		await Auther.LoginAsync(
			Environment.Directory[dictionary].email, 
			Environment.Directory[dictionary].license
		);

		Assert.NotNull(Auther.AuthSession);
		Assert.NotNull(Auther.AuthSession.AccessToken);
		Assert.NotNull(Auther.AuthSession.RefreshToken);
	}

	[Fact]
	public async Task IsLicenseActive_success()
	{
		var active = await Auther.IsLicenseActiveAsync(Environment.Directory[dictionary].license);
		Assert.True(active);
		await Auther.RefreshTokenAsync();
		Assert.NotNull(Auther.AuthSession?.RefreshedToken?.NewAccessToken);
		Assert.NotNull(Auther.AuthSession.RefreshedToken.NewRefreshToken);
	}


	[Fact]
	public async Task RefreshToken_success()
	{
		await Login_success();

		await Auther.RefreshTokenAsync();
		Assert.NotNull(Auther.AuthSession?.RefreshedToken?.NewAccessToken);
		Assert.NotNull(Auther.AuthSession.RefreshedToken.NewRefreshToken);
	}
}
