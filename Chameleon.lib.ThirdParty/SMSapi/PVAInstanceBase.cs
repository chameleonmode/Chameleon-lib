
using System.Text.Json;

using Chameleon.lib.Common.Interfaces.ThirdParty;
using Chameleon.lib.Common.Records;

namespace Chameleon.lib.ThirdParty.SMSapi;
public abstract class PVAInstanceBase(string name, IEnumerable<RCountry> countries, IEnumerable<RService> services) : IPVAInstance {
	public readonly JsonSerializerOptions JSOptions = new() {
		PropertyNameCaseInsensitive = true,
		WriteIndented = true,
	};

	public string Name { get; } = name;
	public string? ApiKey { get; set; }

	public IEnumerable<RCountry> Countries { get; set; } = countries;
	public IEnumerable<RService> Services { get; set; } = services;

	public abstract Task Init();
	public abstract Task Save();
	public abstract Task<Tuple<string, string>> GetNumberAsync(RCountry country, RService app);
	public abstract Task<Tuple<string, string>> GetCodeAsync(RCountry country, RService app, string numberData);
	public abstract Task<Tuple<string, string>> CancelOrderAsync(string orderId);
}
