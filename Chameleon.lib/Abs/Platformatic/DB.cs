using Chameleon.lib.Auth;

namespace Chameleon.lib.Abs.Platformatic;
public class DB : Web {
	DB() { }

	#region  Routes
	public static class Routes {
		public const string users = "/users";
		public const string dataInteractions = "/dataInteractions";
		public const string tags = "/tags";
		public const string itemTags = "/itemTags";

		public static class License {
			public const string prefix = "/license";
			static object LicenseBody => new { license_key = Session.Instance.Login!.LicenseKey };
			public static Task<User?> Update => Post<User>(new($"{prefix}/update",
				Body: new {
					license_key = Session.Instance.Login!.LicenseKey,
					email = Session.Instance.Login!.LoginName
				})
			);

			public record Data(string License_key, string Purchase_id, int Product_id, int Status, object Guid);
			public static Task<Data?> KickLicenseData => Post<Data>(
				new($"{prefix}/data", Body: LicenseBody)
			);

			public record Status(int Valid, int Active, object Guid);
			public static Task<Status?> KickLicenseStatus => Post<Status>(
				new($"{prefix}/status", Body: LicenseBody)
			);

			public record Customer(bool Status, string Secret);
			public static Task<Customer?> KickCustomer => Post<Customer>(new($"{prefix}/customer",
				Body: new { email = Session.Instance.Login!.LoginName })
			);
		}

		public static class Uzer {
			public const string prefix = "/db/user";
			public static Task<User?> GetDBuser => Get<User>(new($"{prefix}/", EnsureSuccess: false));
			public static Task<IEnumerable<User>?> GetDBusers => Get<IEnumerable<User>>(new($"{prefix}/all"));
			public static Task<User?> GetAnyDBuser(string email) => Get<User>(new($"{prefix}/any",
					Q: $"?email={Uri.EscapeDataString(email)}",
					EnsureSuccess: false
				)
			);
			public static Task<IEnumerable<User>?> CreateUser(string email) {
				return Post<IEnumerable<User>>(new($"{prefix}/", Q: $"?email={Uri.EscapeDataString(email)}"));
			}
		}

		public static class Cooky {
			public const string prefix = "/db/cooky";
			public const string DataType = "cooky";
			public record CookyPayload<T>(string ProfileId, T[] CookiesJs);
			public static async Task<IEnumerable<CookyPayload<T>>?> GetCookies<T>() =>
			 (await Get<IEnumerable<DataInteraction>?>(new(prefix + "/")))?
			 		.Select(i => JSON.Deserialize<CookyPayload<T>>(i.DataPayload))
			 		.Where(x => x != null)!;

			public static async Task<DataInteraction?> SendCookies<T>(string email, string profileId, IReadOnlyList<T> cookiesJs) {
				var res = await Post<DataInteraction>(
					new($"{prefix}/", Body: new { email, payload = new { profileId, cookiesJs } })
				);
				if(!Instance.DBusers!.Any(u => u.Email == email))
					Instance.DBusers = await Uzer.GetDBusers;
					
				return res;
			}
		}
	}
	#endregion

	#region Props
	public User? DBuser { get; set; }
	public IEnumerable<User>? DBusers { get; private set; }
	public Routes.License.Status? KickLickenseStatus { get; private set; }
	public Routes.License.Customer? KickCustomer { get; private set; }
	public Routes.License.Data? KickLicenseData { get; private set; }
	#endregion

	#region User's
	public async Task EnsureUser() {
		if (DBuser != null) return;
		DBuser ??= await Routes.Uzer.GetDBuser ?? await Routes.License.Update;
		KickCustomer ??= await Routes.License.KickCustomer;

		if (DBuser!.LicenseKey == null && KickCustomer?.Status == true) {
			DBuser = await Routes.License.Update ?? DBuser;
		}

		if (DBuser.LicenseKey != null) {
			KickLickenseStatus ??= await Routes.License.KickLicenseStatus;
			KickLicenseData ??= await Routes.License.KickLicenseData;
		}

		DBusers ??= await Routes.Uzer.GetDBusers;
	}
	public async Task<User?> CreateUser(string email) {
		var res = await Routes.Uzer.CreateUser(email);
		DBusers = await Routes.Uzer.GetDBusers;
		return DBusers?.FirstOrDefault((u) => u.Email == email);
	}
	public async Task<User?> DeleteUser(string email) {
		var id = DBusers?.FirstOrDefault(u => u.Email == email)?.Id;
		if (id == null) return null;

		var user = await Delete<User>(new($"{Routes.users}/{id}"));
		DBusers = await Routes.Uzer.GetDBusers;
		return user;
	}
	#endregion

	#region DataInteraction's
	public async Task<IEnumerable<DataInteraction>?> GetDataInteractions() {
		return await Get<IEnumerable<DataInteraction>>(new(Routes.dataInteractions + "/"));
	}
	public async Task<IEnumerable<DataInteraction>?> GetDataInteractions(string dataType) {
		return (await GetDataInteractions())?.Where(i => i.DataType == dataType); ;
	}
	public record PostDataInteractionRequest(string ReceiverId, string DataType, object DataPayload);
	public async Task<DataInteraction?> PostDataInteraction(PostDataInteractionRequest request) {
		return await Post<DataInteraction>(new(Routes.dataInteractions + "/",
			Body: new {
				interactionId = Guid.NewGuid().ToString(),
				tenantId = DBuser!.TenantId,
				senderId = DBuser.UserId,
				receiverId = request.ReceiverId,
				dataType = request.DataType,
				dataPayload = JSON.Serialize(request.DataPayload)
			}
		));
	}
	public async Task DeleteDataInteractions(string? dataType = null) {
		var interactions = await GetDataInteractions();
		if (interactions == null) return;

		interactions = dataType == null ? interactions
		: interactions.Where(i => i.DataType == dataType);

		foreach (var interaction in interactions) {
			_ = await Delete<object>(new($"{Routes.dataInteractions}/{interaction.Id}"));
		}
	}
	#endregion

