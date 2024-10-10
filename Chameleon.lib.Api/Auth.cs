using System.Net;

using Chameleon.lib.Common.Extensions;

namespace Chameleon.lib.Api;
public record AuthRequest(string UserNameOrEmailAddress, string Password);
public record LoginResponse(string? AccessToken, string? EncryptedAccessToken, long ExpireInSeconds, string? RefreshToken, long UserId, long? CreatorUserId, string[] Permissions, Limits LicenseLimits, bool TookGuidedTour, bool CanCreateProfiles);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);
public record AuthRefreshTokenResponse(string? NewAccessToken, string? NewRefreshToken, long ExpireInSeconds);
public record IsActiveResponse(bool isActive);
public record Limits(bool HasOutreach, bool HasYouTube, bool HasWordPress, int MaxProfilesCount, ContentDiscoveryLimits ContentDiscoveryLimits, int MaxAssistantsCount);
public record ContentDiscoveryLimits(bool HasProspector, bool HasProspectorContent, bool HasSocials, bool HasSocialsContent, int MaxRssCount);

public static class Auther {
	public static async Task<LoginResponse> LoginAsync(string user, string pass)
	{
		var response = await HttpApiClient.Instance.Post<LoginResponse>("TokenAuth/Authenticate", new AuthRequest(user, pass));
		ArgumentNullException.ThrowIfNull(response.AccessToken, "Response not contain token");

		HttpApiClient.Instance.AuthToken = response.AccessToken;
		return response;
	}

	public static async Task<AuthRefreshTokenResponse> RefreshTokenAsync(string acessToken, string refreshToken)
	{
		var response = await HttpApiClient.Instance.Post<AuthRefreshTokenResponse>("TokenAuth/RefreshToken", new RefreshTokenRequest(acessToken, refreshToken));
		ArgumentNullException.ThrowIfNull(response.NewAccessToken, "Response not contain token");
		ArgumentNullException.ThrowIfNull(response.NewRefreshToken, "Response not contain refresh token");
		HttpApiClient.Instance.AuthToken = response.NewAccessToken;
		return response;
	}

	public static async Task<bool> IsLicenseActiveAsync(string license)
	{
		var response = await HttpApiClient.Instance.Get<IsActiveResponse>($"TokenAuth/IsLicenseActive?key={license}");
		return response.isActive;
	}
}
