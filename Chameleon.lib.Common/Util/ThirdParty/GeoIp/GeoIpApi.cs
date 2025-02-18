using System.Net;
using System.Text.Json;

using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Models;
using Chameleon.lib.Helpers;
using Chameleon.lib.Util;

namespace Chameleon.lib.Common.Util.ThirdParty.GeoIp;

public class GeoIpApi {
	public static async Task<Ipapi?> GetIpapi(SysBrowserProxy proxy, Action<string> onretry) =>
		JsonSerializer.Deserialize<Ipapi>(await GetIPApi(proxy, onretry));

	private static Task<string> GetIPApi(SysBrowserProxy proxy, Action<string> onretry) {
		Toaster.Info($"Requesting timezone/geo data for {proxy.Host ?? "local"}");
		return GetHttpResponseContent(proxy, "http://ip-api.com/json", onretry);
	}
	private static async Task<string> GetHttpResponseContent(SysBrowserProxy proxy, string requestUri, Action<string> onretry) {
		HttpClient client = new(new HttpClientHandler {
			Proxy = proxy.ServerForRequest != null 
			? new WebProxy(proxy.ServerForRequest) {
				Credentials = proxy.UserName?.IsNot() == true && proxy.Password?.IsNot() == true 
				? new NetworkCredential(proxy.UserName, proxy.Password)
				: CredentialCache.DefaultNetworkCredentials
			} : null
		});

		var httpClientTimeoutInSeconds = 5;
		try {
			return await PolyUtil.RetryWithPolicyAsync(async () => {
				client.Timeout = TimeSpan.FromSeconds(httpClientTimeoutInSeconds);
				var response = await client.GetAsync(requestUri);
				if (response.IsSuccessStatusCode) {
					var responseBody = await response.Content.ReadAsStringAsync();
					return responseBody;
				} else {
					throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
				}
			},
			OnError: (e, i) => {
				httpClientTimeoutInSeconds *= i + 1;
				onretry($"Timezone Request from proxy failed. Retrying {i}");
			});
		} finally {
			client.Dispose();
		}
	}
}
