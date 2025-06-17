using System.Text.Json;

namespace Chameleon.lib.ThirdParty.PVA;
public record RCountry(string Name);
public record RService(string Name);
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
public abstract class PVAInstance(string name, IEnumerable<RCountry> countries, IEnumerable<RService> services) : IPVAInstance {
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
