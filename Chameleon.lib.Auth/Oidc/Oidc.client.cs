using Chameleon.lib.Common.ServiceManagers;
using Chameleon.lib.Util;

using IdentityModel.OidcClient;

namespace Chameleon.lib.Auth.Oidc;

public class OidcAuth0Client {
	const string domain = "dev-gcjhdlkot8s8v2vr.us.auth0.com";
	const string clientId = "EAmQqccRcR2mvzGnnQpQAIR7ueTIovH0";

	public static async void SignIn() {
		var port = TcpUtil.NextFreePort(7891);
		if (port > 7896)
			throw new Exception("No free ports available");

		var redirectUri = $"http://127.0.0.1:{port}/callback";
		var client = new OidcClient(new OidcClientOptions {
			Authority = domain,
			ClientId = clientId,
			Scope = "openid profile email",
			RedirectUri = redirectUri,  // must match what's in Auth0 Allowed Callback
			Browser = new OidcSystemBrowser(redirectUri)
		});

		var result = await client.LoginAsync(new LoginRequest());
		if (result.IsError) {
			Toaster.Error("Error logging in", result.Error);
		} else {
			Toaster.Success("Logged in");
			Console.WriteLine($"Identity token: {result.IdentityToken}");
			Console.WriteLine($"Access token: {result.AccessToken}");
		}
	}
}
