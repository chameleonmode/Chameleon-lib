using Chameleon.lib.Abs.Platformatic.Shared;
using Chameleon.lib.Auth;
using Chameleon.lib.Const;

namespace Chameleon.lib.Abs.Platformatic;
public class DB : Base {
	DB() { }

	#region Models / Dto's
	public record User(
		object Id,
		string UserId,
		string Email,
		string LicenseKey,
		string TenantId,
		string Provider,
		string ProviderId,
		DateTime CreatedAt,
		DateTime UpdatedAt
	);
	public record DataInteraction(
		object Id,
		string InteractionId,
		string TenantId,
		string SenderId,
		string ReceiverId,
		string DataType,
		string DataPayload,
		DateTime CreatedAt
	);
	public record Tag(int Id, string Name, string Items, string TenantId);
	public record ItemTag(string TagItemType, string TagItemId, string TagName, string TenantId);
	#endregion

	#region  Routes
	public static class Routes {
		public const string users = "/users";
		public const string dataInteractions = "/dataInteractions";
		public const string tags = "/tags";
		public const string itemTags = "/itemTags";

		public static class License {
			public const string prefix = "/license";
			static object LicenseBody => new { license_key = Session.Instance.Login!.LicenseKey };
			public static Task<DB.User?> Update => Post<DB.User>($"{prefix}/update",
				new(Body: new {
					license_key = Session.Instance.Login!.LicenseKey,
					email = Session.Instance.Login!.LoginName
				})
			);

			public record Data(string License_key, string Purchase_id, int Product_id, int Status, object Guid);
			public static Task<Data?> KickLicenseData => Post<Data>($"{prefix}/data",
				new(Body: LicenseBody)
			);

			public record Status(int Valid, int Active, object Guid);
			public static Task<Status?> KickLicenseStatus => Post<Status>($"{prefix}/status",
				new(Body: LicenseBody)
			);

			public record Customer(bool Status, string Secret);
			public static Task<Customer?> KickCustomer => Post<Customer>($"{prefix}/customer",
				new(Body: new { email = Session.Instance.Login!.LoginName })
			);
		}

		public static class User {
			public const string prefix = "/db/user";
			public static Task<DB.User?> GetDBuser => Get<DB.User>($"{prefix}/", new(EnsureSuccess: false));
			public static Task<IEnumerable<DB.User>?> GetDBusers => Get<IEnumerable<DB.User>>($"{prefix}/all");
			public static Task<DB.User?> GetAnyDBuser(string email) => Get<DB.User>($"{prefix}/any",
				new(
					Q: $"?email={Uri.EscapeDataString(email)}",
					EnsureSuccess: false
				)
			);
			public static Task<IEnumerable<DB.User>?> CreateUser(string email) {
				return Post<IEnumerable<DB.User>>($"{prefix}/", new(Q: $"?email={Uri.EscapeDataString(email)}"));
			}
		}

		public static class Cooky {
			public const string prefix = "/db/cooky";
			public const string DataType = "cooky";
			public record CookyPayload<T>(string ProfileId, T[] CookiesJs);
			public static async Task<IEnumerable<CookyPayload<T>>?> GetCookies<T>() =>
			 (await Get<IEnumerable<DataInteraction>?>(prefix + "/"))?
			 		.Select(i => JS.DeserializeSafely<CookyPayload<T>>(i.DataPayload))
			 		.Where(x => x != null)!;

			public static Task<DataInteraction?> SendCookies<T>(
				string email,
				string profileId,
				IReadOnlyList<T> cookiesJs
			) {
				return Post<DataInteraction>($"{prefix}/",
					new(Body: new { email, payload = new { profileId, cookiesJs } })
				);
			}
		}
	}
	#endregion

	#region Props
	public User? DBuser { get; private set; }
	public IEnumerable<User>? DBusers { get; private set; }
	public Routes.License.Status? KickLickenseStatus { get; private set; }
	public Routes.License.Customer? KickCustomer { get; private set; }
	public Routes.License.Data? KickLicenseData { get; private set; }
	#endregion

	#region User's
	public async Task EnsureUser() {
		if (DBuser != null) return;
		DBuser ??= await Routes.User.GetDBuser ?? await Routes.License.Update;
		KickCustomer ??= await Routes.License.KickCustomer;

		if (DBuser!.LicenseKey == null && KickCustomer?.Status == true) {
			DBuser = await Routes.License.Update ?? DBuser;
		}

		if (DBuser.LicenseKey != null) {
			KickLickenseStatus ??= await Routes.License.KickLicenseStatus;
			KickLicenseData ??= await Routes.License.KickLicenseData;
		}

		DBusers ??= await Routes.User.GetDBusers;
	}
	#endregion

	#region DataInteraction's
	public async Task<IEnumerable<DataInteraction>?> GetDataInteractions() {
		return await Get<IEnumerable<DataInteraction>>(Routes.dataInteractions + "/");
	}
	public async Task<IEnumerable<DataInteraction>?> GetDataInteractions(string dataType) {
		return (await GetDataInteractions())?.Where(i => i.DataType == dataType); ;
	}
	public record PostDataInteractionRequest(string ReceiverId, string DataType, object DataPayload);
	public async Task<DataInteraction?> PostDataInteraction(PostDataInteractionRequest request) {
		return await Post<DataInteraction>(Routes.dataInteractions + "/", new(
			Body: new {
				interactionId = Guid.NewGuid().ToString(),
				tenantId = DBuser!.TenantId,
				senderId = DBuser.UserId,
				receiverId = request.ReceiverId,
				dataType = request.DataType,
				dataPayload = JS.Serialize(request.DataPayload)
			}
		));
	}
	public async Task DeleteDataInteractions(string? dataType = null) {
		var interactions = await GetDataInteractions();
		if (interactions == null) return;

		interactions = dataType == null ? interactions
		: interactions.Where(i => i.DataType == dataType);

		foreach (var interaction in interactions) {
			_ = await Delete<object>($"{Routes.dataInteractions}/{interaction.Id}");
		}
	}
	#endregion

