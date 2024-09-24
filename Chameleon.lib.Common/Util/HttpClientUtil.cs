using System.Net.Http.Headers;

namespace Chameleon.lib.Common.Util;
public static class HttpClientUtil {
	public static async Task<string> GetAsync(string url, IEnumerable<KeyValuePair<string, string>>? headers = null)
	{
		using HttpClient client = new();
		if (headers != null) {
			foreach (var header in headers)
				client.DefaultRequestHeaders.Add(header.Key, header.Value);
		}

		using var response = await client.GetAsync(url);
		return await response.Content.ReadAsStringAsync();
	}

	public static async Task<HttpResponseMessage> PostAsync(string url, AuthenticationHeaderValue? authorization = null, IEnumerable<KeyValuePair<string, string>>? headers = null, MultipartFormDataContent? content = null)
	{
		using HttpClient client = new();
		if (authorization != null)
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

	public async static Task<string> PutAsync(string url, IEnumerable<KeyValuePair<string, string>>? headers = null)
	{
		using HttpClient client = new();
		using var request = new HttpRequestMessage(HttpMethod.Put, url);
		if (headers != null) {
			foreach (var header in headers)
				request.Headers.Add(header.Key, header.Value);
		}

		using var response = await client.SendAsync(request);

		return await response.Content.ReadAsStringAsync();
	}
}
