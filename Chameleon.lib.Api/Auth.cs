using Chameleon.lib.Abs;
using Chameleon.lib.Common;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Interfaces.Sys;
using Chameleon.lib.Common.ServiceManagers;

namespace Chameleon.lib.Api;
public record LoginResponse(string? AccessToken, string? EncryptedAccessToken, long ExpireInSeconds, string? RefreshToken, long UserId, long? CreatorUserId, string[] Permissions, Limits LicenseLimits, bool TookGuidedTour, bool CanCreateProfiles) {
	public RefreshTokenResponse? RefreshedToken { get; set; }
	public string? UserName { get; set; }
	public string? LicenseKey { get; set; }
}
public record RefreshTokenResponse(string? NewAccessToken, string? NewRefreshToken, long ExpireInSeconds);
public record IsActiveResponse(bool isActive);
public record Limits(bool HasOutreach, bool HasYouTube, bool HasWordPress, int MaxProfilesCount, ContentDiscoveryLimits ContentDiscoveryLimits, int MaxAssistantsCount);
public record ContentDiscoveryLimits(bool HasProspector, bool HasProspectorContent, bool HasSocials, bool HasSocialsContent, int MaxRssCount);

public static class Auther {
	public static LoginResponse? AuthSession { get; private set; }
	public static string AuthToken => AuthSession?.RefreshedToken?.NewAccessToken ?? AuthSession?.AccessToken ?? string.Empty;
	public static async Task LoginAsync(string user, string pass)
	{
		var response = await HttpApiClient.Instance.Post<LoginResponse>("TokenAuth/Authenticate", new { UserNameOrEmailAddress = user, Password = pass });
		ArgumentNullException.ThrowIfNull(response.AccessToken, "Response not contain token");
		AuthSession = response;
		AuthSession.UserName = user;
		AuthSession.LicenseKey = pass;

		ABService.Instance.Use(() => (
			AuthSession.UserId,
			AuthSession.UserName,
			AuthSession.LicenseKey,
			AuthSession.CreatorUserId),
			(msg) => {
				if (msg.Is())
					Toaster.Info(msg!);
			},
			(keyValue) => {
				IoC.SetValue(keyValue.value, keyValue.key);
			},
			(key) => IoC.GetValue(key)
		);
	}

	public static async Task RefreshTokenAsync()
	{
		ArgumentNullException.ThrowIfNull(AuthSession, "AuthSession is null");
		var response = await HttpApiClient.Instance.Post<RefreshTokenResponse>("TokenAuth/RefreshToken", new { AuthSession.AccessToken, AuthSession.RefreshToken });
		ArgumentNullException.ThrowIfNull(response.NewAccessToken, "Response not contain token");
		ArgumentNullException.ThrowIfNull(response.NewRefreshToken, "Response not contain refresh token");
		AuthSession!.RefreshedToken = response;
	}

	public static async Task<bool> IsLicenseActiveAsync(string license)
	{
		var response = await HttpApiClient.Instance.Get<IsActiveResponse>($"TokenAuth/IsLicenseActive?key={license}");
		ArgumentNullException.ThrowIfNull(response?.isActive, "Response not contain token");
		return response.isActive;
	}
}
