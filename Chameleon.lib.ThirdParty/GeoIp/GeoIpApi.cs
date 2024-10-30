using System.Net;
using System.Text.Json;

using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Models;

using Polly;

namespace Chameleon.lib.ThirdParty.GeoIp;

public class GeoIpApi {
	public static GeoIpApi Instance { get; } = new GeoIpApi();

	public static async Task<Ipapi?> GetIpapi(string proxyUrl, Action<string> onretry, string? proxyUsername = null, string? proxyPassword = null) =>
		JsonSerializer.Deserialize<Ipapi>(await GetIPApi(proxyUrl, onretry, proxyUsername, proxyPassword));
	public static Task<string> GetIPApi(string proxyUrl, Action<string> onretry, string? proxyUsername = null, string? proxyPassword = null)
					=> GetHttpResponseContent(proxyUrl, "http://ip-api.com/json", onretry, proxyUsername, proxyPassword);

	private static async Task<string> GetHttpResponseContent(string proxyUrl, string requestUri, Action<string> onretry, string? proxyUsername = null, string? proxyPassword = null)
	{
		var handler = await InitializeHttpClientHandlerWithRetry(proxyUrl, proxyUsername, proxyPassword, onretry);
		HttpClient client = new(handler) {
			Timeout = TimeSpan.FromSeconds(15)
		};

		try {
			var response = await Policy.WrapAsync(
					Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode >= HttpStatusCode.InternalServerError).Or<HttpRequestException>()
							.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(retryAttempt), (outcome, timespan, retryAttempt, context) => {
								onretry($"Timezone Request from proxy failed. Retry {retryAttempt} for {context.PolicyKey} at {context.OperationKey}: due to {outcome.Exception?.Message} {outcome.Result?.StatusCode}");
							}),
					Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode).Or<HttpRequestException>()
							.CircuitBreakerAsync(
									handledEventsAllowedBeforeBreaking: 4,
									durationOfBreak: TimeSpan.FromSeconds(3)
							)).ExecuteAsync(() => client.GetAsync(requestUri));

			if (response.IsSuccessStatusCode) {
				var responseBody = await response.Content.ReadAsStringAsync();
				return responseBody;
			} else {
				throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
			}
		} finally {
			client.Dispose();
		}
	}

	private static async Task<HttpClientHandler> InitializeHttpClientHandlerWithRetry(string proxyUrl, string? proxyUsername, string? proxyPassword, Action<string> onretry) =>
		await Policy.Handle<WebException>()
				.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(retryAttempt), (exception, timespan, retryAttempt, context) => {
					onretry($"Proxy initialization failed. Retry {retryAttempt}: due to {exception.Message}");
				})
				.ExecuteAsync(() => {
					var handler = new HttpClientHandler {
						Proxy = new WebProxy(proxyUrl)
					};
					if (proxyUsername?.Is() == true && proxyPassword?.Is() == true)
						handler.Proxy.Credentials = new NetworkCredential(proxyUsername, proxyPassword);
					return Task.FromResult(handler);
				});
}
