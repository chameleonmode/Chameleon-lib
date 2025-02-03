using Chameleon.lib.Auth;
using Chameleon.lib.Const;

namespace Chameleon.lib.Abs.Platformatic;
public class PlatformaticDB {
	readonly Session session;
	readonly AbsClient absClient;

	bool ranLicenseCheck = false;

	PlatformaticDB() {
		session = Session.Instance;
		absClient = new AbsClient(Configs.Urls.ABS_PLATFORMATIC_BASE_URL, session.Authenticate);
	}

	#region Props
	//
	Task<PlatformaticUser?> GetDBuser =>
		absClient.Get<PlatformaticUser>(Configs.Endpoints.DB.USER,
			new(
				Q: $"?email={Uri.EscapeDataString(session.Login!.LoginName)}", EnsureSuccess: false
			)
		);
	public PlatformaticUser? DBuser { get; private set; }
	//
	Task<IEnumerable<PlatformaticUser>?> GetDBusers =>
		absClient.Get<IEnumerable<PlatformaticUser>>(Configs.Endpoints.Users);
	public IEnumerable<PlatformaticUser>? DBusers { get; private set; }
	// 
	public Task<PlatformaticUser?> ValidateLicese =>
		absClient.Post<PlatformaticUser>(Configs.Endpoints.LICENSE.ACTIVATE,
			new(
				Body: new { license_key = session.Login!.LicenseKey }
			)
		);
	#endregion

	//Auth
	public async Task EnsureUser() {
		DBuser ??= await GetDBuser ?? await ValidateLicese;
		ArgumentNullException.ThrowIfNull(DBuser, "User not found");
		DBusers ??= await GetDBusers;

		// Double check license key if it's null
		// TODO: Remove this after all users have migrated to auth0
		if (!ranLicenseCheck && DBuser.licenseKey == null) {
			DBuser = (await ValidateLicese) ?? DBuser;
			ranLicenseCheck = true;
		}
	}

	#region GET's
	public async Task<List<PlatformaticDataInteraction>?> GetDataInteractions() {
		await EnsureUser();
		return await absClient.Get<List<PlatformaticDataInteraction>>(Configs.Endpoints.DataInteractions);
	}
	public async Task<IEnumerable<CookyPayload<T>>?> GetCookyDataInteractions<T>() {
		var interactions = await GetDataInteractions();
		return interactions?
			.Where(i => i.dataType == "cooky")
			.Select(i => JS.DeserializeSafely<CookyPayload<T>>(i.dataPayload))
			.Where(payload => payload != null)!;
	}
	#endregion
	
	#region POST's
	public async Task CreateUser(string email) {
		await EnsureUser();

		DBusers = await absClient.Post<IEnumerable<PlatformaticUser>?>(Configs.Endpoints.DB.USER,
			new(Body: new { email })
		);
	}
	public async Task<PlatformaticDataInteraction?> SendCookies<T>(
			string receiverEmail,
			string profileId,
			IReadOnlyList<T> cookiesJs) {
		await EnsureUser();
		return await absClient.Post<PlatformaticDataInteraction>(Configs.Endpoints.DB.COOKIES,
			new(
				Body: new { receiverEmail, payload = new { profileId, cookiesJs } }
			)
		);
	}
	#endregion

	#region DELETE's
	public async Task DeleteDataInteractions() {
		var interactions = await GetDataInteractions();
		foreach (var interaction in interactions!) {
			_ = await absClient.Delete<object>($"{Configs.Endpoints.DataInteractions}/{interaction.id}");
		}
	}
	#endregion

	public static PlatformaticDB Instance { get; } = new();
}
