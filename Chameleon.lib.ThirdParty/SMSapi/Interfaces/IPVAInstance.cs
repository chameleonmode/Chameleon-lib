using Chameleon.lib.Common.Records;

namespace Chameleon.lib.ThirdParty.SMSapi.Interfaces;
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
