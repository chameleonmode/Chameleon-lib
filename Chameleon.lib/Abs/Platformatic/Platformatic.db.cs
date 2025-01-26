using Chameleon.lib.Auth;
using Chameleon.lib.Const;

namespace Chameleon.lib.Abs.Platformatic;
public class PlatformaticDB {
	readonly Session session = Session.Instance;
	readonly AbsClient absClient = new(Configs.Urls.ABS_PLATFORMATIC_BASE_URL);
	
	public PlatformaticUser? DBuser { get; private set; }

	public async Task Ensure() {
		await session.SignIn();
		DBuser = (await GetDBuser()) ?? (await ValidateLicese());
	}

	public async Task<PlatformaticUser?> GetDBuser() {
		try {
			return await absClient.GetAsync<PlatformaticUser>(Configs.Endpoints.DB.USER);
		} catch {
			return null;
		}
	}

	public async Task<PlatformaticUser?> ValidateLicese() {
		return await absClient.PostAsync<PlatformaticUser>(
			$"{Configs.Urls.ABS_PLATFORMATIC_BASE_URL}/license/activate",
			new { license_key = session.Login!.LicenseKey }
		);
	}

	public async Task<List<PlatformaticDataInteraction>?> GetDataInteractions() {
		return await absClient.GetAsync<List<PlatformaticDataInteraction>>(Configs.Endpoints.DataInteractions);
	}

	public async Task DeleteDataInteractions() {
		var interactions = await GetDataInteractions();
		foreach (var interaction in interactions!) {
			_ = await absClient.DeleteAsync<object>($"{Configs.Endpoints.DataInteractions}/{interaction.id}");
		}
	}

	public async Task<PlatformaticDataInteraction?> SendCookies<T>(
			string receiverEmail,
			string profileId,
			IReadOnlyList<T> cookiesJs) {
		return await absClient.PostAsync<PlatformaticDataInteraction>(Configs.Endpoints.DB.COOKIES, new {
			receiverEmail,
			payload = new {
				profileId,
				cookiesJs
			}
		});
	}

	public async Task<IEnumerable<CookyPayload<T>>?> GetCookyDataInteractions<T>() {
		var interactions = await GetDataInteractions();
		return interactions?.Select(i => JS.DeserializeSafely<CookyPayload<T>>(i.dataPayload))
												.Where(payload => payload != null)!;
	}

	public static PlatformaticDB Instance { get; } = new();
}
