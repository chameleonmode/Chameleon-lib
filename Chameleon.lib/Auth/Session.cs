using Chameleon.lib.Auth.Oidc;
using System.Net.Http.Headers;

namespace Chameleon.lib.Auth;

public class Session
{
	Session() { }

	public Client Auth0Client { get; } = new();
	public LoginSettings? Login { get; private set; }

	public void SetLogin(LoginSettings login)
	{
		Login = login;
		IoC.SetJsonValue(login, nameof(LoginSettings));
	}

	public async Task<(Client, AuthenticationHeaderValue)> Authenticate()
	{
		return (Auth0Client, await Auth0Client.TryLogIn());
	}

	public async Task Logout()
	{
		try
		{
			await Auth0Client.Logout();
			Auth0Client.Authorization = null;
			IoC.ClearValue(nameof(TokenResponse));
		}
		catch (Exception)
		{
			// ignore for now
		}
		finally
		{
			if (Login != null)
				IoC.SetJsonValue(new LoginSettings(Login.LoginName, Login.LicenseKey, false), nameof(LoginSettings));
		}
	}

	// singleton	
	public static Session Instance { get; } = new();
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
