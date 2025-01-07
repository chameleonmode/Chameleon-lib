using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Chameleon.lib.Abs;

public class ABService {
	// Private
	private readonly AbsClient absClient = AbsClient.Instance;
	private readonly AbsAuth absAuth = AbsAuth.Instance;

	//
	private async Task<T> RetryWithPolicyAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
	{
		for (var i = 1; i <= maxRetries; i++) {
			try {
				return await operation();
			} catch (Exception) when (i < maxRetries) {
				await Task.Delay(256 * i); // Exponential backoff
				_ = await absAuth.Refresh();

			}
		}
		return await operation(); // Last try
	}

	#region Cookies API Methods

	public async Task<List<CookiesRecord<T>>?> GetCookies<T>()
	{
		return await RetryWithPolicyAsync(async () => {
			return (await absClient.GetAsync<List<CookiesRecord<T>>>(
				Constas.Endpoints.Cookies)
			)?.Data;
		});
	}

	public async Task AddCookies(
			string userId,
			string? email,
			string profileId,
			object cookies)
	{
		_ = await RetryWithPolicyAsync(async () => {
			return (await absClient.PutAsync<object>(
				Constas.Endpoints.Cookies,
				new
				{
					userId,
					email,
					profileId,
					cookies
				})
			)?.Data;
		});
	}

	public async Task DeleteCookies()
	{
		_ = await RetryWithPolicyAsync(async () => {
			return await absClient.DeleteAsync(Constas.Endpoints.Cookies);
		});
	}

	#endregion

	#region singleton
	private ABService() { }
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