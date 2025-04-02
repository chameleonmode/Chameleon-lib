using System.Text.Json;

using Chameleon.lib.Common.Constants;


namespace Chameleon.lib.Common.Models.Dto;
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
public class WebrowserDto {
	public bool webRTC { get; set; }
	public bool webGL { get; set; }
	public bool tracking { get; set; }
	public bool flash { get; set; }
	public float canvas { get; set; }
	public int? userAgentId { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
public class UserProfileDto : Interfaces.Dto {
	public int creatorUserId { get; set; }
	public int? folderId { get; set; }
	public bool isFavourite { get; set; }
	public string? notes { get; set; }
	public int proxyId { get; set; }
	public float limitCache { get; set; } = 100;
	public object? youtubeApiKey { get; set; }
	public object? youtubeClientId { get; set; }
	public object? youtubeClientSecret { get; set; }
	public object? wordPressSettings { get; set; }
	public ProxDto proxy { get; set; } = new();
	public WebrowserDto webBrowser { get; set; } = new();
}
public abstract class UP : Interfaces.Dto {
	public int? ProfileId { get; set; }
}
public class UPPersonDto : UP {
	public string? FirstName { get; set; }
	public string? LastName { get; set; }
	public string? MiddleName { get; set; }
	public string? JobTitle { get; set; }
	public string? PhoneNumber { get; set; }
	public string? Email { get; set; }
	public string? BirthPlace { get; set; }
	public string? Notes { get; set; }
	public DateTime BirthDate { get; set; } = DateTimeOffset.Now.AddYears(-20).DateTime;
	public DateTimeOffset BirthDateOffset => new(BirthDate);
	public Enums.GenderType Gender { get; set; } = Enums.GenderType.Female;
	public string Gendertext => Gender.ToString();
}
public class UPBusinessDto : UP {
	public string? CompanyName { get; set; }
	public string? Department { get; set; }
	public string? PhoneNumber { get; set; }
	public string? WebSite { get; set; }
	public string? Notes { get; set; }
}
public class UPAddressDto : UP {
	public int? CountryId { get; set; }
	public string? AddressLine1 { get; set; }
	public string? AddressLine2 { get; set; }
	public string? City { get; set; }
	public string? State { get; set; }
	public string? Zip { get; set; }
	public string? Notes { get; set; }
}
public class UPLoginDto : UP {
	public string? WebSite { get; set; }
	public string? Email { get; set; }
	public string? UserName { get; set; }
	public string? Password { get; set; }
	public string? Notes { get; set; }
}
public class CountryzDto : UP {
	public string? Name { get; set; }
	public bool IsMetric { get; set; }
	public string? IsoCode2 { get; set; }
	public string? IsoCode3 { get; set; }
}

public class CountryzRepo {
	private CountryzRepo() { }

	public List<CountryzDto> Countryz { get; } = JsonSerializer.Deserialize<List<CountryzDto>>(
#pragma warning disable CA1869 // Cache and reuse 'JsonSerializerOptions' instances
"""
		[{"id":93,"name":"Afghanistan","isMetric":true,"isoCode2":"AF","isoCode3":"AFG"},{"id":92,"name":"Albania","isMetric":true,"isoCode2":"AL","isoCode3":"ALB"},{"id":91,"name":"Algeria","isMetric":true,"isoCode2":"DZ","isoCode3":"DZA"},{"id":90,"name":"Argentina","isMetric":true,"isoCode2":"AR","isoCode3":"ARG"},{"id":1,"name":"Armenia","isMetric":true,"isoCode2":"AM","isoCode3":"ARM"},{"id":94,"name":"Australia","isMetric":true,"isoCode2":"AU","isoCode3":"AUS"},{"id":95,"name":"Austria","isMetric":true,"isoCode2":"AT","isoCode3":"AUT"},{"id":97,"name":"Azerbaijan","isMetric":true,"isoCode2":"AZ","isoCode3":"AZE"},{"id":104,"name":"Bahrain","isMetric":true,"isoCode2":"BH","isoCode3":"BHR"},{"id":98,"name":"Bangladesh","isMetric":true,"isoCode2":"BD","isoCode3":"BGD"},{"id":99,"name":"Belarus","isMetric":true,"isoCode2":"BY","isoCode3":"BLR"},{"id":100,"name":"Belgium","isMetric":true,"isoCode2":"BE","isoCode3":"BEL"},{"id":101,"name":"Belize","isMetric":false,"isoCode2":"BZ","isoCode3":"BLZ"},{"id":102,"name":"Bhutan","isMetric":true,"isoCode2":"BT","isoCode3":"BTN"},{"id":103,"name":"Bolivia","isMetric":true,"isoCode2":"BO","isoCode3":"BOL"},{"id":89,"name":"Bosnia and Herzegovina","isMetric":true,"isoCode2":"BA","isoCode3":"BIH"},{"id":88,"name":"Botswana","isMetric":true,"isoCode2":"BW","isoCode3":"BWA"},{"id":87,"name":"Brazil","isMetric":true,"isoCode2":"BR","isoCode3":"BRA"},{"id":86,"name":"Brunei","isMetric":true,"isoCode2":"BN","isoCode3":"BRN"},{"id":85,"name":"Bulgaria","isMetric":true,"isoCode2":"BG","isoCode3":"BGR"},{"id":84,"name":"Cambodia","isMetric":true,"isoCode2":"KH","isoCode3":"KHM"},{"id":83,"name":"Cameroon","isMetric":true,"isoCode2":"CM","isoCode3":"CMR"},{"id":82,"name":"Canada","isMetric":true,"isoCode2":"CA","isoCode3":"CAN"},{"id":81,"name":"Caribbean","isMetric":false,"isoCode2":"029","isoCode3":"029"},{"id":80,"name":"Chile","isMetric":true,"isoCode2":"CL","isoCode3":"CHL"},{"id":79,"name":"China","isMetric":true,"isoCode2":"CN","isoCode3":"CHN"},{"id":78,"name":"Colombia","isMetric":true,"isoCode2":"CO","isoCode3":"COL"},{"id":77,"name":"Congo (DRC)","isMetric":true,"isoCode2":"CD","isoCode3":"COD"},{"id":76,"name":"Costa Rica","isMetric":true,"isoCode2":"CR","isoCode3":"CRI"},{"id":75,"name":"Côte d’Ivoire","isMetric":true,"isoCode2":"CI","isoCode3":"CIV"},{"id":74,"name":"Croatia","isMetric":true,"isoCode2":"HR","isoCode3":"HRV"},{"id":73,"name":"Cuba","isMetric":true,"isoCode2":"CU","isoCode3":"CUB"},{"id":96,"name":"Czech Republic","isMetric":true,"isoCode2":"CZ","isoCode3":"CZE"},{"id":105,"name":"Denmark","isMetric":true,"isoCode2":"DK","isoCode3":"DNK"},{"id":106,"name":"Dominican Republic","isMetric":true,"isoCode2":"DO","isoCode3":"DOM"},{"id":107,"name":"Ecuador","isMetric":true,"isoCode2":"EC","isoCode3":"ECU"},{"id":126,"name":"Egypt","isMetric":true,"isoCode2":"EG","isoCode3":"EGY"},{"id":127,"name":"El Salvador","isMetric":true,"isoCode2":"SV","isoCode3":"SLV"},{"id":128,"name":"Eritrea","isMetric":true,"isoCode2":"ER","isoCode3":"ERI"},{"id":129,"name":"Estonia","isMetric":true,"isoCode2":"EE","isoCode3":"EST"},{"id":130,"name":"Ethiopia","isMetric":true,"isoCode2":"ET","isoCode3":"ETH"},{"id":131,"name":"Faroe Islands","isMetric":true,"isoCode2":"FO","isoCode3":"FRO"},{"id":133,"name":"Finland","isMetric":true,"isoCode2":"FI","isoCode3":"FIN"},{"id":140,"name":"France","isMetric":true,"isoCode2":"FR","isoCode3":"FRA"},{"id":134,"name":"Georgia","isMetric":true,"isoCode2":"GE","isoCode3":"GEO"},{"id":135,"name":"Germany","isMetric":true,"isoCode2":"DE","isoCode3":"DEU"},{"id":136,"name":"Greece","isMetric":true,"isoCode2":"GR","isoCode3":"GRC"},{"id":137,"name":"Greenland","isMetric":true,"isoCode2":"GL","isoCode3":"GRL"},{"id":138,"name":"Guatemala","isMetric":true,"isoCode2":"GT","isoCode3":"GTM"},{"id":139,"name":"Haiti","isMetric":true,"isoCode2":"HT","isoCode3":"HTI"},{"id":125,"name":"Honduras","isMetric":true,"isoCode2":"HN","isoCode3":"HND"},{"id":132,"name":"Hong Kong SAR","isMetric":true,"isoCode2":"HK","isoCode3":"HKG"},{"id":124,"name":"Hungary","isMetric":true,"isoCode2":"HU","isoCode3":"HUN"},{"id":114,"name":"Iceland","isMetric":true,"isoCode2":"IS","isoCode3":"ISL"},{"id":108,"name":"India","isMetric":true,"isoCode2":"IN","isoCode3":"IND"},{"id":109,"name":"Indonesia","isMetric":true,"isoCode2":"ID","isoCode3":"IDN"},{"id":110,"name":"Iran","isMetric":true,"isoCode2":"IR","isoCode3":"IRN"},{"id":111,"name":"Iraq","isMetric":true,"isoCode2":"IQ","isoCode3":"IRQ"},{"id":112,"name":"Ireland","isMetric":true,"isoCode2":"IE","isoCode3":"IRL"},{"id":113,"name":"Israel","isMetric":true,"isoCode2":"IL","isoCode3":"ISR"},{"id":115,"name":"Italy","isMetric":true,"isoCode2":"IT","isoCode3":"ITA"},{"id":122,"name":"Jamaica","isMetric":true,"isoCode2":"JM","isoCode3":"JAM"},{"id":116,"name":"Japan","isMetric":true,"isoCode2":"JP","isoCode3":"JPN"},{"id":117,"name":"Jordan","isMetric":true,"isoCode2":"JO","isoCode3":"JOR"},{"id":118,"name":"Kazakhstan","isMetric":true,"isoCode2":"KZ","isoCode3":"KAZ"},{"id":119,"name":"Kenya","isMetric":true,"isoCode2":"KE","isoCode3":"KEN"},{"id":120,"name":"Korea","isMetric":true,"isoCode2":"KR","isoCode3":"KOR"},{"id":121,"name":"Kuwait","isMetric":true,"isoCode2":"KW","isoCode3":"KWT"},{"id":72,"name":"Kyrgyzstan","isMetric":true,"isoCode2":"KG","isoCode3":"KGZ"},{"id":123,"name":"Laos","isMetric":true,"isoCode2":"LA","isoCode3":"LAO"},{"id":71,"name":"Latin America","isMetric":true,"isoCode2":"419","isoCode3":"419"},{"id":52,"name":"Latvia","isMetric":true,"isoCode2":"LV","isoCode3":"LVA"},{"id":19,"name":"Lebanon","isMetric":true,"isoCode2":"LB","isoCode3":"LBN"},{"id":20,"name":"Libya","isMetric":true,"isoCode2":"LY","isoCode3":"LBY"},{"id":21,"name":"Liechtenstein","isMetric":true,"isoCode2":"LI","isoCode3":"LIE"},{"id":22,"name":"Lithuania","isMetric":true,"isoCode2":"LT","isoCode3":"LTU"},{"id":23,"name":"Luxembourg","isMetric":true,"isoCode2":"LU","isoCode3":"LUX"},{"id":24,"name":"Macao SAR","isMetric":true,"isoCode2":"MO","isoCode3":"MAC"},{"id":26,"name":"Macedonia, FYRO","isMetric":true,"isoCode2":"MK","isoCode3":"MKD"},{"id":33,"name":"Malaysia","isMetric":true,"isoCode2":"MY","isoCode3":"MYS"},{"id":27,"name":"Maldives","isMetric":true,"isoCode2":"MV","isoCode3":"MDV"},{"id":28,"name":"Mali","isMetric":true,"isoCode2":"ML","isoCode3":"MLI"},{"id":29,"name":"Malta","isMetric":true,"isoCode2":"MT","isoCode3":"MLT"},{"id":30,"name":"Mexico","isMetric":true,"isoCode2":"MX","isoCode3":"MEX"},{"id":31,"name":"Moldova","isMetric":true,"isoCode2":"MD","isoCode3":"MDA"},{"id":32,"name":"Monaco","isMetric":true,"isoCode2":"MC","isoCode3":"MCO"},{"id":18,"name":"Mongolia","isMetric":true,"isoCode2":"MN","isoCode3":"MNG"},{"id":17,"name":"Montenegro","isMetric":true,"isoCode2":"ME","isoCode3":"MNE"},{"id":16,"name":"Morocco","isMetric":true,"isoCode2":"MA","isoCode3":"MAR"},{"id":15,"name":"Myanmar","isMetric":true,"isoCode2":"MM","isoCode3":"MMR"},{"id":14,"name":"Nepal","isMetric":true,"isoCode2":"NP","isoCode3":"NPL"},{"id":13,"name":"Netherlands","isMetric":true,"isoCode2":"NL","isoCode3":"NLD"},{"id":12,"name":"New Zealand","isMetric":true,"isoCode2":"NZ","isoCode3":"NZL"},{"id":11,"name":"Nicaragua","isMetric":true,"isoCode2":"NI","isoCode3":"NIC"},{"id":10,"name":"Nigeria","isMetric":true,"isoCode2":"NG","isoCode3":"NGA"},{"id":9,"name":"Norway","isMetric":true,"isoCode2":"NO","isoCode3":"NOR"},{"id":8,"name":"Oman","isMetric":true,"isoCode2":"OM","isoCode3":"OMN"},{"id":7,"name":"Pakistan","isMetric":true,"isoCode2":"PK","isoCode3":"PAK"},{"id":6,"name":"Panama","isMetric":true,"isoCode2":"PA","isoCode3":"PAN"},{"id":5,"name":"Paraguay","isMetric":true,"isoCode2":"PY","isoCode3":"PRY"},{"id":4,"name":"Peru","isMetric":true,"isoCode2":"PE","isoCode3":"PER"},{"id":3,"name":"Philippines","isMetric":true,"isoCode2":"PH","isoCode3":"PHL"},{"id":2,"name":"Poland","isMetric":true,"isoCode2":"PL","isoCode3":"POL"},{"id":25,"name":"Portugal","isMetric":true,"isoCode2":"PT","isoCode3":"PRT"},{"id":34,"name":"Puerto Rico","isMetric":false,"isoCode2":"PR","isoCode3":"PRI"},{"id":35,"name":"Qatar","isMetric":true,"isoCode2":"QA","isoCode3":"QAT"},{"id":36,"name":"Réunion","isMetric":true,"isoCode2":"RE","isoCode3":"REU"},{"id":55,"name":"Romania","isMetric":true,"isoCode2":"RO","isoCode3":"ROU"},{"id":56,"name":"Russia","isMetric":true,"isoCode2":"RU","isoCode3":"RUS"},{"id":57,"name":"Rwanda","isMetric":true,"isoCode2":"RW","isoCode3":"RWA"},{"id":58,"name":"Saudi Arabia","isMetric":true,"isoCode2":"SA","isoCode3":"SAU"},{"id":59,"name":"Senegal","isMetric":true,"isoCode2":"SN","isoCode3":"SEN"},{"id":60,"name":"Serbia","isMetric":true,"isoCode2":"RS","isoCode3":"SRB"},{"id":62,"name":"Singapore","isMetric":true,"isoCode2":"SG","isoCode3":"SGP"},{"id":69,"name":"Slovakia","isMetric":true,"isoCode2":"SK","isoCode3":"SVK"},{"id":63,"name":"Slovenia","isMetric":true,"isoCode2":"SI","isoCode3":"SVN"},{"id":64,"name":"Somalia","isMetric":true,"isoCode2":"SO","isoCode3":"SOM"},{"id":65,"name":"South Africa","isMetric":true,"isoCode2":"ZA","isoCode3":"ZAF"},{"id":66,"name":"Spain","isMetric":true,"isoCode2":"ES","isoCode3":"ESP"},{"id":67,"name":"Sri Lanka","isMetric":true,"isoCode2":"LK","isoCode3":"LKA"},{"id":68,"name":"Sweden","isMetric":true,"isoCode2":"SE","isoCode3":"SWE"},{"id":54,"name":"Switzerland","isMetric":true,"isoCode2":"CH","isoCode3":"CHE"},{"id":61,"name":"Syria","isMetric":true,"isoCode2":"SY","isoCode3":"SYR"},{"id":53,"name":"Taiwan","isMetric":true,"isoCode2":"TW","isoCode3":"TWN"},{"id":43,"name":"Tajikistan","isMetric":true,"isoCode2":"TJ","isoCode3":"TJK"},{"id":37,"name":"Thailand","isMetric":true,"isoCode2":"TH","isoCode3":"THA"},{"id":38,"name":"Trinidad and Tobago","isMetric":true,"isoCode2":"TT","isoCode3":"TTO"},{"id":39,"name":"Tunisia","isMetric":true,"isoCode2":"TN","isoCode3":"TUN"},{"id":40,"name":"Turkey","isMetric":true,"isoCode2":"TR","isoCode3":"TUR"},{"id":41,"name":"Turkmenistan","isMetric":true,"isoCode2":"TM","isoCode3":"TKM"},{"id":42,"name":"Ukraine","isMetric":true,"isoCode2":"UA","isoCode3":"UKR"},{"id":44,"name":"United Arab Emirates","isMetric":true,"isoCode2":"AE","isoCode3":"ARE"},{"id":51,"name":"United Kingdom","isMetric":true,"isoCode2":"GB","isoCode3":"GBR"},{"id":45,"name":"United States","isMetric":false,"isoCode2":"US","isoCode3":"USA"},{"id":46,"name":"Uruguay","isMetric":true,"isoCode2":"UY","isoCode3":"URY"},{"id":47,"name":"Uzbekistan","isMetric":true,"isoCode2":"UZ","isoCode3":"UZB"},{"id":48,"name":"Venezuela","isMetric":true,"isoCode2":"VE","isoCode3":"VEN"},{"id":49,"name":"Vietnam","isMetric":true,"isoCode2":"VN","isoCode3":"VNM"},{"id":50,"name":"World","isMetric":true,"isoCode2":"001","isoCode3":"001"},{"id":70,"name":"Yemen","isMetric":true,"isoCode2":"YE","isoCode3":"YEM"},{"id":141,"name":"Zimbabwe","isMetric":true,"isoCode2":"ZW","isoCode3":"ZWE"}]
		""", new JsonSerializerOptions() {
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	})!;
#pragma warning restore CA1869 // Cache and reuse 'JsonSerializerOptions' instances

	public static CountryzRepo Instance { get; } = new CountryzRepo();
}