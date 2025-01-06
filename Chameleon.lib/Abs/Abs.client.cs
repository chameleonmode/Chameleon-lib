using System.Text;
using System.Net;
using System.Buffers;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chameleon.lib.Abs;

public class AbsClient {
	// Constants
	private const int DEFAULT_TIMEOUT_SECONDS = 30;
	private const int MAX_CONNECTIONS_PER_SERVER = 20;

	// Private fields
	private static readonly HttpClient _httpClient = new(new SocketsHttpHandler {
		MaxConnectionsPerServer = MAX_CONNECTIONS_PER_SERVER,
		PooledConnectionLifetime = TimeSpan.FromMinutes(2),
		KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
		AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
	}) {
		BaseAddress = new Uri(Constas.ABS_BASE_URL),
		Timeout = TimeSpan.FromSeconds(DEFAULT_TIMEOUT_SECONDS)
	};
	private static readonly JsonSerializerOptions _jsonOptions = new() {
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	// Public properties
	public Func<Task<IAuth?>> TokenProvider { get; set; } = () => Task.FromResult<IAuth?>(null);

	// Public methods
	public async Task<ApiSuccessResponse<T?>> GetAsync<T>(string requestUri) =>
		await SendRequestAsync<T>(HttpMethod.Get, requestUri, ensureSuccess: false);


	public async Task<ApiSuccessResponse<T?>> PostAsync<T>(string requestUri, object body, bool authentication = true) =>
		await SendRequestAsync<T>(HttpMethod.Post, requestUri, body, authentication);


	public async Task<ApiSuccessResponse<T?>> PutAsync<T>(string requestUri, object body) =>
		await SendRequestAsync<T>(HttpMethod.Put, requestUri, body);


	public async Task<object> DeleteAsync(string requestUri) =>
		await SendRequestAsync<object>(HttpMethod.Delete, requestUri);

	private async Task<ApiSuccessResponse<T?>> SendRequestAsync<T>(
			HttpMethod method,
			string requestUri,
			object? body = null,
			bool authentication = true,
			bool ensureSuccess = true)
	{
		if (authentication) {
			var token = (await TokenProvider())?.AccessToken;
			_httpClient.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(token)
					? new AuthenticationHeaderValue("Bearer", token)
					: null;

		}

		using var request = new HttpRequestMessage(method, requestUri);
		if (body != null) {
			var json = JsonSerializer.Serialize(body, _jsonOptions);
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");
		}

		using var response = await _httpClient.SendAsync(request);
		var buffer = BufferPool.Rent();
		try {
			using var stream = await response.Content.ReadAsStreamAsync();
			using var ms = new MemoryStream();
			int read;
			while ((read = await stream.ReadAsync(buffer)) > 0) {
				ms.Write(buffer, 0, read);
			}

			var content = Encoding.UTF8.GetString(ms.ToArray());

			if (ensureSuccess && !response.IsSuccessStatusCode) {
				var apiError = DeserializeSafely<ApiErrorResponse>(content);
				var exception = new HttpRequestException($"{method} {requestUri} returned {response.StatusCode}");
				if (apiError != null) exception.Data["ApiError"] = apiError;
				throw exception;
			}else if(response.StatusCode == HttpStatusCode.NoContent) {
				return new ApiSuccessResponse<T?>(default, null);
			}

			return DeserializeSafely<ApiSuccessResponse<T?>>(content)
				?? throw new InvalidOperationException("Response is unreadable");
		} finally {
			BufferPool.Return(buffer);
		}
	}

	private static T? DeserializeSafely<T>(string json)
	{
		try {
			return JsonSerializer.Deserialize<T>(json, _jsonOptions);
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

	#region singleton
	private static AbsClient? _instance;
	private static readonly object _lock = new();
	private AbsClient() { }
	public static AbsClient Instance {
		get {
			lock (_lock) {
				return _instance ??= new AbsClient();
			}
		}
	}
	#endregion
}
