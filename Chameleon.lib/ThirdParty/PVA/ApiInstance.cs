using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
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
	public string Name { get; } = name;
	public string? ApiKey { get; set; }

	public IEnumerable<RCountry> Countries { get; set; } = countries;
	public IEnumerable<RService> Services { get; set; } = services;

	public static async Task<string> GetAsync(string url, IEnumerable<KeyValuePair<string, string>>? headers = null) {
		using HttpClient client = new();
		if (headers != null) {
			foreach (var header in headers)
				client.DefaultRequestHeaders.Add(header.Key, header.Value);
		}

		using var response = await client.GetAsync(url);
		var content = await response.Content.ReadAsStringAsync();
		return content;
	}

	public static async Task<HttpResponseMessage> PostAsync(string url,
		AuthenticationHeaderValue? authorization = null, IEnumerable<KeyValuePair<string, string>>? headers = null, MultipartFormDataContent? content = null
	) {
		using HttpClient client = new();
		client.DefaultRequestHeaders.Authorization = authorization;

		using var request = new HttpRequestMessage(HttpMethod.Post, url) {
			Content = content
		};
		if (headers != null) {
			foreach (var header in headers)
				request.Headers.Add(header.Key, header.Value);
		}

		return await client.SendAsync(request);
	}

	public async static Task<string> PutAsync(string url, IEnumerable<KeyValuePair<string, string>>? headers = null) {
		using HttpClient client = new();
		using var request = new HttpRequestMessage(HttpMethod.Put, url);
		if (headers != null) {
			foreach (var header in headers)
				request.Headers.Add(header.Key, header.Value);
		}

		using var response = await client.SendAsync(request);
		var content = await response.Content.ReadAsStringAsync();
		return content;
	}

	public abstract Task Init();
	public abstract Task Save();
	public abstract Task<Tuple<string, string>> GetNumberAsync(RCountry country, RService app);
	public abstract Task<Tuple<string, string>> GetCodeAsync(RCountry country, RService app, string numberData);
	public abstract Task<Tuple<string, string>> CancelOrderAsync(string orderId);
}
