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

	public static PlatformaticDB Instance { get; } = new();
}
