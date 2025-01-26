using Chameleon.lib.Const;
using System.Net;
using Chameleon.lib.Auth;
using System.Net.Http.Json;

namespace Chameleon.lib.Abs;

public class AbsClient(string baseUrl) {
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
			object? body = null) {
		httpClient.DefaultRequestHeaders.Authorization = Session.Instance.Authorization;

		using var response = await httpClient.SendAsync(new HttpRequestMessage(method, requestUri) {
			Content = body != null
				? JsonContent.Create(body, mediaType: null, JS.InsensitiveCamelCaseOptions)
				: null
		});
		var content = await response.Content.ReadAsStringAsync();

		return !response.IsSuccessStatusCode
			? throw new HttpRequestException($"{method} {requestUri} returned {response.StatusCode}: " + content)
			: response.StatusCode == HttpStatusCode.NoContent
				? default
				: JS.DeserializeSafely<T>(content)
								?? throw new InvalidOperationException("Response is unreadable");
	}

	// Public methods
	public async Task<T?> GetAsync<T>(string requestUri) =>
		await SendRequestAsync<T>(HttpMethod.Get, requestUri);

	public async Task<T?> PostAsync<T>(string requestUri, object body) =>
		await SendRequestAsync<T>(HttpMethod.Post, requestUri, body);

	public async Task<T?> PutAsync<T>(string requestUri, object body) =>
		await SendRequestAsync<T>(HttpMethod.Put, requestUri, body);

	public async Task<T?> DeleteAsync<T>(string requestUri) =>
		await SendRequestAsync<T>(HttpMethod.Delete, requestUri);
}
