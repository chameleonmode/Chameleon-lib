using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chameleon.lib.Abs;
public class ABService {
	// Constants
	private const int DEFAULT_TIMEOUT_SECONDS = 30;
	private const int MAX_CONNECTIONS_PER_SERVER = 20;
	private const int DEFAULT_BUFFER_SIZE = 8192;

	// Lazy-loaded singleton
	private static readonly Lazy<ABService> _instance = new(() => new ABService(Constas.ABS_BASE_URL));
	public static ABService Instance => _instance.Value;

	// Private fields
	private readonly HttpClient _httpClient;
	private static readonly JsonSerializerOptions _jsonOptions = new() {
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private string? token;
	private Func<Tuple<long, string, string, long?>>? credLoader;

	// Public properties
	public long UserId => credLoader!()!.Item1;
	public string UserName => credLoader!()!.Item2;
	public string LicenseKey => credLoader!()!.Item3;
	public bool IsAuthenticated => !string.IsNullOrWhiteSpace(token);

	// Private constructor to enforce singleton usage
	private ABService(string baseUrl)
	{
		var handler = new SocketsHttpHandler {
			MaxConnectionsPerServer = MAX_CONNECTIONS_PER_SERVER,
			PooledConnectionLifetime = TimeSpan.FromMinutes(2),
			KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
			AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
		};

		_httpClient = new HttpClient(handler) {
			BaseAddress = new Uri(baseUrl),
			Timeout = TimeSpan.FromSeconds(DEFAULT_TIMEOUT_SECONDS)
		};
	}

	public void SetLoaders(Func<Tuple<long, string, string, long?>> credz)
	{
		credLoader = credz;
	}

	#region Public API Methods

	public async Task<string?> GetTokenAsync()
	{
		return await RetryWithPolicyAsync<string>(async () => {
			var (userId, email, license_key, creatorId) = credLoader!();
			var data = new { userId, email, license_key, creatorId };
			var response = await PostAsync<ApiSuccessResponse<Doc<TokenObject>>>(
					"/auth/license",
					data
			);
			var token = response?.Data?.Objects
					.FindLast(o => o.Type == UserType.TOKEN.ToString())?.Data?.Token;
			this.token = token;

			return this.token;
		});
	}

	public async Task<ApiSuccessResponse<Doc<TokenObject>>?> LoginAsync()
	{
		return await RetryWithPolicyAsync(async () => {
			var body = new { token };
			return await PostAsync<ApiSuccessResponse<Doc<TokenObject>>>("/auth/login", body);
		});
	}

	public async Task<ApiSuccessResponse<Doc<object>>?> AddObjectAsync(
			string objectType,
			object data)
	{
		SetBearerToken();

		return await RetryWithPolicyAsync(async () => {
			var body = new { type = objectType, data };
			var endpoint = $"/api/objects/{UserId}";
			return await PutAsync<ApiSuccessResponse<Doc<object>>>(endpoint, body);
		});
	}

	public async Task<ApiSuccessResponse<List<BaseObject<object>>>?> GetObjectsAsync(
			ObjectType objectType)
	{
		SetBearerToken();

		return await RetryWithPolicyAsync(async () => {
			var endpoint = $"/api/objects/{UserId}?type={objectType}";
			var response = await GetAsync<ApiSuccessResponse<List<BaseObject<object>>>>(endpoint);

			return response;
		});
	}

	public async Task<ApiSuccessResponse<Doc<object>>?> AddCookiesAsync(
			string userId,
			object data)
	{
		SetBearerToken();

		return await RetryWithPolicyAsync(async () => {
			var endpoint = $"/api/objects/{userId}";
			var body = new { type = ObjectType.COOKIE.ToString(), data };
			return await PutAsync<ApiSuccessResponse<Doc<object>>>(endpoint, body);
		});
	}

	public async Task<ApiSuccessResponse<List<BaseObject<CookieObject<T>>>>?> GetCookiesAsync<T>()
	{
		SetBearerToken();

		var cacheKey = $"cookies_{UserId}_{typeof(T).Name}";

		return await RetryWithPolicyAsync(async () => {
			var endpoint = $"/api/objects/{UserId}?type={ObjectType.COOKIE}";
			var response = await GetAsync<ApiSuccessResponse<List<BaseObject<CookieObject<T>>>>>(endpoint);

			return response;
		});
	}

	public async Task<bool> DeleteCookieAsync(string cookieId)
	{
		SetBearerToken();

		return await RetryWithPolicyAsync(async () => {
			var endpoint = $"/api/objects/{UserId}?type={ObjectType.COOKIE.ToString()}&_id={cookieId}";
			await DeleteAsync(endpoint);
			return true;
		});
	}

	#endregion

	#region Internal HTTP Helpers

	private void SetBearerToken()
	{
		_httpClient.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(token)
				? new AuthenticationHeaderValue("Bearer", token)
				: null;
	}

	private async Task<T?> RetryWithPolicyAsync<T>(Func<Task<T?>> operation, int maxRetries = 3)
	{
		for (var i = 1; i <= maxRetries; i++) {
			try {
				return await operation();
			} catch (Exception) when (i < maxRetries) {
				await Task.Delay(100 * i); // Exponential backoff
			}
		}
		return await operation(); // Last try
	}

	private static class BufferPool {
		private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

		public static byte[] Rent(int minimumLength) => _arrayPool.Rent(minimumLength);
		public static void Return(byte[] array) => _arrayPool.Return(array);
	}

	private async Task<T?> GetAsync<T>(string requestUri)
	{
		return await SendRequestAsync<T>(HttpMethod.Get, requestUri, null, false);
	}

	private async Task<T?> PostAsync<T>(string requestUri, object body)
	{
		return await SendRequestAsync<T>(HttpMethod.Post, requestUri, body);
	}

	private async Task<T?> PutAsync<T>(string requestUri, object body)
	{
		return await SendRequestAsync<T>(HttpMethod.Put, requestUri, body);
	}

	private async Task DeleteAsync(string requestUri) => await SendRequestAsync<object>(HttpMethod.Delete, requestUri, null);

	private async Task<T?> SendRequestAsync<T>(
			HttpMethod method,
			string requestUri,
			object? body = null,
			bool ensureSuccess = true)
	{
		using var request = new HttpRequestMessage(method, requestUri);
		if (body != null) {
			var json = JsonSerializer.Serialize(body, _jsonOptions);
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");
		}

		using var response = await _httpClient.SendAsync(request);
		var buffer = BufferPool.Rent(DEFAULT_BUFFER_SIZE);
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
			}

			return DeserializeSafely<T>(content);
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

	#endregion
}