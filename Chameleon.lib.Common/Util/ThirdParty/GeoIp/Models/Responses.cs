namespace Chameleon.lib.Common.Util.ThirdParty.GeoIp.Models;
//https://geoip-lookup.vercel.app/api/geoip
public class Geoiplookup {
	public bool success { get; set; }
	public string? ip { get; set; }
	public string? timezone { get; set; }
	public string? languages { get; set; }
}
