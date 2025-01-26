using Chameleon.lib.Auth.Oidc;
using System.Net.Http.Headers;

namespace Chameleon.lib.Auth;
public class Session {
	public OidcAuth0Client Auth0Client { get; } = new();
	public LoginSettings? Login => IoC.GetJsonValue<LoginSettings>(nameof(LoginSettings));

	public AuthenticationHeaderValue Authorization =>
		new("Bearer", Auth0Client.Token?.access_token);

	public async Task SignIn() {
		try {
			await Auth0Client.RefreshToken();
		} catch {
			await Auth0Client.Login();
		}
	}

	public async Task Logout() {
		if (Login != null)
			IoC.SetJsonValue(new LoginSettings(Login.LoginName, Login.LicenseKey, false), nameof(LoginSettings));
		await Auth0Client.Logout();
	}

	// singleton
	private Session() {
	}
	public static Session Instance { get; } = new();
}
