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

public class Api {
	public static async Task<Ipapi?> GeoIp(WebProxy? proxy, Action<string> onretry) {
		var timeout = 4;
		var response = await EX.Poly(
			async () => {
				using var client = new HttpClient(new HttpClientHandler { Proxy = proxy }) {
					Timeout = TimeSpan.FromSeconds(timeout)
				};
				var response = await client.GetAsync("http://ip-api.com/json");
				onretry(response.EnsureSuccessStatusCode().StatusCode.ToString());
				return await response.Content.ReadAsStringAsync();
			},
			new(e => {
				timeout *= 2;
				onretry($"Retrying with {timeout} second timout due to: {e.Message}");
				return Task.CompletedTask;
			})
		);
		return response is not null ? JSON.Deserialize<Ipapi>(response) : throw new Exception("Failed to retrieve IP data from Ipapi.");
	}
}
