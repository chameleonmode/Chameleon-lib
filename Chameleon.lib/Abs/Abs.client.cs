using Chameleon.lib.Const;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Web;

namespace Chameleon.lib.Abs;

public class AbsClient(string baseUrl, Func<Task<AuthenticationHeaderValue>> authorization) {
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
		httpClient.DefaultRequestHeaders.Authorization = await authorization();

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
			!response.IsSuccessStatusCode ? ensureSuccess
				? JS.DeserializeSafely<PlatformaticReqError>(content) is PlatformaticReqError err 
					? throw new Exception($"{method} {requestUri} {err.statusCode}: \n{err.error}\n{err.message}")
					: throw new HttpRequestException($"{method} {requestUri} returned {response.StatusCode}: " + content)
				: default
			: response.StatusCode == HttpStatusCode.NoContent
				? default
				: JS.DeserializeSafely<T>(content)
						?? throw new InvalidOperationException("Response is unreadable");
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
