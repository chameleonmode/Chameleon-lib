using Chameleon.lib.Auth.Oidc;
using Chameleon.lib.Util;
using System.Net.Http.Headers;

namespace Chameleon.lib.Auth;

public class Session {
	public string LoginName => Login!.LoginName;
	public string LicenseKey => Login!.LicenseKey;
	Session() { }

	public Client Auth0Client { get; } = new();
	public LoginSettings? Login { get; private set; }

	public async Task Logineer(LoginSettings login) {
		_ = await Authenticate();
		Login = login;
		IoC.SetJsonValue(login, nameof(LoginSettings));
	}

	public async Task<(Client, AuthenticationHeaderValue)> Authenticate() {
		return (Auth0Client, await Auth0Client.TryLogIn());
	}

	public async Task Logout() {
		await EX.Try(async () => {
			await Auth0Client.Logout();
			Auth0Client.Authorization = null;
			IoC.ClearValue(nameof(TokenResponse));
		});
		if (Login == null) return;
		IoC.SetJsonValue(new LoginSettings(Login.LoginName, Login.LicenseKey, false), nameof(LoginSettings));
	}

	// singleton	
	public static Session I { get; } = new();
}

public record LoginSettings(string LoginName, string LicenseKey, bool AutoLogin = true);

#pragma warning disable IDE1006 // Naming Styles
public record TokenResponse(
		string access_token,
		string refresh_token,
		string id_token,
		string Scope,
		int expires_in,
		string token_type
);

public record TokenPayload(
		string iss,
		string sub,
		string[] aud,
		int iat,
		int exp,
		string scope,
		string azp,
		object[] permissions
);

#pragma warning restore IDE1006 // Naming Styles
