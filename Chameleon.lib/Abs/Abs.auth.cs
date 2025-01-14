namespace Chameleon.lib.Abs;
public class AbsAuth {
	// Private
	public const string endpoint = Abs.Constas.Endpoints.Auth;
	//
	private readonly AbsClient absClient = AbsClient.Instance;
	//
	private AuthRecord? authRecord;

	private Action<(string key, string? value)>? onSave;
	private Func<string, string?>? onLoad;
	private Func<(long userId, string username, string license_key, long? owner_id)>? credz;

	// Private constructor to enforce singleton usage
	private AbsAuth()
	{
		var load = new Func<Task<AuthRecord?>>(async () => {
			if (authRecord == null) {
				var refreshToken = onLoad?.Invoke(Constas.IoCKeys.IAuth);
				if (!string.IsNullOrEmpty(refreshToken)) {
					try {
						authRecord = await Refresh(refreshToken);
					} catch {
						authRecord = await Login();
					}
				} else {
					try {
						authRecord = await Login();
					} catch {
						authRecord = await Register();
					}
				}
				onSave?.Invoke((Constas.IoCKeys.IAuth, authRecord?.Auth?.RefreshToken));
			}

			return authRecord;
		});

		absClient.TokenProvider = async () => {
			return (await load())?.Auth;
		};
	}

	public void Use(
		Func<(long userId, string username, string license_key, long? owner_id)> credz,
		Action<(string key, string? value)> onSave,
		Func<string, string?> onLoad
		)
	{
		this.credz = credz;
		this.onSave = onSave;
		this.onLoad = onLoad;
	}

	public async Task<AuthRecord?> Register()
	{
		var (userId, email, license_key, creatorId) = credz!();
		// Register
		return (
			await absClient.PostAsync<AuthRecord>(
				$"{endpoint}/register",
				new { userId, email, license_key, creatorId }, 
				false)
		)?.Data;
	}

	public async Task<AuthRecord?> Login()
	{
		var email = credz!()!.username;
		// Login
		return (
			await absClient.PostAsync<AuthRecord>(
				$"{endpoint}/login", 
				new { email },
				false)
			)?.Data;
	}

	public async Task<AuthRecord?> Refresh(string? refreshToken = null)
	{
		try {
			refreshToken ??= onLoad?.Invoke(Constas.IoCKeys.IAuth);
			// Refresh
			return (
				await absClient.PostAsync<AuthRecord>(
					$"{endpoint}/refresh",
					new { refreshToken },
					false)
				)?.Data;
		} catch {
			return null;
		}
	}

	public async Task Logout(string refreshToken)
	{
		// Logout
		_ = await absClient.PostAsync<object>($"{endpoint}/logout", new { refreshToken }, false);
		authRecord = null;
		onSave?.Invoke((Constas.IoCKeys.IAuth, ""));
	}


	#region singleton
	private static AbsAuth? _instance;
	private static readonly object _lock = new();
	public static AbsAuth Instance {
		get {
			lock (_lock) {
				return _instance ??= new AbsAuth();
			}
		}
	}
	#endregion
}