using Chameleon.lib.Auth.Oidc;
using System.Net.Http.Headers;

namespace Chameleon.lib.Auth;
public class Session {
	AuthenticationHeaderValue? authorization;

	public OidcAuth0Client Auth0Client { get; } = new();
	public LoginSettings? Login => IoC.GetJsonValue<LoginSettings>(nameof(LoginSettings));

	public async Task<AuthenticationHeaderValue> Authenticate() {
		if (authorization?.Parameter is null) {
			try {
				await Auth0Client.RefreshToken();
			} catch {
				await Auth0Client.Login();
			} finally {
				authorization = new("Bearer", Auth0Client.Token?.access_token);
			}
		}
		return authorization;
	}

	public async Task Logout() {
		await Auth0Client.Logout();
		if (Login != null)
			IoC.SetJsonValue(new LoginSettings(Login.LoginName, Login.LicenseKey, false), nameof(LoginSettings));
	}

	// singleton
	private Session() {
	}
	public static Session Instance { get; } = new();
}
