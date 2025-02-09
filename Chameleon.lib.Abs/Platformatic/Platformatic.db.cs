using Chameleon.lib.Const;
using Chameleon.lib.Util;

namespace Chameleon.lib.Abs.Platformatic;
public class PlatformaticDB {
	readonly AbsClient absClient = new(Configs.Urls.ABS_PLATFORMATIC_BASE_URL);

	public async Task AddCookies<T>(
			string forUserId,
			string profileId,
			IReadOnlyList<T> cookies) {
		_ = await PolyUtil.RetryWithPolicyAsync(async () => {
			return await absClient.PutAsync<object>(
				Configs.Endpoints.Cookies,
				new {
					forUserId,
					profileId,
					cookies
				});
		});
	}

	//public async Task<List<CookiesRecord<T>>?> GetCookies<T>() {
	//	return await PolyUtil.RetryWithPolicyAsync(
	//		async () => {
	//			return (await absClient.GetAsync<List<CookiesRecord<T>>>(
	//				Configs.Endpoints.Cookies)
	//			)?.Data;
	//		}, OnError);
	//}

	//public async Task DeleteCookies() {
	//	_ = await PolyUtil.RetryWithPolicyAsync(async () => {
	//		return await absClient.DeleteAsync(Configs.Endpoints.Cookies);
	//	}, OnError);
	//}

	public static PlatformaticDB Instance { get; } = new();
}
