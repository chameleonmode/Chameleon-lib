using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Chameleon.lib.Abs;
public class ABService {
	// Private
	private readonly AbsClient _absClient = AbsClient.Instance;
	//
	private AuthRecord? authRecord;

	private Action<string?>? onMessage;
	private Action<(string key, string? value)>? onSave;
	private Func<string, string?>? onLoad;
	private Func<(long user_id, string username, string license_key, long? owner_id)>? credz;

	// Public properties
	public long UserId => credz!()!.user_id;
	public string UserName => credz!()!.username;
	public string LicenseKey => credz!()!.license_key;

	// Private constructor to enforce singleton usage
	private ABService()
	{
		var load = new Func<Task<AuthRecord?>>(async () =>
		{
			if (authRecord == null) {
				var refreshToken = onLoad?.Invoke(Constas.IoCKeys.IAuth);
				if (!string.IsNullOrEmpty(refreshToken)) {
					try {
						return await Refresh(refreshToken);
					} catch {
						return await Login();
					}
				} else {
					try {
						return await Login();
					} catch {
						return await Register();
					}
				}
			}
			return null;
		});

		_absClient.TokenProvider = async() => {
			authRecord = await load();
			onSave?.Invoke((Constas.IoCKeys.IAuth, authRecord?.Auth?.RefreshToken));
			return authRecord?.Auth;
		};
	}

	public void Use(
		Func<(long user_id, string username, string license_key, long? owner_id)> credz, 
		Action<string?> onMessage,
		Action<(string key, string? value)> onSave,
		Func<string, string?> onLoad
		)
	{
		this.credz = credz;
		this.onMessage = onMessage;
		this.onSave = onSave;
		this.onLoad = onLoad;
	}

	private static async Task<T> RetryWithPolicyAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
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

	#region auth
	public async Task<AuthRecord?> Register()
	{
		// 
		var (userId, email, license_key, creatorId) = credz!();
		var endpoint = $"/auth/register";
		var body = new { user_id = userId, email, license_key, creatorId };
		// Register
		return (await RetryWithPolicyAsync(async () =>
			await _absClient.PostAsync<AuthRecord>(endpoint, body, false)
		))?.Data;
	}

	public async Task<AuthRecord?> Login()
	{
		//
		var email = credz!()!.username;
		var endpoint = $"/auth/login";
		var body = new { email };
		// Login
		return (await RetryWithPolicyAsync(async () =>
			await _absClient.PostAsync<AuthRecord>(endpoint, body, false)
		, 1))?.Data;
	}

	public async Task<AuthRecord?> Refresh(string refreshToken)
	{
		// 
		var endpoint = $"/auth/refresh";
		var body = new { refreshToken };
		// Refresh
		return (await RetryWithPolicyAsync(async () =>
			await _absClient.PostAsync<AuthRecord>(endpoint, body, false)
		, 1))?.Data;
	}

	public async Task Logout(string refreshToken)
	{
		// 
		var endpoint = $"/auth/logout";
		var body = new { refreshToken };
		// Logout
		var response = await RetryWithPolicyAsync(async () =>
			await _absClient.PostAsync<object>(endpoint, body, false)
		);

		//
		authRecord = null;
		onSave?.Invoke((Constas.IoCKeys.IAuth, ""));
		onMessage?.Invoke(response?.Meta?.Message);
	}
	#endregion

	public async Task<List<CookiesRecord<T>>?> GetCookiesAsync<T>()
	{
		var endpoint = $"/api/objects/cookies";
		//
		return await RetryWithPolicyAsync(async () => {
			return (await _absClient.GetAsync<List<CookiesRecord<T>>>(endpoint))?.Data;
		});
	}

	public async Task AddCookiesAsync(
			string userId,
			string? email,
			string profileId,
			object cookies)
	{
		var endpoint = $"/api/objects/cookies";
		//const { userId, tenantId, profileId, cookies } = req.body;

		var body = new
		{
			userId,
			email,
			profileId,
			cookies
		};
		//
		_ = await RetryWithPolicyAsync(async () => {
			return (await _absClient.PutAsync<object>(endpoint, body))?.Data;
		});
	}


	public async Task DeleteCookiesAsync()
	{
		var endpoint = $"/api/objects/cookies";
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