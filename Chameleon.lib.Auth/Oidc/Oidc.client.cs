using Auth0.OidcClient;

using Chameleon.lib.Util;

using IdentityModel.OidcClient;

namespace Chameleon.lib.Auth.Oidc;

public class CustomAuth0Client(Auth0ClientOptions options) : Auth0ClientBase(options, "dotnet") { }
public class OidcAuth0Client {
	const string domain = "dev-gcjhdlkot8s8v2vr.us.auth0.com";
	const string clientId = "dEtvplqXMKlDV1xSuuPfTLoWxtR8uMJv";

		public static async Task<LoginResult> SignIn() {
		var port = TcpUtil.NextFreePort(7891);
		if (port > 7896)
			throw new Exception("No free ports available");
		var redirectUri = $"http://127.0.0.1:{port}/callback";

		var client = new CustomAuth0Client(new Auth0ClientOptions {
			Domain = domain,
			ClientId = clientId,
			// Try requesting fewer scopes to see if that affects encryption
			Scope = "openid",
			RedirectUri = redirectUri,
			Browser = new OidcSystemBrowser(redirectUri)
		});

		Console.WriteLine("Launching browser for Auth0 login...");
		var loginResult = await client.LoginAsync(new LoginRequest {
			FrontChannelExtraParameters = {
						{ "audience", "https://api.chameleonmode.com/" },
						{ "response_type", "token id_token" },  // Try implicit flow
            { "nonce", Guid.NewGuid().ToString() },
						{ "token_format", "jwt" },
						{ "token_endpoint_auth_method", "none" }
				}
		});

		if (loginResult.IsError) {
			Console.WriteLine($"Error: {loginResult.Error}");
		} else {
			// Print both tokens for comparison
			Console.WriteLine($"Access Token: {loginResult.AccessToken}");
			Console.WriteLine($"ID Token: {loginResult.IdentityToken}");
		}

		return loginResult;
	}
}

