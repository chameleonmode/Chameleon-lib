using System.Net;

using IdentityModel.OidcClient.Browser;
using Chameleon.lib.Util;
using Chameleon.lib.Const;
using System.Text.Json;
using System.Text;

namespace Chameleon.lib.Auth.Oidc;
public class BrowserAuth {
	const string domain = "dev-gcjhdlkot8s8v2vr.us.auth0.com";
	const string clientId = "dEtvplqXMKlDV1xSuuPfTLoWxtR8uMJv";
	const string redirectUri = "http://127.0.0.1:7891/callback";
	const string audience = "https://api.chameleonmode.com/";
	const string responseHtml = @"
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

	readonly string state; 
	readonly string codeVerifier; 
	readonly string codeChallenge;

	public BrowserAuth() {
		// Generate state and PKCE values
		state = StringsUtil.GenerateRandomString();
		codeVerifier = StringsUtil.GenerateRandomString();
		codeChallenge = StringsUtil.GenerateCodeChallenge(codeVerifier);
	}

	/// <summary>
	/// Get the authorization code from the user
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public async Task<string> GetCode() {
		// Start local server to receive callback
		using var listener = new HttpListener();
		listener.Prefixes.Add(redirectUri + "/");
		listener.Start();

		// Construct authorization URL & Open browser
		var authUrl = $"https://{domain}/authorize?" +
				$"response_type=code&" +
				$"client_id={clientId}&" +
				$"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
				$"scope=openid%20profile%20email&" +
				$"audience={Uri.EscapeDataString(audience)}&" +
				$"state={state}&" +
				$"code_challenge={codeChallenge}&" +
				$"code_challenge_method=S256";
		ProcessUtil.OpenBrowser(authUrl);

		// Wait for the callback
		var context = await listener.GetContextAsync();

		// Send a nice HTML response
		using var response = context.Response;
		var buffer = Encoding.UTF8.GetBytes(responseHtml);
		response.ContentLength64 = buffer.Length;
		response.ContentType = "text/html";
		await response.OutputStream.WriteAsync(buffer);

		// Parse the request URL
		var request = context.Request;
		ArgumentNullException.ThrowIfNull(request.Url, "request.Url");
		var queryParams = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
		return queryParams["code"] ?? throw new Exception("Code not found in response");
	}

	/// <summary>
	/// Exchange the code for a token
	/// </summary>
	/// <param name="code"></param>
	/// <returns></returns>
	public async Task<TokenResponse?> GetToken(string code) {
		// Exchange code for token
		using var client = new HttpClient();
		var tokenResponse = await client.PostAsync(
			$"https://{domain}/oauth/token",
			new FormUrlEncodedContent(new Dictionary<string, string> {
				{ "grant_type", "authorization_code" },
				{ "client_id", clientId },
				{ "code_verifier", codeVerifier },
				{ "code", code },
				{ "redirect_uri", redirectUri }
			})
		);
		var jsonResponse = await tokenResponse.Content.ReadAsStringAsync();

		// Deserialize the JSON response
		return JsonSerializer.Deserialize<TokenResponse>(jsonResponse, JS.CaseInsensitiveOptions);
	}
}

/// <summary>
/// Construct a system browser that listens on http://127.0.0.1:{port}/{path}
/// </summary>
/// <param name="port">The TCP port to listen on.</param>
/// <param name="path">Optional path component (e.g., "callback").</param>
public class OidcSystemBrowser(string redirectUrl) : IBrowser {

	public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default) {
		// 1. Create an HTTP listener to wait for the OAuth redirect
		using var listener = new HttpListener();
		listener.Prefixes.Add(redirectUrl.EndsWith('/') ? redirectUrl : redirectUrl + "/");
		listener.Start();

		// 2. Launch the system's default browser to the authorize URL
		try {
			ProcessUtil.OpenBrowser(options.StartUrl);
		} catch (Exception ex) {
			return new BrowserResult {
				ResultType = BrowserResultType.UnknownError,
				Error = ex.Message
			};
		}

		// 3. Wait for the incoming HTTP request from the IdP
		try {
			var context = await listener.GetContextAsync();
			var request = context.Request;
			var response = context.Response;

			// 4. Construct a minimal HTML response (so the user sees a message)
			var responseString = "<html><head><meta charset='utf-8'/></head><body>Authentication complete. You can close this window.</body></html>";
			var responseBytes = System.Text.Encoding.UTF8.GetBytes(responseString);
			response.ContentLength64 = responseBytes.Length;
			await response.OutputStream.WriteAsync(responseBytes, cancellationToken);
			response.OutputStream.Close();

			// 5. The authorization code & state are in request.Url
			var url = request.Url?.ToString();

			// 6. Return success with the final redirect URL
			return new BrowserResult {
				ResultType = BrowserResultType.Success,
				Response = url
			};
		} catch (Exception ex) {
			return new BrowserResult {
				ResultType = BrowserResultType.UnknownError,
				Error = ex.Message
			};
		} finally {
			listener.Stop();
		}
	}
}