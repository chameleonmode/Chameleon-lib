using Chameleon.lib.Const;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Chameleon.lib.Auth.Oidc;

namespace Chameleon.lib.Abs;
public class AbsClient(string baseUrl, Func<Task<(OidcAuth0Client, AuthenticationHeaderValue)>> authorization) {
	private readonly HttpClient httpClient = new(new HttpClientHandler {
		AutomaticDecompression = DecompressionMethods.GZip,
		ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
	}) {
		BaseAddress = new Uri(baseUrl)
	};

	//
	private async Task<T?> SendRequestAsync<T>(
			HttpMethod method,
			string requestUri,
			object? body = null,
			string? q = null,
			bool ensureSuccess = true) {
		var (auth0client, authentication) = await authorization();
		httpClient.DefaultRequestHeaders.Authorization = authentication;
		httpClient.DefaultRequestHeaders.Add("x-auth0-identity", $"identity {auth0client.Token?.id_token}");

		using var response = await httpClient.SendAsync(new HttpRequestMessage(method, $"{requestUri}{q ?? ""}") {
			Content = body == null ? null
				: JsonContent.Create(body, mediaType: null, JS.InsensitiveCamelCaseOptions)
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

	//
	public Task<T?> Get<T>(string requestUri, string? query = null, bool throwsOnFail = true) =>
		SendRequestAsync<T>(HttpMethod.Get, requestUri, ensureSuccess: throwsOnFail, q: query);

	public Task<T?> Post<T>(string requestUri, object body) =>
		SendRequestAsync<T>(HttpMethod.Post, requestUri, body);

	public Task<T?> Put<T>(string requestUri, object body) =>
		SendRequestAsync<T>(HttpMethod.Put, requestUri, body);

	public Task<T?> Delete<T>(string requestUri) =>
		SendRequestAsync<T>(HttpMethod.Delete, requestUri);
}