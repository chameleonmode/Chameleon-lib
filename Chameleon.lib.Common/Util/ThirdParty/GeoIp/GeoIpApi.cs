using System.Net;
using System.Text.Json;

using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Models;
using Chameleon.lib.Util;

namespace Chameleon.lib.Common.Util.ThirdParty.GeoIp;

public class GeoIpApi {
	public static GeoIpApi Instance { get; } = new GeoIpApi();

	public static async Task<Ipapi?> GetIpapi(SysBrowserProxy proxy, Action<string> onretry) =>
		JsonSerializer.Deserialize<Ipapi>(await GetIPApi(proxy, onretry));
	public static Task<string> GetIPApi(SysBrowserProxy proxy, Action<string> onretry)
					=> GetHttpResponseContent(proxy, "http://ip-api.com/json", onretry);

	private static async Task<string> GetHttpResponseContent(
		SysBrowserProxy proxy, string requestUri, Action<string> onretry)
	{
		HttpClient client = new(new HttpClientHandler {
			Proxy = new WebProxy(proxy.ServerForRequest) {
				Credentials = proxy.UserName?.Is() == true && proxy.Password?.Is() == true
				? new NetworkCredential(proxy.UserName, proxy.Password)
				: CredentialCache.DefaultNetworkCredentials
			}
		}) {
			Timeout = TimeSpan.FromSeconds(3)
		};

		try {
			return await PolyUtil.RetryWithPolicyAsync(async () => {
				var response = await client.GetAsync(requestUri);
				if (response.IsSuccessStatusCode) {
					var responseBody = await response.Content.ReadAsStringAsync();
					return responseBody;
				} else {
					throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
				}
			},
			OnError: (e, i) => {
				onretry($"Timezone Request from proxy failed. Retrying {i}");
			});
		} finally {
			client.Dispose();
		}
	}
}
