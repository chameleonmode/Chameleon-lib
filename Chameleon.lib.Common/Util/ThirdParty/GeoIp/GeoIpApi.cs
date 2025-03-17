using System.Net;

using Chameleon.lib.Common.Models;
using Chameleon.lib.Const;
using Chameleon.lib.Util;

namespace Chameleon.lib.Common.Util.ThirdParty.GeoIp;

public class GeoIpApi {
	public static async Task<Ipapi?> GetIpapi(WebProxy? proxy, Action<string> onretry) =>
		JS.DeserializeSafely<Ipapi>(await GetIPApi(proxy, onretry));

	private static Task<string> GetIPApi(WebProxy? proxy, Action<string> onretry) {
		return GetHttpResponseContent(proxy, "http://ip-api.com/json", onretry);
	}
	private static async Task<string> GetHttpResponseContent(WebProxy? proxy, string requestUri, Action<string> onretry) {
		HttpClient client = new(new HttpClientHandler {
			Proxy = proxy,
		});

		var httpClientTimeoutInSeconds = 5;
		try {
			return await PolyUtil.RetryWithPolicyAsync(
				async () => {
					client.Timeout = TimeSpan.FromSeconds(httpClientTimeoutInSeconds);
					var response = await client.GetAsync(requestUri);
					return response.IsSuccessStatusCode
						? await response.Content.ReadAsStringAsync()
						: throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
				},
				OnError: (e, i) => {
					httpClientTimeoutInSeconds *= i + 1;
					onretry($"Timezone Request from proxy failed. Retrying {i}");
				},
				sleep: 4000
			);
		} finally {
			client.Dispose();
		}
	}
}
