using System.Text.Json;

using Chameleon.lib.Auth;
using Chameleon.lib.Const;
using Chameleon.lib.Util;

namespace Chameleon.lib.Abs.Platformatic;
public class PlatformaticDB {
	readonly Session session = Session.Instance;
	readonly AbsClient absClient = new(Configs.Urls.ABS_PLATFORMATIC_BASE_URL);
	
	public PlatformaticDBuser? DBuser { get; private set; }

	public async Task<PlatformaticDBuser?> GetDBuser() {
		if(session.Auth0Client.Token == null) {
			await session.SignIn();
		}
		try {
			return DBuser ??= await absClient.GetAsync<PlatformaticDBuser>(Configs.Endpoints.DB + "/user");
		} catch {
			await session.ValidateLicese();
			return DBuser = await PolyUtil.RetryWithPolicyAsync(async () => {
				return await absClient.GetAsync<PlatformaticDBuser>(Configs.Endpoints.DB + "/user");
			});
		}
	}

	public async Task AddCookies<T>(
			string forUserId,
			string profileId,
			IReadOnlyList<T> cookiesJs) {
		var dbUser = await GetDBuser();
		var body = new {
			forUserId,
			fromUserId = dbUser?.userId,
			dbUser?.tenantId,
			profileId,
			cookiesJs = JsonSerializer.Serialize(cookiesJs, JS.InsensitiveCamelCaseOptions)
		};
		 var res = await absClient.PostAsync<object>(Configs.Endpoints.Cookies, body);
		//_ = await PolyUtil.RetryWithPolicyAsync(async () => {
		//	return await absClient.PutAsync<object>(
		//		Configs.Endpoints.Cookies,
		//		new {
		//			forUserId,
		//			fromUserId = dbUser?.userId,
		//			dbUser?.tenantId,
		//			profileId,
		//			cookiesJs
		//		});
		//});
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
