namespace Chameleon.lib.Common.Util.ThirdParty;
public record RCountry(string Name);
public record RService(string Name);

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
public interface IPVAInstance {
	string Name { get; }
	string? ApiKey { get; set; }
	IEnumerable<RCountry> Countries { get; }
	IEnumerable<RService> Services { get; }
	Task Init();
	Task Save();
	Task<Tuple<string, string>> GetNumberAsync(RCountry country, RService app);
	Task<Tuple<string, string>> GetCodeAsync(RCountry country, RService app, string numberData);
	Task<Tuple<string, string>> CancelOrderAsync(string orderId);
}