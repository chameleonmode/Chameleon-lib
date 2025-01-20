using System.Net;

using IdentityModel.OidcClient.Browser;
using Chameleon.lib.Util;

namespace Chameleon.lib.Auth.Oidc;
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