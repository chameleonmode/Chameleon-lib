using Chameleon.lib.Util;

namespace Chameleon.lib.Abs;

public class ABService {
	// Private
	private readonly AbsClient absClient = AbsClient.Instance;
	private readonly AbsAuth absAuth = AbsAuth.Instance;

	#region Cookies API Methods

	async void OnError(Exception ex, int i) => await absAuth.Refresh();
	public async Task<List<CookiesRecord<T>>?> GetCookies<T>()
	{
		return await PolyUtil.RetryWithPolicyAsync(
			async () => {
				return (await absClient.GetAsync<List<CookiesRecord<T>>>(
					Constas.Endpoints.Cookies)
				)?.Data;
			}, OnError);
	}

	public async Task AddCookies<T>(
			string userId,
			string? email,
			string profileId,
			IReadOnlyList<T> cookies)
	{
		_ = await PolyUtil.RetryWithPolicyAsync(async () => {
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
		}, OnError);
	}

	public async Task DeleteCookies()
	{
		_ = await PolyUtil.RetryWithPolicyAsync(async () => {
			return await absClient.DeleteAsync(Constas.Endpoints.Cookies);
		}, OnError);
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