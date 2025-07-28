using System.Net;
using Chameleon.lib.Helpers;
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
	public string? isp { get; set; }
	public string? org { get; set; }
	public string? _as { get; set; }
	public string? query { get; set; }
	public string? proxy { get; set; }
	public bool system { get; set; }
	public string? locale => $"en-{countryCode}"; 
	public string? zone => timezone; 
}

public class Api {
	public static async Task<Ipapi?> GeoIp(WebProxy? proxy) {
		Toaster.Info($"Requesting timezone/geo data for {proxy?.Address?.Host ?? "local"}");
		var timeout = 3;
		var response = await EX.Poly(
			async () => {
				using var client = new HttpClient(new HttpClientHandler { Proxy = proxy }) {
					Timeout = TimeSpan.FromSeconds(timeout)
				};
				var response = await client.GetAsync("http://ip-api.com/json");
				response.EnsureSuccessStatusCode();
				return await response.Content.ReadAsStringAsync();
			},
			new(e => {
				timeout *= 2;
				Toaster.Error($"{e.Message}", $"Retrying with {timeout} second timeout...");
				return Task.CompletedTask;
			}, retries: 2)
		);
		if (JSON.Deserialize<Ipapi>(response ?? "") is not { } ipapi)
			throw new Exception("Failed to retrieve IP data.");

		Toaster.Info(
			$"IP {ipapi.query}: {ipapi.city} {ipapi.country} ({ipapi.countryCode})\n" +
			$"({ipapi.lat}, {ipapi.lon}) - {ipapi.timezone}");
		return ipapi;
	}
}
