using Chameleon.lib.Const;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Chameleon.lib.Auth.Oidc;

namespace Chameleon.lib.Abs;

public record Params(string? Q = null, object? Body = null, bool EnsureSuccess = true) {
	public HttpContent? Content => Body == null ? null
		: JsonContent.Create(Body, mediaType: null, JS.InsensitiveCamelCaseOptions);
}

public class AbsClient(string baseUrl, Func<Task<(OidcAuth0Client, AuthenticationHeaderValue)>> authorization) {
	private readonly HttpClient httpClient = new(new HttpClientHandler {
		AutomaticDecompression = DecompressionMethods.GZip,
		ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
	}) {
		BaseAddress = new Uri(baseUrl)
	};

	//
	private async Task<T?> SendRequestAsync<T>(HttpMethod method, string path, Params? @params) {
		var (auth0client, authentication) = await authorization();
		httpClient.DefaultRequestHeaders.Authorization = authentication;
		httpClient.DefaultRequestHeaders.Add("x-auth0-identity", $"identity {auth0client.Token?.id_token}");

		var requestUri = $"{path}{@params?.Q ?? ""}";
		using var response = await httpClient.SendAsync(new HttpRequestMessage(method, requestUri) {
			Content = @params?.Content
		});
		var content = await response.Content.ReadAsStringAsync();

		return
			response.IsSuccessStatusCode ?
				response.StatusCode != HttpStatusCode.NoContent
				? JS.DeserializeSafely<T>(content) ?? throw new InvalidOperationException("Response is unreadable")
				: default
			: @params?.EnsureSuccess == true
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
		SendRequestAsync<T>(HttpMethod.Get, path, @params);
	public Task<T?> Post<T>(string path, Params @params) =>
		SendRequestAsync<T>(HttpMethod.Post, path, @params);
	public Task<T?> Put<T>(string path, Params @params) =>
		SendRequestAsync<T>(HttpMethod.Put, path, @params);
	public Task<T?> Delete<T>(string path, Params? @params = null) =>
		SendRequestAsync<T>(HttpMethod.Delete, path, @params);
}