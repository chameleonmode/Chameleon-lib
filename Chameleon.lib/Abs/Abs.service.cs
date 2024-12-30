using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Chameleon.lib.Abs;
public class ABService {
	// Private fields
	private readonly AbsClient _absClient = AbsClient.Instance;
	private string? token;

	// Public properties
	private Func<Tuple<long, string, string, long?>>? credLoader;
	public long UserId => credLoader!()!.Item1;
	public string UserName => credLoader!()!.Item2;
	public string LicenseKey => credLoader!()!.Item3;

	// Private constructor to enforce singleton usage
	private ABService() {
		_absClient.TokenProvider = async () => {
			if (string.IsNullOrWhiteSpace(this.token)) {
				_ = await GetTokenAsync();
			}
			return this.token;
		};
	}

	public void SetLoaders(Func<Tuple<long, string, string, long?>> credz)
	{
		credLoader = credz;
	}

	private static async Task<T?> RetryWithPolicyAsync<T>(Func<Task<T?>> operation, int maxRetries = 3)
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

	#region Public API Methods

	public async Task<string?> GetTokenAsync()
	{
		_absClient.TokenProvider = () => Task.FromResult<string?>(null);

		var (userId, email, license_key, creatorId) = credLoader!();
		//
		var endpoint = $"/auth/license";
		var body = new { userId, email, license_key, creatorId };
		return await RetryWithPolicyAsync(async () => {
			var response = await _absClient.PostAsync<string>(endpoint, body);
			this.token = response?.Data;
			_absClient.TokenProvider = () => Task.FromResult(this.token);
			return await _absClient.TokenProvider();
		});
	}

	public async Task<string?> LoginAsync()
	{
		//
		var endpoint = $"/auth/login";
		var body = new { token };
		return await RetryWithPolicyAsync(async () => {
			return (await _absClient.PostAsync<string?>(endpoint, body))?.Data;
		});
	}

	public async Task<Doc<object>?> AddCookiesAsync(
			string userId,
			object data)
	{
		var endpoint = $"/api/objects/{userId}";
		var body = new { 
			type = ObjectType.COOKIE.ToString(), 
			data
		};
		//
		return await RetryWithPolicyAsync(async () => {
			return (await _absClient.PutAsync<Doc<object>>(endpoint, body))?.Data;
		});
	}

	public async Task<Doc<ObjectsCookies<T>>?> GetCookiesAsync<T>()
	{
		var endpoint = $"/api/objects/{UserId}?type={ObjectType.COOKIE}";
		//
		return await RetryWithPolicyAsync(async () => {
			return (await _absClient.GetAsync<Doc<ObjectsCookies<T>>>(endpoint))?.Data;
		});
	}

	public async Task DeleteCookieAsync(string cookieId)
	{
		var endpoint = $"/api/objects/{UserId}?type={ObjectType.COOKIE}&_id={cookieId}";
		//
		_ = await RetryWithPolicyAsync(async () => {
			return await _absClient.DeleteAsync(endpoint);
		});
	}

	#endregion

	#region singleton
	private static ABService? _instance;
	private static readonly object _lock = new();
	public static ABService Instance {
		get {
			lock (_lock) {
				return _instance ??= new ABService();
			}
		}
	}
	#endregion
}