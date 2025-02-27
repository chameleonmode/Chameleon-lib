using Chameleon.lib.Auth;
using Chameleon.lib.Const;
using System.Net;
using System.Net.Http.Json;

namespace Chameleon.lib.Abs.Platformatic;
public class Client {
  public record Response<T>(T Payload);
	public record Params(
		string? Q = null,
		object? Body = null,
		bool EnsureSuccess = true,
		bool Authorize = true,
		HttpCompletionOption CompletionOption = HttpCompletionOption.ResponseContentRead
	) {
		public HttpContent? Content => Body == null ? null
			: JsonContent.Create(Body, mediaType: null, JS.InsensitiveCamelCaseOptions);
	}
	
	Client() { }
	public HttpClient HttpClient { get; } = new(new HttpClientHandler {
		AutomaticDecompression = DecompressionMethods.GZip,
		ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
	}) {
		BaseAddress = new Uri(Configs.Urls.ABS_PLATFORMATIC_BASE_URL)
	};


	//
	private async Task<T?> SendRequestAsync<T>(HttpMethod method, string path, Params @params) {
		if (@params.Authorize) {
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
			response.IsSuccessStatusCode ?
				response.StatusCode != HttpStatusCode.NoContent
				? JS.DeserializeSafely<T>(content) ?? throw new InvalidOperationException("Response is unreadable")
				: default
			: @params.EnsureSuccess == true
				? throw new HttpRequestException($"{method} {requestUri}: \n{response.StatusCode}\n" +
					(
						JS.DeserializeSafely<PlatformaticReqError>(content) is PlatformaticReqError err
							? $"{err.error}\n{err.message}" : content
					)
				)
				: default;
	}

	//
	public Task<T?> Get<T>(string path, Params? @params = null) =>
		SendRequestAsync<T>(HttpMethod.Get, path, @params ??= new());
	public Task<T?> Post<T>(string path, Params @params) =>
		SendRequestAsync<T>(HttpMethod.Post, path, @params);
	public Task<T?> Put<T>(string path, Params @params) =>
		SendRequestAsync<T>(HttpMethod.Put, path, @params);
	public Task<T?> Delete<T>(string path, Params? @params = null) =>
		SendRequestAsync<T>(HttpMethod.Delete, path, @params ??= new());

	public static Client Instance { get; } = new();
}