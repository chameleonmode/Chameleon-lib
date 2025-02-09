using Chameleon.lib.Auth.Oidc;
using System.Net.Http.Headers;

namespace Chameleon.lib.Auth;
public class Session {
	public OidcAuth0Client Auth0Client { get; } = new();
	public LoginSettings? Login => IoC.GetJsonValue<LoginSettings>(nameof(LoginSettings));

	public async Task<(OidcAuth0Client, AuthenticationHeaderValue)> Authenticate() {
		return (Auth0Client, await Auth0Client.TryLogIn());
	}

	public async Task Logout() {
		try {
			await Auth0Client.Logout();
			IoC.ClearValue(nameof(TokenResponse));
		} catch {
			// ignore for now
		} finally {
			if (Login != null)
				IoC.SetJsonValue(new LoginSettings(Login.LoginName, Login.LicenseKey, false), nameof(LoginSettings));
		}
	}

	// singleton
	private Session() {
	}
	public static Session Instance { get; } = new();
}
