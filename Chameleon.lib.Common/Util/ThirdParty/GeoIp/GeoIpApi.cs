using System.Net;
using System.Text.Json;

using Chameleon.lib.Common.Models;

using Polly;

namespace Chameleon.lib.Common.Util.ThirdParty.GeoIp;

public class GeoIpApi {
	public static GeoIpApi Instance { get; } = new GeoIpApi();

	public static async Task<Ipapi?> GetIpapi(SysBrowserProxy proxy, Action<string> onretry) =>
		JsonSerializer.Deserialize<Ipapi>(await GetIPApi(proxy, onretry));
	public static Task<string> GetIPApi(SysBrowserProxy proxy, Action<string> onretry)
					=> GetHttpResponseContent(proxy, "http://ip-api.com/json", onretry);

	private static async Task<string> GetHttpResponseContent(SysBrowserProxy proxy, string requestUri, Action<string> onretry)
	{
		var handler = await InitializeHttpClientHandlerWithRetry(proxy, onretry);
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

	private static async Task<HttpClientHandler> InitializeHttpClientHandlerWithRetry(SysBrowserProxy proxy, Action<string> onretry) =>
		await Policy.Handle<WebException>()
				.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(retryAttempt), (exception, timespan, retryAttempt, context) => {
					onretry($"Proxy initialization failed. Retry {retryAttempt}: due to {exception.Message}");
				})
				.ExecuteAsync(() => {
					var handler = new HttpClientHandler {
						Proxy = new WebProxy(proxy.ServerForRequest)
					};
					if (proxy.UserName?.Is() == true && proxy.Password?.Is() == true)
						handler.Proxy.Credentials = new NetworkCredential(proxy.UserName, proxy.Password);
					return Task.FromResult(handler);
				});
}
