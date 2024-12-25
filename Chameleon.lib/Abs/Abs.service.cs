using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace Chameleon.lib.Abs;
public class ABService {
	// Lazy-loaded singleton
	private static readonly Lazy<ABService> _instance = new(() => new ABService("http://localhost:3001"));
	public static ABService Instance => _instance.Value;
	// ------------------------

	private readonly HttpClient _httpClient;
	private readonly JsonSerializerOptions _jsonOptions;

	private string? token;
	//Auther.AuthSession!.UserId, Auther.AuthSession!.UserName!, Auther.AuthSession!.LicenseKey!, Auther.AuthSession!.CreatorUserId!
	private Func<Tuple<long,string,string,long?>>? credLoader;

	public bool IsAuthenticated => !string.IsNullOrWhiteSpace(token);
	public long UserId => credLoader!().Item1;
	public string UserName => credLoader!().Item2;
	public string LicenseKey => credLoader!().Item3;

	// Private constructor to enforce singleton usage
	private ABService(string baseUrl)
	{
		_httpClient = new HttpClient {
			BaseAddress = new Uri(baseUrl)
		};

		_jsonOptions = new JsonSerializerOptions {
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
	}
	//

	public void SetLoaders(Func<Tuple<long,string,string,long?>> credz)
	{
		this.credLoader = credz;
	}

	#region Public API Methods

	public async Task<string?> GetTokenAsync()
	{
		var (userId, email, license_key, creatorId) = credLoader!();
		var data = new { userId, email, license_key, creatorId };
		var response = await PostAsync<ApiSuccessResponse<Doc<TokenObject>>>(
			"/auth/license"
			, data
		);
		var token = response?.Data?.Objects
			.FindLast(o => o.Type == ObjectTypes.USER.GetUserType(UserType.TOKEN))?.Data?.Token;
		this.token = token;

		return this.token;
	}

	public async Task<ApiSuccessResponse<Doc<TokenObject>>?> LoginAsync()
	{
		var body = new { token };
		var response = await PostAsync<ApiSuccessResponse<Doc<TokenObject>>>("/auth/login", body);
		return response;
	}

	public async Task<ApiSuccessResponse<Doc<object>>?> AddObjectAsync(
			string objectType,
			object data
	)
	{
		SetBearerToken();

		var body = new
		{
			type = objectType,
			data
		};

		var endpoint = $"/api/objects/{UserId}";
		var response = await PutAsync<ApiSuccessResponse<Doc<object>>>(endpoint, body);
		return response;
	}

	public async Task<ApiSuccessResponse<List<BaseObject<object>>>?> GetObjectsAsync(
			ObjectType objectType
	)
	{
		SetBearerToken();

		var endpoint = $"/api/objects/{UserId}?type={ObjectTypes.OBJECT.GetObjectType(objectType)}";
		var response = await GetAsync<ApiSuccessResponse<List<BaseObject<object>>>>(endpoint);
		return response;
	}

	public async Task<ApiSuccessResponse<Doc<object>>?> AddCookiesAsync(
		string userId,
		object data
	)
	{
		SetBearerToken();

		var endpoint = $"/api/objects/{userId}";
		var response = await PutRawJsonAsync<ApiSuccessResponse<Doc<object>>>(
			endpoint, 
			JsonSerializer.Serialize(new { type = ObjectTypes.OBJECT.GetObjectType(ObjectType.COOKIE), data }, _jsonOptions)
		);
		return response;
	}

	/// <summary>
	/// get cookies for a user
	/// </summary>
	/// <typeparam name="T">BrowserContextCookiesResult</typeparam>
	/// <returns></returns>
	public async Task<ApiSuccessResponse<List<BaseObject<CookieObject<T>>>>?> GetCookiesAsync<T>()
	{
		SetBearerToken();

		var endpoint = $"/api/objects/{UserId}?type={ObjectTypes.OBJECT.GetObjectType(ObjectType.COOKIE)}";
		var response = await GetAsync<ApiSuccessResponse<List<BaseObject<CookieObject<T>>>>>(endpoint);
		return response;
	}

	public async Task<bool> DeleteCookieAsync(
			string cookieId
	)
	{
		SetBearerToken();

		var endpoint = $"/api/objects/{UserId}?type={ObjectTypes.OBJECT.GetObjectType(ObjectType.COOKIE)}&_id={cookieId}";
		await DeleteAsync(endpoint);
		return true;
	}

	#endregion

	#region Internal HTTP Helpers

	private void SetBearerToken()
	{
		_httpClient.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(token)
			? new AuthenticationHeaderValue("Bearer", token) 
			: null;
	}

	private async Task<T?> GetAsync<T>(string requestUri, bool ensureSuccess = true)
	{
		using var response = await _httpClient.GetAsync(requestUri);
		var content = await response.Content.ReadAsStringAsync();

		if (ensureSuccess && !response.IsSuccessStatusCode) {
			var apiError = DeserializeSafely<ApiErrorResponse>(content);
			var exception = new HttpRequestException($"GET {requestUri} returned {response.StatusCode}");
			if (apiError != null) exception.Data["ApiError"] = apiError;
			throw exception;
		}

		return DeserializeSafely<T>(content);
	}

	private async Task<T?> PostAsync<T>(string requestUri, object body, bool ensureSuccess = true)
	{
		var json = JsonSerializer.Serialize(body, _jsonOptions);
		using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
		using var response = await _httpClient.PostAsync(requestUri, httpContent);
		var content = await response.Content.ReadAsStringAsync();

		if (ensureSuccess && !response.IsSuccessStatusCode) {
			var apiError = DeserializeSafely<ApiErrorResponse>(content);
			var exception = new HttpRequestException($"POST {requestUri} returned {response.StatusCode}");
			if (apiError != null) exception.Data["ApiError"] = apiError;
			throw exception;
		}

		return DeserializeSafely<T>(content);
	}

	private async Task<T?> PutAsync<T>(string requestUri, object body, bool ensureSuccess = true)
	{
		var json = JsonSerializer.Serialize(body, _jsonOptions);
		using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
		using var response = await _httpClient.PutAsync(requestUri, httpContent);
		var content = await response.Content.ReadAsStringAsync();

		if (ensureSuccess && !response.IsSuccessStatusCode) {
			var apiError = DeserializeSafely<ApiErrorResponse>(content);
			var exception = new HttpRequestException($"PUT {requestUri} returned {response.StatusCode}");
			if (apiError != null) exception.Data["ApiError"] = apiError;
			throw exception;
		}

		return DeserializeSafely<T>(content);
	}

	private async Task<T?> PutRawJsonAsync<T>(string requestUri, string rawJson, bool ensureSuccess = true)
	{
		using var httpContent = new StringContent(rawJson, Encoding.UTF8, "application/json");
		using var response = await _httpClient.PutAsync(requestUri, httpContent);
		var content = await response.Content.ReadAsStringAsync();

		if (ensureSuccess && !response.IsSuccessStatusCode) {
			var apiError = DeserializeSafely<ApiErrorResponse>(content);
			var exception = new HttpRequestException($"PUT {requestUri} returned {response.StatusCode}");
			if (apiError != null) exception.Data["ApiError"] = apiError;
			throw exception;
		}

		return DeserializeSafely<T>(content);
	}

	private async Task DeleteAsync(string requestUri, bool ensureSuccess = true)
	{
		using var response = await _httpClient.DeleteAsync(requestUri);
		var content = await response.Content.ReadAsStringAsync();

		if (ensureSuccess && !response.IsSuccessStatusCode) {
			var apiError = DeserializeSafely<ApiErrorResponse>(content);
			var exception = new HttpRequestException($"DELETE {requestUri} returned {response.StatusCode}");
			if (apiError != null) exception.Data["ApiError"] = apiError;
			throw exception;
		}
	}

	private T? DeserializeSafely<T>(string json)
	{
		try {
			return JsonSerializer.Deserialize<T>(json, _jsonOptions);
		} catch {
			// swallow or log
			return default;
		}
	}

	#endregion
}
