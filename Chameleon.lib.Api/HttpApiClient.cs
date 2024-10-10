using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Chameleon.lib.Common.Constants;

using Polly;
using Polly.Wrap;

namespace Chameleon.lib.Api;
public class HttpApiClient {
	private readonly HttpClient _httpClient = new(new HttpClientHandler {
		AutomaticDecompression = DecompressionMethods.GZip,
		ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
	});
	private readonly JsonSerializerOptions options = new() {
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};
	public event Action<string>? OnRetry;
	public event Action<string>? OnCircuitBreaker;
	public event Func<Task>? OnAuthError;

	public string AuthToken { get; set; } = string.Empty;

	public AuthenticationHeaderValue Authorization => new("Bearer", AuthToken);
	public AsyncPolicyWrap<HttpResponseMessage> AsyncPolicyWrap { get; } = Policy.WrapAsync([
		Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode >= HttpStatusCode.InternalServerError)
			.Or<HttpRequestException>()
			.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (outcome, timespan, retryAttempt, context) => {
				Instance.OnRetry?.Invoke($"Request Failed: Retry {retryAttempt}: {outcome.Result?.StatusCode}");
			}),
		Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Unauthorized)
			.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), async (outcome, timespan, retryAttempt, context) => {
				if(Instance.OnAuthError != null)
				await Instance.OnAuthError();
			}),
		Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
			.Or<HttpRequestException>()
			.CircuitBreakerAsync(
					handledEventsAllowedBeforeBreaking: 2,
					durationOfBreak: TimeSpan.FromSeconds(5),
					onBreak: (outcome, breakDelay) => {
						// Log the circuit breaker opening
						Instance.OnCircuitBreaker?.Invoke($"Circuit breaker opened due to: {outcome.Exception?.Message ?? outcome.Result.ReasonPhrase}");
					},
					onReset: () => {
						// Log the circuit breaker resetting
						Instance.OnCircuitBreaker?.Invoke("Circuit breaker reset.");
					},
					onHalfOpen: () => {
						// Log the circuit breaker half-open state
						Instance.OnCircuitBreaker?.Invoke("Circuit breaker is half-open.");
					})]);


	public async Task<TResponse> Post<TResponse>(string path, object? body = default)
	{
		var response = await Send(() => Build(HttpMethod.Post, path, body));
		return await Read<TResponse>(response);
	}

	public async Task<TResponse> Get<TResponse>(string path, object? body = default)
	{
		var response = await Send(() => Build(HttpMethod.Get, path, body));
		return await Read<TResponse>(response);
	}

	private Task<HttpResponseMessage> Send(Func<HttpRequestMessage> @request)
	{
		return AsyncPolicyWrap.ExecuteAsync(() => {
			return _httpClient.SendAsync(@request());
		});
	}

	private async Task<T> Read<T>(HttpResponseMessage response)
	{
		_ = response.EnsureSuccessStatusCode();

		var responseContent = JsonSerializer.Deserialize<RootResponse<T>>(await response.Content.ReadAsStringAsync(), options);
		return responseContent is null
			? throw new InvalidDataException($"Response could not be determined for {nameof(RootResponse<T>)}")
			: responseContent.result is null 
			? throw new InvalidDataException($"Response could not be determined for {nameof(T)}") : 
			responseContent.result;
	}

	private HttpRequestMessage Build(HttpMethod method, string path, object? body = default)
	{
		var request = new HttpRequestMessage(method, Consts.ApiBaseUrl + path);
		request.Headers.Authorization = Authorization;
		if (body != null) {
			request.Content = new StringContent(JsonSerializer.Serialize(body, options), Encoding.UTF8, "application/json");
		}
		return request;
	}
	public static HttpApiClient Instance { get; } = new HttpApiClient();
	private HttpApiClient() { }
}