	#region Tag's
	public async Task<IEnumerable<Tag>?> GetTags() {
		var tags = new List<Tag>();
		do {
			var tag = await Get<IEnumerable<Tag>>(
				new($"{Routes.tags}/", Q: $"?offset={tags.Count}&limit=100")
			);
			if (tag == null || !tag.Any()) break;
			tags.AddRange(tag);	
		} while (true);
		return tags;
	}

	public async Task<Tag?> CreateTag(string name, Dictionary<string, List<string>> items) {
		return await Post<Tag>(new($"{Routes.tags}/", 
				Body: new {
					name,
					items = JSON.Serialize(items),
					tenantId = DBuser!.TenantId
				}
		));
	}

	public async Task<Tag?> UpdateTag(int id, string name, Dictionary<string, List<string>> items) {
		return await Put<Tag>(new($"{Routes.tags}/{id}",
				Body: new {
					name,
					items = JSON.Serialize(items),
					tenantId = DBuser!.TenantId
				}
		));
	}

	public async Task<Tag?> GetTag(int id) {
		return await Get<Tag>(new($"{Routes.tags}/{id}", EnsureSuccess: false));
	}

	public async Task<Tag?> GetTagBy(string name) {
		var tags =  await Get<IEnumerable<Tag>>( new($"{Routes.tags}/",
				Q: $"?where.name.eq={Uri.EscapeDataString($"{name}")}"
		));
		return tags?.FirstOrDefault();
	}

	public async Task DeleteTag(int id) {
		_ = await Delete<object>(new($"{Routes.tags}/{id}"));
	}

	public async Task<IEnumerable<Tag>?> SearchTags(string pattern) {
		var tags = new List<Tag>();
		do {
			var tag = await Get<IEnumerable<Tag>>(new($"{Routes.tags}/",
				Q: $"?where.name.like={Uri.EscapeDataString($"%{pattern}%")}&offset={tags.Count}&limit=100")
			);
			if (tag == null || !tag.Any()) break;
			tags.AddRange(tag);
		} while (true);
		return tags;
	}

	public async Task<IEnumerable<ItemTag>?> GetItemTagsForTag(int id) {
		return await Get<IEnumerable<ItemTag>>(new($"{Routes.tags}/{id}/itemTagTagName?limit=100"));
	}
	#endregion

	#region ItemTag's
	public async Task<IEnumerable<ItemTag>?> GetItemTags() {
		var tags = new List<ItemTag>();
		do {
			var tag = await Get<IEnumerable<ItemTag>>(
				new($"{Routes.itemTags}/",Q: $"?offset={tags.Count}&limit=100")
			);
			if (tag == null || !tag.Any()) break;
			tags.AddRange(tag);
		} while (true);
		return tags;
	}

	public async Task<ItemTag?> CreateItemTag(string tagItemType, string tagItemId, string tagName) {
		return await Post<ItemTag>( new($"{Routes.itemTags}/",
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = DBuser!.TenantId
				}
		));
	}

	public async Task<ItemTag?> UpdateItemTag(string tagItemType, string tagItemId, string tagName) {
		return await Put<ItemTag>( new($"{Routes.itemTags}/",
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = DBuser!.TenantId
				}
		));
	}

	public async Task<ItemTag?> GetItemTagBy(string tagItemType, string tagItemId, string tagName) {
		return await Get<ItemTag>(new($"{Routes.itemTags}/tagItemType/{tagItemType}/tagItemId/{tagItemId}/tag/{tagName}", 
			EnsureSuccess: false)
		);
	}

	public async Task<ItemTag?> CreateItemTagBy(string tagItemType, string tagItemId, string tagName) {
		return await Post<ItemTag>(new($"{Routes.itemTags}/tagItemType/{tagItemType}/tagItemId/{tagItemId}/tag/{tagName}", 
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = DBuser!.TenantId
				}
		));
	}

	public async Task<ItemTag?> UpdateItemTagBy(string tagItemType, string tagItemId, string tagName) {
		return await Put<ItemTag>( new($"{Routes.itemTags}/tagItemType/{tagItemType}/tagItemId/{tagItemId}/tag/{tagName}",
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = DBuser!.TenantId
				}
		));
	}

	public async Task<ItemTag?> DeleteItemTagBy(string tagItemType, string tagItemId, string tagName) {
		return await Delete<ItemTag>(new($"{Routes.itemTags}/tagItemType/{tagItemType}/tagItemId/{tagItemId}/tag/{tagName}"));
	}

	public async Task<IEnumerable<ItemTag>?> GetItemTagsForItem(string tagItemType, string tagItemId) {
		return await Get<IEnumerable<ItemTag>>(new($"{Routes.itemTags}/",
				Q: $"?where.tagItemType.eq={Uri.EscapeDataString(tagItemType)}&where.tagItemId.eq={Uri.EscapeDataString(tagItemId)}"
		));
	}
	#endregion

	public static DB Instance { get; } = new();
}
