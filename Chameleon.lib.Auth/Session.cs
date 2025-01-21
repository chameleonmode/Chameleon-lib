using Chameleon.lib.Abs;
using Chameleon.lib.Auth.Oidc;

using System.Net.Http.Json;

namespace Chameleon.lib.Auth;
public class Session {
	public LoginSettings? LoginSetings { get; set; }
	public string? AccessToken { get; set; }

	public async Task SignIn() {
		var auth = new BrowserAuth();
		var code = await auth.GetCode();
		var token = await auth.GetToken(code) ?? throw new Exception("Token not found in response");
		AccessToken = token.access_token;
	}

	public async Task ValidateLicese() {
		ArgumentNullException.ThrowIfNull(LoginSetings);
		ArgumentNullException.ThrowIfNull(AccessToken);

		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Authorization =
				new("Bearer", AccessToken);

		var response = await httpClient.PostAsJsonAsync(
			$"{Constas.ABS_PLATFORMATIC_BASE_URL}/license/activate",
			new { LoginSetings.LicenseKey }
		);
		var body = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode) {
			Console.WriteLine("License activation success: " + body);
		} else {
			throw new InvalidOperationException($"License activation error ({response.StatusCode}): " + body);
		}
	}

	// singleton
	public static Session Instance { get; } = new();
}
