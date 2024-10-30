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
