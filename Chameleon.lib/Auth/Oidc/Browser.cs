using System.Net;

using Chameleon.lib.Util;
using System.Text;

namespace Chameleon.lib.Auth.Oidc;
public class Browser(Client oidcClient) {
	const string authResponseHtml = @"
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
	const string logoutResponseHtml = @"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'/>
            <title>Logout Complete</title>
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
                <h1>Logout Complete</h1>
                <p>Successfully logged out from Auth0</p>
                <span class='close-text'>You can close this window now</span>
            </div>
            <script>
                setTimeout(() => window.close(), 2000);
            </script>
        </body>
        </html>";
	
	public Task<string> GetCode() => HandleCallback(oidcClient.AuthUrl, authResponseHtml, "code");
	public Task Logout() => HandleCallback(oidcClient.LogoutUrl, logoutResponseHtml);

	private async Task<string> HandleCallback(string url, string htmlResponse, string? expectedParam = null) {
		using var listener = new HttpListener();
		listener.Prefixes.Add(oidcClient.RedirectUri + "/");
		listener.Start();

		ProcessUtil.OpenBrowser(url);

		var context = await listener.GetContextAsync();

		// Send response after extracting the code
		using var response = context.Response;
		var buffer = Encoding.UTF8.GetBytes(htmlResponse);
		response.ContentLength64 = buffer.Length;
		response.ContentType = "text/html";
		await response.OutputStream.WriteAsync(buffer);

		if (expectedParam != null) {
			ArgumentNullException.ThrowIfNull(context.Request.Url, "request.Url");

			var queryParams = System.Web.HttpUtility.ParseQueryString(context.Request.Url.Query);
			return queryParams[expectedParam] 
				?? throw new Exception($"{expectedParam} not found in response");
		}

		return null!;
	}
}

