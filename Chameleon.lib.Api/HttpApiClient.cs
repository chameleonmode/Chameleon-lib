using System.Net;
using System.Text;
using System.Text.Json;
using Chameleon.lib.Api.Dto;

using Chameleon.lib.Util;

namespace Chameleon.lib.Api;

public class HttpApiClient {
	public event Action<string>? OnRetry;
	public event Action<string>? OnCircuitBreaker;
	public event Func<Task>? OnAuthError;
	public event Action<HttpMethod>? OnSendSeccess;

	private readonly HttpClient _httpClient = new(new HttpClientHandler {
		AutomaticDecompression = DecompressionMethods.GZip,
		ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
	});
	private readonly JsonSerializerOptions options = new() {
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	//
	public Task<T> Put<T>(string path, object? body = default) => Send<T>(HttpMethod.Put, path, body);
	public Task<T> Get<T>(string path, object? body = default) => Send<T>(HttpMethod.Get, path, body);
	public Task<T> Post<T>(string path, object? body = default) => Send<T>(HttpMethod.Post, path, body);
	public Task<T> Delete<T>(string path) => Send<T>(HttpMethod.Delete, path);

	private async Task<T> Send<T>(HttpMethod method, string path, object? body = default) {
		var response = await EX.Poly(async () => {
			var request = new HttpRequestMessage(method, "http://18.157.103.1/api/" + path);
			if (Auther.AuthToken.IsNot())
				request.Headers.Authorization = new("Bearer", Auther.AuthToken);

			if (body != null) {
				request.Content = new StringContent(JSON.Serialize(body, options), Encoding.UTF8, "application/json");
			}
			return await _httpClient.SendAsync(request);
		}, new((e) => {
			OnRetry?.Invoke(e.Message);
			if (e.Message.Contains("401")) _ = OnAuthError?.Invoke();
			OnCircuitBreaker?.Invoke(e.Message);
			return Task.CompletedTask;
		}));

		ArgumentNullException.ThrowIfNull(response);
		if (typeof(T) == typeof(RootResult)) return await Read<T>(response!);

		var read = await Read<RootResponse<T>>(response);
		ArgumentNullException.ThrowIfNull(read.result, $"Response could not be determined for {nameof(T)}");
		OnSendSeccess?.Invoke(method);
		return read.result;
	}
	private async Task<T> Read<T>(HttpResponseMessage response) {
		_ = response.EnsureSuccessStatusCode();
		var responseString = await response.Content.ReadAsStringAsync();
		var responseContent = JSON.Deserialize<T>(responseString, options);
		ArgumentNullException.ThrowIfNull(responseContent, $"Response is unserializable for {nameof(T)}");
		return responseContent;
	}

	public static HttpApiClient Instance { get; } = new HttpApiClient();
	private HttpApiClient() { }
}
