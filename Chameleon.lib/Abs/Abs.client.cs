using Chameleon.lib.Const;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Chameleon.lib.Auth.Oidc;

namespace Chameleon.lib.Abs;

public class AbsClient(string baseUrl, Func<Task<(OidcAuth0Client auth0client, AuthenticationHeaderValue authentication)>> authorization) {
	// Private fields
	private readonly HttpClient httpClient = new(new SocketsHttpHandler {
		PooledConnectionLifetime = TimeSpan.FromMinutes(2),
		KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
		AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
	}) {
		BaseAddress = new Uri(baseUrl)
	};

	// Private methods
	private async Task<T?> SendRequestAsync<T>(
			HttpMethod method,
			string requestUri,
			object? body = null,
			string? q = null,
			bool ensureSuccess = true) {
		var (auth0client, authentication) = await authorization();
		httpClient.DefaultRequestHeaders.Authorization = authentication;
		httpClient.DefaultRequestHeaders.Add("x-auth0-identity", $"identity {auth0client.Token?.id_token}");

		if (q != null) {
			requestUri += q;
		}

		using var response = await httpClient.SendAsync(new HttpRequestMessage(method, requestUri) {
			Content = body != null
				? JsonContent.Create(body, mediaType: null, JS.InsensitiveCamelCaseOptions)
				: null
		});
		var content = await response.Content.ReadAsStringAsync();

		return
			response.IsSuccessStatusCode ?
				response.StatusCode != HttpStatusCode.NoContent
				? JS.DeserializeSafely<T>(content) ?? throw new InvalidOperationException("Response is unreadable")
				: default
			: ensureSuccess
				? throw new HttpRequestException($"{method} {requestUri}: \n{response.StatusCode}\n" +
					(
						JS.DeserializeSafely<PlatformaticReqError>(content) is PlatformaticReqError err
							? $"{err.error}\n{err.message}" : content
					)
				) 
				: default;
	}

	// Public methods
	public async Task<T?> GetAsync<T>(string requestUri, string? query = null, bool throwsOnFail = true) =>
		await SendRequestAsync<T>(HttpMethod.Get, requestUri, ensureSuccess: throwsOnFail, q: query);

	public async Task<T?> PostAsync<T>(string requestUri, object body) =>
		await SendRequestAsync<T>(HttpMethod.Post, requestUri, body);

	public async Task<T?> PutAsync<T>(string requestUri, object body) =>
		await SendRequestAsync<T>(HttpMethod.Put, requestUri, body);

	public async Task<T?> DeleteAsync<T>(string requestUri) =>
		await SendRequestAsync<T>(HttpMethod.Delete, requestUri);
}
