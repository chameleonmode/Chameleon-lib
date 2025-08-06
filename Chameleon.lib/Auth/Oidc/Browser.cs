using System.Net;

using System.Text;
using System.Web;
using Chameleon.lib.Browzio.Services.Browzas;
using Chameleon.lib.Util;

namespace Chameleon.lib.Auth.Oidc;

public class Browser(Client oidcClient) {
	public Func<string, Task<IBrowserInstance?>> Open { get; set; } = async url => {
		var browser = await EX.Catch(
			async () => await Browzio.Browzio.I.Browzas.Open(Browzio.Browzio.Factory.Chrome(new(url))),
			ex => { if (!Browzio.Browzio.Utilities.IsInstalled(Browzio.BrowserType.Chrome)) Processez.OpenBrowser(url); }
		);
		/// Session.I.Auth0Client.OidcBrowser.TaskCompletion?.Task.ContinueWith(_ => browser?.Closee());
		return browser;
	};
	const string authResponseHtml = @"
		<!DOCTYPE html>
		<html>
		<head>
		    <meta charset='utf-8'/>
		    <title>Chameleon</title>
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
            <title>Chameleon</title>
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

	private async Task<string> HandleCallback(string url, string htmlResponse, string? expect = null) {
		var buffer = Encoding.UTF8.GetBytes(htmlResponse);
		using var listener = new HttpListener();
		listener.Prefixes.Add(oidcClient.RedirectUri + "/");
		listener.Start();

		var browser = await Open(url);
		var context = await listener.GetContextAsync();
		var request = context.Request;

		using var response = context.Response;
		response.ContentLength64 = buffer.Length;
		response.ContentType = "text/html";
		await response.OutputStream.WriteAsync(buffer);

		if (browser != null) await browser.Closee();

		return expect == null
			? ""
			: HttpUtility.ParseQueryString(request.Url?.Query!)[expect] ?? throw new Exception($"{expect} not found in response");
	}
}

