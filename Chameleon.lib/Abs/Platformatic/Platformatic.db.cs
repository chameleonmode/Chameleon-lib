using Chameleon.lib.Auth;
using Chameleon.lib.Const;

namespace Chameleon.lib.Abs.Platformatic;
public class PlatformaticDB {
	readonly Session session;
	readonly AbsClient absClient;

	bool ranLicenseCheck = false;

	private PlatformaticDB() {
		session = Session.Instance;
		absClient = new AbsClient(Configs.Urls.ABS_PLATFORMATIC_BASE_URL, session.Authenticate);
	}

	Task<PlatformaticUser?> GetDBuser =>
		absClient.Get<PlatformaticUser>(Configs.Endpoints.DB.USER,
			$"?email={Uri.EscapeDataString(session.Login!.LoginName)}", false);
	Task<IEnumerable<PlatformaticUser>?> GetDBusers =>
		absClient.Get<IEnumerable<PlatformaticUser>>(Configs.Endpoints.Users);
	
	public Task<PlatformaticUser?> ValidateLicese =>
		absClient.Post<PlatformaticUser>(Configs.Endpoints.LICENSE.ACTIVATE,
			new { license_key = session.Login!.LicenseKey }
		);

	public PlatformaticUser? DBuser { get; private set; }
	public List<PlatformaticUser> DBusers { get; } = [];


	public async Task EnsureUser() {
		DBuser ??= await GetDBuser;
		DBuser ??= await ValidateLicese;
		ArgumentNullException.ThrowIfNull(DBuser, "User not found");

		// Double check license key if it's null
		// TODO: Remove this after all users have migrated to auth0
		if (!ranLicenseCheck && DBuser.licenseKey == null) {
			DBuser = (await ValidateLicese) ?? DBuser;
			ranLicenseCheck = true;
		}

		if (DBusers.Count == 0) {
			var users = await GetDBusers;
			DBusers.AddRange(users!);
		}
	}

	public async Task<PlatformaticUser?> CreateUser(string email) {
		await EnsureUser();

		if (DBusers.Any(u => u.email == email))
			throw new InvalidOperationException("User already exists");

		var newUser = await absClient.Post<PlatformaticUser>(Configs.Endpoints.DB.USER, new {
			email
		});
		DBusers.Add(newUser!);

		return newUser;
	}

	// GET
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

	// POST
	public async Task<PlatformaticDataInteraction?> SendCookies<T>(
			string receiverEmail,
			string profileId,
			IReadOnlyList<T> cookiesJs) {
		await EnsureUser();
		return await absClient.Post<PlatformaticDataInteraction>(Configs.Endpoints.DB.COOKIES, new {
			receiverEmail,
			payload = new {
				profileId,
				cookiesJs
			}
		});
	}

	// DELETE
	public async Task DeleteDataInteractions() {
		var interactions = await GetDataInteractions();
		foreach (var interaction in interactions!) {
			_ = await absClient.Delete<object>($"{Configs.Endpoints.DataInteractions}/{interaction.id}");
		}
	}

	public static PlatformaticDB Instance { get; } = new();
}
