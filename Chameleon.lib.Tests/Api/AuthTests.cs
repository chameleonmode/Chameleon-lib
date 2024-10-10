using System.Diagnostics;

using Chameleon.lib.Api;

namespace Chameleon.lib.Tests.Api;
public class AuthTests : Base {
	[Fact]
	public async Task LoginAsync_ValidCredentials_Succeeds()
	{
		var login = await Auther.LoginAsync(email, lkey);
		Assert.NotNull(login.AccessToken);
		Assert.NotNull(login.RefreshToken);

		var refresh = await Auther.RefreshTokenAsync(login.AccessToken, login.RefreshToken);
		Assert.NotNull(refresh.NewAccessToken);
		Assert.NotNull(refresh.NewRefreshToken);
	}

	[Fact]
	public async Task LoginAsync_ValidCredentials_NeedsRefresh()
	{
		var isin = await Auther.IsLicenseActiveAsync(lkey);

		var login = await Auther.LoginAsync(email,lkey);
		Assert.NotNull(login.AccessToken);
		Assert.NotNull(login.RefreshToken);

		var refresh = await Auther.RefreshTokenAsync(login.AccessToken, login.RefreshToken);
		Assert.NotNull(refresh.NewAccessToken);
		Assert.NotNull(refresh.NewRefreshToken);
	}
}
