using System.Net.Http.Json;
using System.Text.Json;
using System.Text;

using Auth0.OidcClient;

using Chameleon.lib.Abs;
using Chameleon.lib.Util;

using IdentityModel.OidcClient;
using System.Net;
using System.Security.Cryptography;

namespace Chameleon.lib.Auth.Oidc;
public class TokenResponse {
	public string AccessToken { get; set; }
	public string IdToken { get; set; }
	public string Scope { get; set; }
	public int ExpiresIn { get; set; }
	public string TokenType { get; set; }
}

public class BrowserAuth {
	const string domain = "dev-gcjhdlkot8s8v2vr.us.auth0.com";
	const string clientId = "dEtvplqXMKlDV1xSuuPfTLoWxtR8uMJv";

	private static HttpListener? listener;
	const string redirectUri = "http://127.0.0.1:7891/callback";

	public static async Task<TokenResponse> SignIn() {
		// Generate state and PKCE values
		string state = GenerateRandomString();
		string codeVerifier = GenerateRandomString();
		string codeChallenge = GenerateCodeChallenge(codeVerifier);

		// Start local server to receive callback
		listener = new HttpListener();
		listener.Prefixes.Add(redirectUri.EndsWith("/") ? redirectUri : redirectUri + "/");
		listener.Start();

		// Construct authorization URL
		var authUrl = $"https://{domain}/authorize?" +
				$"response_type=code&" +
				$"client_id={clientId}&" +
				$"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
				$"scope=openid%20profile%20email&" +
				$"audience={Uri.EscapeDataString("https://api.chameleonmode.com/")}&" +
				$"state={state}&" +
				$"code_challenge={codeChallenge}&" +
				$"code_challenge_method=S256";

		// Open browser
		System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
			FileName = authUrl,
			UseShellExecute = true
		});

		string code = "";
		try {
			// Wait for the callback
			var context = await listener.GetContextAsync();
			var request = context.Request;
			var response = context.Response;

			// Send a nice HTML response
			var responseHtml = @"
               <!DOCTYPE html>
               <html>
               <head>
                   <meta charset='utf-8'/>
                   <title>Authentication Complete</title>
                   <style>
                       body { 
                           font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
                           background: #f0f2f5;
                           display: flex;
                           align-items: center;
                           justify-content: center;
                           height: 100vh;
                           margin: 0;
                       }
                       .container {
                           background: white;
                           padding: 2rem 3rem;
                           border-radius: 8px;
                           box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                           text-align: center;
                       }
                       h1 { 
                           color: #1a73e8;
                           margin-bottom: 1rem;
                       }
                       p {
                           color: #5f6368;
                           margin-bottom: 2rem;
                       }
                       .checkmark {
                           color: #34a853;
                           font-size: 48px;
                           margin-bottom: 1rem;
                       }
                       .close-text {
                           font-size: 0.9rem;
                           color: #80868b;
                       }
                   </style>
               </head>
               <body>
                   <div class='container'>
                       <div class='checkmark'>✓</div>
                       <h1>Authentication Complete</h1>
                       <p>Successfully authenticated with Auth0</p>
                       <span class='close-text'>You can close this window now</span>
                   </div>
                   <script>
                       setTimeout(() => window.close(), 2000);
                   </script>
               </body>
               </html>";

			var buffer = Encoding.UTF8.GetBytes(responseHtml);
			response.ContentLength64 = buffer.Length;
			response.ContentType = "text/html";
			await response.OutputStream.WriteAsync(buffer);
			response.Close();

			// Parse the response
			var queryParams = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
			code = queryParams["code"];
			var returnedState = queryParams["state"];

			if (state != returnedState) {
				throw new Exception("Invalid state parameter");
			}
		} finally {
			listener.Stop();
		}

		// Exchange code for token
		using (var client = new HttpClient()) {
			var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string> {
							 { "grant_type", "authorization_code" },
							 { "client_id", clientId },
							 { "code_verifier", codeVerifier },
							 { "code", code },
							 { "redirect_uri", redirectUri }
					 });

			var tokenResponse = await client.PostAsync(
					$"https://{domain}/oauth/token",
					tokenRequest
			);

			var jsonResponse = await tokenResponse.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<TokenResponse>(jsonResponse, new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			});
		}
	}

	private static string GenerateRandomString(int length = 32) {
		var bytes = new byte[length];
		using (var rng = RandomNumberGenerator.Create()) {
			rng.GetBytes(bytes);
		}
		return Convert.ToBase64String(bytes)
				.TrimEnd('=')
				.Replace('+', '-')
				.Replace('/', '_');
	}

	private static string GenerateCodeChallenge(string codeVerifier) {
		using (var sha256 = SHA256.Create()) {
			var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
			return Convert.ToBase64String(challengeBytes)
					.TrimEnd('=')
					.Replace('+', '-')
					.Replace('/', '_');
		}
	}
}

public class CustomAuth0Client : Auth0ClientBase {
	public CustomAuth0Client(Auth0ClientOptions options) : base(options, "dotnet") {
		// Modify the options before passing to base
	}
}

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

	//private static string DecryptJWE(string encryptedToken) {
	//	try {
	//		// Split the token to get the parts
	//		var parts = encryptedToken.Split('.');
	//		if (parts.Length != 5) {
	//			throw new Exception("Invalid JWE token format");
	//		}

	//		// Get your encryption key from Auth0 (you'll need to get this from Auth0)
	//		var key = "YOUR_ENCRYPTION_KEY"; // This needs to be provided by Auth0

	//		// Decrypt the token
	//		var decrypted = Jose.JWT.Decode(
	//				encryptedToken,
	//				Encoding.UTF8.GetBytes(key),
	//				JweAlgorithm.DIR,
	//				JweEncryption.A256GCM
	//		);

	//		return decrypted;
	//	} catch (Exception ex) {
	//		Console.WriteLine($"Error decrypting token: {ex.Message}");
	//		throw;
	//	}
	//}

	public static async Task ValidateLicese(string licenseKey, string accessToken) {
		// 4. Call the Node/Platformatic endpoint
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Authorization =
				new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

		var response = await httpClient.PostAsJsonAsync(
			$"{Constas.ABS_PLATFORMATIC_BASE_URL}/license/activate",
			new { licenseKey }
		);
		var body = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode) {
			Console.WriteLine("License activation success: " + body);
		} else {
			throw new InvalidOperationException($"License activation error ({response.StatusCode}): " + body);
		}
	}
}

// Extension method to handle base64url padding
public static class Base64Extensions {
	public static string PadBase64(this string base64) {
		var padding = 3 - ((base64.Length + 3) % 4);
		if (padding == 0) return base64;
		return base64 + new string('=', padding);
	}
}
