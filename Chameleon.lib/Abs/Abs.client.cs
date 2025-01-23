using Chameleon.lib.Const;
using System.Buffers;
using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using System.Text;
using Chameleon.lib.Auth;

namespace Chameleon.lib.Abs;

public class AbsClient(string baseUrl) {
	// Private fields
	private readonly HttpClient httpClient = new(new SocketsHttpHandler {
		PooledConnectionLifetime = TimeSpan.FromMinutes(2),
		KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
		AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
	}) {
		BaseAddress = new Uri(baseUrl)
	};

	// Public methods
	public async Task<T?> GetAsync<T>(string requestUri) =>
		await SendRequestAsync<T>(HttpMethod.Get, requestUri);


	public async Task<T?> PostAsync<T>(string requestUri, object body, bool authentication = true) =>
		await SendRequestAsync<T>(HttpMethod.Post, requestUri, body, authentication);


	public async Task<T?> PutAsync<T>(string requestUri, object body) =>
		await SendRequestAsync<T>(HttpMethod.Put, requestUri, body);


	public async Task<object?> DeleteAsync(string requestUri) =>
		await SendRequestAsync<object>(HttpMethod.Delete, requestUri);

	private async Task<T?> SendRequestAsync<T>(
			HttpMethod method,
			string requestUri,
			object? body = null,
			bool authentication = true) {
		if (authentication) {
			httpClient.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", Session.Instance.Auth0Client.Token?.access_token);
		}

		using var request = new HttpRequestMessage(method, requestUri);
		if (body != null) {
			var json = JsonSerializer.Serialize(body, JS.InsensitiveCamelCaseOptions);
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");
		}

		using var response = await httpClient.SendAsync(request);
		var buffer = BufferPool.Rent();
		try {
			using var stream = await response.Content.ReadAsStreamAsync();
			using var ms = new MemoryStream();
			var read = 0;
			while ((read = await stream.ReadAsync(buffer)) > 0) {
				ms.Write(buffer, 0, read);
			}

			var content = Encoding.UTF8.GetString(ms.ToArray());

			return !response.IsSuccessStatusCode
				? throw new HttpRequestException($"{method} {requestUri} returned {response.StatusCode}: " + content)
				: response.StatusCode == HttpStatusCode.NoContent
					? default
					: DeserializeSafely<T>(content)
									?? throw new InvalidOperationException("Response is unreadable");
		} finally {
			BufferPool.Return(buffer);
		}
	}

	private static T? DeserializeSafely<T>(string json) {
		try {
			return JsonSerializer.Deserialize<T>(json, JS.InsensitiveCamelCaseOptions);
		} catch {
			return default;
		}
	}


	/// <summary>
	/// A pool of byte arrays to reduce memory allocations.
	/// </summary>
	private static class BufferPool {
		//private const int DEFAULT_BUFFER_SIZE = 8192;
		private const int DEFAULT_BUFFER_SIZE = 4096;

		private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

		public static byte[] Rent(int minimumLength = DEFAULT_BUFFER_SIZE) => _arrayPool.Rent(minimumLength);
		public static void Return(byte[] array) => _arrayPool.Return(array);
	}
}
