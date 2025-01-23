using Chameleon.lib.Auth.Oidc;
using Chameleon.lib.Const;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;

namespace Chameleon.lib.Auth;
public class Session {
	public OidcAuth0Client Auth0Client { get; } = new();
	public LoginSettings? Login => IoC.GetJsonValue<LoginSettings>(nameof(LoginSettings));

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

	public TokenPayload GetJwtPayload() {
		var jwtHandler = new JwtSecurityTokenHandler();
		var jwtToken = jwtHandler.ReadJwtToken(Auth0Client.Token!.access_token);

		// Get the payload as a JSON string
		var payload = jwtToken.Payload.SerializeToJson();

		return JsonSerializer.Deserialize<TokenPayload>(payload!, JS.CaseInsensitiveOptions)!;
	}

	public async Task ValidateLicese() {
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Authorization =
				new("Bearer", Auth0Client.Token!.access_token);

		var response = await httpClient.PostAsJsonAsync(
			$"{Configs.Urls.ABS_PLATFORMATIC_BASE_URL}/license/activate",
			new { license_key = Login!.LicenseKey }
		);
		var body = await response.Content.ReadAsStringAsync();
		if (!response.IsSuccessStatusCode) {
			throw new InvalidOperationException($"License activation error ({response.StatusCode}): " + body);
		}
		//Console.WriteLine("License activation success: " + body);
		//} else {
		//	throw new InvalidOperationException($"License activation error ({response.StatusCode}): " + body);
		//}
	}

	// singleton
	private Session() {
	}
	public static Session Instance { get; } = new();
}
