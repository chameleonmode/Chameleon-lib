using Chameleon.lib.Auth;
using Chameleon.lib.Const;
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
		HttpCompletionOption CompletionOption = HttpCompletionOption.ResponseContentRead
	) {
		public HttpContent? Content => Body == null ? null
			: JsonContent.Create(Body, mediaType: null, JS.InsensitiveCamelCaseOptions);
	}
	public record ReqError(string Error, string Message);
	//
	public HttpClient HttpClient { get; } = new(new HttpClientHandler {
		AutomaticDecompression = DecompressionMethods.GZip,
		ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
	}) {
		BaseAddress = new Uri(
#if DEBUG
					"http://127.0.0.1:3042"
#else
					"https://chameleon-ws.onrender.com"
#endif
			)
	};

	//
	private async Task<T?> SendRequestAsync<T>(HttpMethod method, string path, Request @params) {
		if (@params.Authenticate) {
			var (auth0client, authentication) = await Session.Instance.Authenticate();
			HttpClient.DefaultRequestHeaders.Authorization = authentication;
			HttpClient.DefaultRequestHeaders.Add("x-auth0-identity", $"identity {auth0client.Token?.id_token}");
		}

		var requestUri = $"{path}{@params.Q ?? ""}";
		using var response = await HttpClient.SendAsync(new HttpRequestMessage(method, requestUri) {
			Content = @params.Content
		}, @params.CompletionOption);

		if (@params.CompletionOption == HttpCompletionOption.ResponseHeadersRead) {
			_ = response.EnsureSuccessStatusCode();
			return typeof(T) == typeof(HttpResponseMessage) ?
				(T)(object)await response.Content.ReadAsStreamAsync()
				: default;
		}

		var content = await response.Content.ReadAsStringAsync();
		return
			response.IsSuccessStatusCode ? JS.DeserializeSafely<T>(content) 
			: @params.EnsureSuccess == true
				? throw new HttpRequestException($"{method} {requestUri}: \n{response.StatusCode}\n" +
					(
						JS.DeserializeSafely<ReqError>(content) is ReqError err
							? $"{err.Error}\n{err.Message}" : content
					)
				)
				: default;
	}

	//
	public Task<T?> Get<T>(string path, Request? @params = null) =>
		SendRequestAsync<T>(HttpMethod.Get, path, @params ??= new());
	public Task<T?> Post<T>(string path, Request @params) =>
		SendRequestAsync<T>(HttpMethod.Post, path, @params);
	public Task<T?> Put<T>(string path, Request @params) =>
		SendRequestAsync<T>(HttpMethod.Put, path, @params);
	public Task<T?> Delete<T>(string path, Request? @params = null) =>
		SendRequestAsync<T>(HttpMethod.Delete, path, @params ??= new());

	public static Client Instance { get; } = new();
}