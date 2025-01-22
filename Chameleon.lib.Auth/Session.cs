using Chameleon.lib.Abs;
using Chameleon.lib.Auth.Oidc;

using System.Net.Http.Json;

namespace Chameleon.lib.Auth;
public class Session {
	public LoginSettings? Login => IoC.GetJsonValue<LoginSettings>(nameof(LoginSettings));
	public TokenResponse? Token => IoC.GetJsonValue<TokenResponse>(nameof(TokenResponse));

	private void SetToken(TokenResponse token) {
		IoC.SetJsonValue(token, nameof(TokenResponse));
	}

	public async Task SignIn() {
		var auth = new BrowserAuth();
		var code = await auth.GetCode();
		var token = await auth.GetToken(code);
		SetToken(token);
	}

	public async Task RefreshToken() {
		ArgumentNullException.ThrowIfNull(Token);
		var token = await BrowserAuth.RefreshToken(Token.refresh_token);
		SetToken(token);
	}

	public void Logout() {
		if (Login != null)
			IoC.SetJsonValue(new LoginSettings(Login.LoginName, Login.LicenseKey, false), nameof(LoginSettings));
		IoC.ClearValue(nameof(TokenResponse));
	}

	public async Task ValidateLicese() {
		ArgumentNullException.ThrowIfNull(Login);
		ArgumentNullException.ThrowIfNull(Token);

		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Authorization =
				new("Bearer", Token.access_token);

		var response = await httpClient.PostAsJsonAsync(
			$"{Constas.ABS_PLATFORMATIC_BASE_URL}/license/activate",
			new { license_key = Login.LicenseKey }
		);
		var body = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode) {
			Console.WriteLine("License activation success: " + body);
		} else {
			throw new InvalidOperationException($"License activation error ({response.StatusCode}): " + body);
		}
	}

	// singleton
	private Session() {
	}
	public static Session Instance { get; } = new();
}
