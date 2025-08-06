using Chameleon.lib.Auth.Oidc;
using Chameleon.lib.Util;
using System.Net.Http.Headers;

namespace Chameleon.lib.Auth;

public class Session {
	public Client Auth0Client { get; } = new Client();
	public LoginSettings Settings { get; set; } = IoC.GetJsonValue<LoginSettings>(nameof(LoginSettings)) ?? new("", "", false);
	Session() { }

	public void Save(LoginSettings settings) {
		IoC.SetJsonValue(nameof(LoginSettings), settings, null);
		Settings = settings;
	}

	public async Task Login(LoginSettings login) {
		_ = await Authenticate();
		Save(login);
	}

	public async Task<(Client, AuthenticationHeaderValue)> Authenticate(Client? client = null) {
		client ??= Auth0Client;
		return (client, await client.TryLogIn());
	}

	public async Task Logout() {
		try {
			await Auth0Client.Logout();
		} catch (Exception e) {
			EX.PrintException(e);
		} finally {
			IoC.ClearValue(nameof(TokenResponse));
			Save(Settings with { AutoLogin = false });
		}
	}

	// singleton	
	public static Session I { get; } = new();
}

public record LoginSettings(string LoginName = "", string LicenseKey = "", bool AutoLogin = true);

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