	#region Tag's
	public async Task<IEnumerable<Tag>?> GetTags() {
		var tags = new List<Tag>();
		do {
			var tag = await Get<IEnumerable<Tag>>($"{Routes.tags}/",
				new(Q: $"?offset={tags.Count}&limit=100")
			);
			if (tag == null || !tag.Any()) break;
			tags.AddRange(tag);	
		} while (true);
		return tags;
	}

	public async Task<Tag?> CreateTag(string name, Dictionary<string, List<string>> items) {
		return await Post<Tag>($"{Routes.tags}/", new(
				Body: new {
					name,
					items = JS.Serialize(items),
					tenantId = DBuser!.TenantId
				}
		));
	}

	public async Task<Tag?> UpdateTag(int id, string name, Dictionary<string, List<string>> items) {
		return await Put<Tag>($"{Routes.tags}/{id}", new(
				Body: new {
					name,
					items = JS.Serialize(items),
					tenantId = DBuser!.TenantId
				}
		));
	}

	public async Task<Tag?> GetTag(int id) {
		return await Get<Tag>($"{Routes.tags}/{id}", 
			new(EnsureSuccess: false)
		);
	}

	public async Task<Tag?> GetTagBy(string name) {
		var tags =  await Get<IEnumerable<Tag>>($"{Routes.tags}/", new(
				Q: $"?where.name.eq={Uri.EscapeDataString($"{name}")}"
		));
		return tags?.FirstOrDefault();
	}

	public async Task DeleteTag(int id) {
		_ = await Delete<object>($"{Routes.tags}/{id}");
	}

	public async Task<IEnumerable<Tag>?> SearchTags(string pattern) {
		var tags = new List<Tag>();
		do {
			var tag = await Get<IEnumerable<Tag>>($"{Routes.tags}/",
				new(Q: $"?where.name.like={Uri.EscapeDataString($"%{pattern}%")}&offset={tags.Count}&limit=100")
			);
			if (tag == null || !tag.Any()) break;
			tags.AddRange(tag);
		} while (true);
		return tags;
	}

	public async Task<IEnumerable<ItemTag>?> GetItemTagsForTag(int id) {
		return await Get<IEnumerable<ItemTag>>($"{Routes.tags}/{id}/itemTagTagName?limit=100");
	}
	#endregion

	#region ItemTag's
	public async Task<IEnumerable<ItemTag>?> GetItemTags() {
		var tags = new List<ItemTag>();
		do {
			var tag = await Get<IEnumerable<ItemTag>>($"{Routes.itemTags}/",
				new(Q: $"?offset={tags.Count}&limit=100")
			);
			if (tag == null || !tag.Any()) break;
			tags.AddRange(tag);
		} while (true);
		return tags;
	}

	public async Task<ItemTag?> CreateItemTag(string tagItemType, string tagItemId, string tagName) {
		return await Post<ItemTag>($"{Routes.itemTags}/", new(
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = DBuser!.TenantId
				}
		));
	}

	public async Task<ItemTag?> UpdateItemTag(string tagItemType, string tagItemId, string tagName) {
		return await Put<ItemTag>($"{Routes.itemTags}/", new(
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = DBuser!.TenantId
				}
		));
	}

	public async Task<ItemTag?> GetItemTagBy(string tagItemType, string tagItemId, string tagName) {
		return await Get<ItemTag>($"{Routes.itemTags}/tagItemType/{tagItemType}/tagItemId/{tagItemId}/tag/{tagName}", 
			new(EnsureSuccess: false)
		);
	}

	public async Task<ItemTag?> CreateItemTagBy(string tagItemType, string tagItemId, string tagName) {
		return await Post<ItemTag>($"{Routes.itemTags}/tagItemType/{tagItemType}/tagItemId/{tagItemId}/tag/{tagName}", new(
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = DBuser!.TenantId
				}
		));
	}

	public async Task<ItemTag?> UpdateItemTagBy(string tagItemType, string tagItemId, string tagName) {
		return await Put<ItemTag>($"{Routes.itemTags}/tagItemType/{tagItemType}/tagItemId/{tagItemId}/tag/{tagName}", new(
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = DBuser!.TenantId
				}
		));
	}

	public async Task<ItemTag?> DeleteItemTagBy(string tagItemType, string tagItemId, string tagName) {
		return await Delete<ItemTag>($"{Routes.itemTags}/tagItemType/{tagItemType}/tagItemId/{tagItemId}/tag/{tagName}");
	}

	public async Task<IEnumerable<ItemTag>?> GetItemTagsForItem(string tagItemType, string tagItemId) {
		return await Get<IEnumerable<ItemTag>>($"{Routes.itemTags}/", new(
				Q: $"?where.tagItemType.eq={Uri.EscapeDataString(tagItemType)}&where.tagItemId.eq={Uri.EscapeDataString(tagItemId)}"
		));
	}
	#endregion

	public static DB Instance { get; } = new();
}
