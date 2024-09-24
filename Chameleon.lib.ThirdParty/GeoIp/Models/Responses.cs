using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chameleon.lib.ThirdParty.GeoIp.Models;
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
	public float lat { get; set; }
	public float lon { get; set; }
	public string? timezone { get; set; }
	public string? isp { get; set; }
	public string? org { get; set; }
	public string? _as { get; set; }
	public string? query { get; set; }
}
