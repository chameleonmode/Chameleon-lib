using System.Net;
using Chameleon.lib.Util;

namespace Chameleon.lib.ThirdParty.GeoIp;
//https://geoip-lookup.vercel.app/api/geoip
public class Geoiplookup {
	public bool success { get; set; }
	public string? ip { get; set; }
	public string? timezone { get; set; }
	public string? languages { get; set; }
}
//http://ip-api.com/json
public class Ipapi {
	public string? status { get; set; }
	public string? country { get; set; }
	public string? countryCode { get; set; }
	public string? region { get; set; }
	public string? regionName { get; set; }
	public string? city { get; set; }
	public string? zip { get; set; }
	public double lat { get; set; }
	public double lon { get; set; }
	public string? timezone { get; set; }
	public bool tzSystem { get; set; }
	public string? isp { get; set; }
	public string? org { get; set; }
	public string? _as { get; set; }
	public string? query { get; set; }
	public string? proxy { get; set; }
}

public class GeoIpApi {
	public static async Task<Ipapi?> GetIpapi(WebProxy? proxy, Action<string> onretry) =>
		JSON.Deserialize<Ipapi>(await GetHttpResponseContent(proxy, "http://ip-api.com/json", onretry) ?? "");

	private static async Task<string?> GetHttpResponseContent(WebProxy? proxy, string requestUri, Action<string> onretry) {
		HttpClient client = new(new HttpClientHandler {
			Proxy = proxy,
		});

		var httpClientTimeoutInSeconds = 5;
		try {
			return await Exceptionz.Policy(
				async () => {
					client.Timeout = TimeSpan.FromSeconds(httpClientTimeoutInSeconds);
					var response = await client.GetAsync(requestUri);
					return response.IsSuccessStatusCode
						? await response.Content.ReadAsStringAsync()
						: throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
				},
				caught: (e, i) => {
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
