using Chameleon.lib.Auth.Oidc;
using Chameleon.lib.Util;
using System.Net.Http.Headers;

namespace Chameleon.lib.Auth;

public class Session {
	public Client Auth0Client { get; }

	public Func<string, Task> OpenBrowser { get; set; } = url => Task.Run(() => ProcessUtil.OpenBrowser(url));
	public LoginSettings Settings { get; set; } = IoC.GetJsonValue<LoginSettings>(nameof(LoginSettings)) ?? new("", "", false);
	
	Session() {
		Auth0Client = new Client(async Url => await OpenBrowser(Url));
	}

	public void Save(LoginSettings settings) {
		IoC.SetJsonValue(settings, nameof(LoginSettings));
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
		await EX.Try(Auth0Client.Logout);
		IoC.ClearValue(nameof(TokenResponse));
		Save(Settings with { AutoLogin = false });
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
