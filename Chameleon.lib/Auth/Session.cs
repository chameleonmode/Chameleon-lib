using Chameleon.lib.Auth.Oidc;
using System.Net.Http.Headers;

namespace Chameleon.lib.Auth;
public class Session {
	Session() { }

	public Client Auth0Client { get; } = new();
	public LoginSettings? Login { get; private set; }

	public void SetLogin(LoginSettings login) {
		Login = login;
		IoC.SetJsonValue(login, nameof(LoginSettings));
	}

	public async Task<(Client, AuthenticationHeaderValue)> Authenticate() {
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
	public static Session Instance { get; } = new();
}
