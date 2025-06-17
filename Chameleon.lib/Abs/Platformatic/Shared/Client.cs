using Chameleon.lib.Auth;
using Chameleon.lib.Util;
using System.Net;
using System.Net.Http.Json;

namespace Chameleon.lib.Abs.Platformatic.Shared;
public class Client {
	Client() { }
	public record Request(
		string? Q = null,
		object? Body = null,
		bool EnsureSuccess = true,
		bool Authenticate = true,
		HttpCompletionOption CompletionOption = HttpCompletionOption.ResponseContentRead,
		Dictionary<string,string>? Headers = null
	) {
		public HttpContent? Content => Body == null ? null
			: JsonContent.Create(Body, mediaType: null, JSON.InsensitiveCamelCaseOptions);
	}
	public record ReqError(string Error, string Message);

	//
	public string AddressUri { get; } =
		#if DEBUG
					//"http://127.0.0.1:3042"
					"https://chameleon-ws.onrender.com"
		#else
					"https://chameleon-ws.onrender.com"
		#endif
	;
	public HttpClient HttpClient => new(new HttpClientHandler {
		AutomaticDecompression = DecompressionMethods.GZip,
		ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
	}) {
		BaseAddress = new Uri(AddressUri)
	};

	internal async Task<T?> SendRequestAsync<T>(HttpMethod method, string path, Request req) {
		using var client = this.HttpClient;
		if (req.Authenticate) {
			var (auth0client, authentication) = await Session.Instance.Authenticate();
			client.DefaultRequestHeaders.Authorization = authentication;
			client.DefaultRequestHeaders.Add("x-auth0-identity", $"identity {auth0client.Token?.id_token}");
		}
		foreach (var header in req.Headers ?? []) client.DefaultRequestHeaders.Add(header.Key, header.Value);

		var requestUri = $"{path}{req.Q ?? ""}";
		using var response = await client.SendAsync(new HttpRequestMessage(method, requestUri) {
			Content = req.Content
		}, req.CompletionOption);

		if (req.CompletionOption == HttpCompletionOption.ResponseHeadersRead) {
			_ = response.EnsureSuccessStatusCode();
			return typeof(T) == typeof(HttpResponseMessage) ?
				(T)(object)await response.Content.ReadAsStreamAsync()
				: default;
		}

		var content = await response.Content.ReadAsStringAsync();
		return
			response.IsSuccessStatusCode ? JSON.Deserialize<T>(content)
			: req.EnsureSuccess == true
				? throw new HttpRequestException($"{method} {requestUri}: \n{response.StatusCode}\n" +
					(
						JSON.Deserialize<ReqError>(content) is ReqError err
							? $"{err.Error}\n{err.Message}" : content
					)
				)
				: default;
	}

	//
	public static Client Instance { get; } = new();
}