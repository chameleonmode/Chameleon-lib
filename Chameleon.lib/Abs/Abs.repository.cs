namespace Chameleon.lib.Abs;
public class AbsApiCookiesRepo<T> {
	// Services
	private readonly ABService abService = ABService.Instance;

	// Fields

	//Properties
	public List<CookiesRecord<T>> CookiesCache { get; } = [];
	public Task<bool> HasCookies => GetCookies();

	// Retrieves cookies from server
	public async Task<bool> GetCookies()
	{
		CookiesCache.Clear();

		try {
			var result = (await abService.GetCookies<T>())
					?? throw new InvalidOperationException("Response is unreadable");
			CookiesCache.AddRange(result);
		} catch (Exception e) {
			Console.WriteLine("Failed to get cookies " + e.Message);
		}

		return CookiesCache.Count != 0;
	}

	public async Task DeleteCookies()
	{
		await abService.DeleteCookies();
		CookiesCache.Clear();
	}

	public async Task AddCookies(string userId, string? email, string profileId, IReadOnlyList<T> cookies)
	{
		 await abService.AddCookies(userId, email, profileId, cookies);
	}
}
