using System.Diagnostics;

using Chameleon.lib.Api;

namespace Chameleon.lib.Tests.Api;
public class AuthTests : ApiTestsBase {
	[Fact]
	public async Task LoginAsync_ValidCredentials_NeedsRefresh()
	{
		await tcs.Task;
		var isin = await Auther.IsLicenseActiveAsync(lkey);
		Assert.NotNull(LoginResponse);
		Assert.NotNull(LoginResponse.AccessToken);
		Assert.NotNull(LoginResponse.RefreshToken);

		await Auther.RefreshTokenAsync();
		Assert.NotNull(Auther.AuthSession?.RefreshedToken?.NewAccessToken);
		Assert.NotNull(Auther.AuthSession.RefreshedToken.NewRefreshToken);
	}
}
