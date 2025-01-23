using Chameleon.lib.Const;
using Chameleon.lib.Util;

using System.Text.Json;

namespace Chameleon.lib.Auth.Oidc;
public class OidcAuth0Client {
	readonly string state;
	readonly string codeVerifier;
	readonly string codeChallenge;
	public OidcBrowser OidcBrowser { get; } 
	public string RedirectUri { get; }

	public TokenResponse? Token => IoC.GetJsonValue<TokenResponse>(nameof(TokenResponse));
	public string AuthUrl => $"https://{Configs.Oidc.Domain}/authorize?" +
				$"response_type=code&" +
				$"client_id={Configs.Oidc.ClientId}&" +
				$"redirect_uri={Uri.EscapeDataString(RedirectUri)}&" +
				$"scope=openid%20profile%20email%20offline_access&" +
				$"audience={Uri.EscapeDataString(Configs.Oidc.ApiAudience)}&" +
				$"state={state}&" +
				$"code_challenge={codeChallenge}&" +
				$"code_challenge_method=S256";
	public string LogoutUrl => $"https://{Configs.Oidc.Domain}/oidc/logout?" +
				$"post_logout_redirect_uri={Uri.EscapeDataString(RedirectUri)}&" +
				$"id_token_hint={Uri.EscapeDataString(Token!.id_token)}&" +
				$"client_id={Configs.Oidc.ClientId}";

	public OidcAuth0Client() {
		// Generate state and PKCE values
		state = StringsUtil.GenerateRandomString();
		codeVerifier = StringsUtil.GenerateRandomString();
		codeChallenge = StringsUtil.GenerateCodeChallenge(codeVerifier);

		RedirectUri = $"http://127.0.0.1:{TcpUtil.NextFreePort(7891, 7896)}/callback";
		OidcBrowser = new OidcBrowser(this);
	}

	private TokenResponse DeserializeToken(string res) {
		var token = JsonSerializer.Deserialize<TokenResponse>(res, JS.CaseInsensitiveOptions)
			?? throw new Exception("Token not found in response");
		IoC.SetJsonValue(token, nameof(TokenResponse));
		return token;
	}

	private void SaveToken(TokenResponse token) {
		IoC.SetJsonValue(token, nameof(TokenResponse));
	}
	private async Task<TokenResponse> GetNewToken(string code) {
		using var client = new HttpClient();
		var res = await client.PostAsync(
			$"https://{Configs.Oidc.Domain}/oauth/token",
			new FormUrlEncodedContent(new Dictionary<string, string> {
				{ "grant_type", "authorization_code" },
				{ "client_id", Configs.Oidc.ClientId },
				{ "code_verifier", codeVerifier },
				{ "code", code },
				{ "redirect_uri", RedirectUri }
			})
		);

		return DeserializeToken(await res.Content.ReadAsStringAsync());
	}

	/// <summary>
	/// Exchange the code for a token
	/// </summary>
	/// <param name="code"></param>
	/// <returns></returns>
	public async Task Login() {
		var code = await OidcBrowser.GetCode();
		var token = await GetNewToken(code);
		SaveToken(token);
	}

	/// <summary>
	/// Logs out the current user by revoking tokens and clearing stored token data
	/// </summary>
	/// <returns>Task representing the logout operation</returns>
	public async Task Logout() {
		await OidcBrowser.Logout();
		IoC.ClearValue(nameof(TokenResponse));
	}

	/// <summary>
	/// Refresh Token
	/// </summary>
	/// <param name="refreshToken"></param>
	/// <returns></returns>
	public async Task RefreshToken() {
		ArgumentNullException.ThrowIfNull(Token);

		using var client = new HttpClient();
		var res = await client.PostAsync(
				$"https://{Configs.Oidc.Domain}/oauth/token",
				new FormUrlEncodedContent(new Dictionary<string, string> {
					 { "grant_type", "refresh_token" },
					 { "client_id", Configs.Oidc.ClientId },
					 { "refresh_token", Token.refresh_token }
				})
		);

		SaveToken(DeserializeToken(await res.Content.ReadAsStringAsync()));
	}
}
