using Chameleon.lib.Auth;

namespace Chameleon.lib.Abs.Platformatic;

public class DB : Web {
	public Routes.License License { get; } = new();
	public Routes.Uzer Uzer { get; } = new();
	DB() { }

	#region  Routes
	public static class Routes {
		public const string users = "/users";
		public const string dataInteractions = "/dataInteractions";
		public const string tags = "/tags";
		public const string itemTags = "/itemTags";

		public class License() : Root("license") {
			public static class Replies {
				public record Data(string License_key, string Purchase_id, int Product_id, int Status, object Guid);
				public record Status(int Valid, int Active, object Guid);
				public record Customer(bool Status, string Secret);
			}
			public Task<User?> Update => Post<User>(new($"{Prefix}/update",
				Body: new {
					license_key = Session.Instance.Login!.LicenseKey,
					email = Session.Instance.Login.LoginName
				})
			);
			public async Task Register() {
				var user = await Post<User>(new($"{Prefix}/register",
					Body: new {
						license_key = Session.Instance.Login!.LicenseKey,
						email = Session.Instance.Login.LoginName
					})
				) ?? throw new Exception("Failed to register user with license key.");
				await KickLicenseStatus();
				await KickLicenseData();
				Instance.Uzer.User = user;
			}

			public Replies.Data? Data { get; private set; }
			public async Task KickLicenseData() => Data ??= await Post<Replies.Data>(
				new($"{Prefix}/data", Body: LicenseBody)
			);

			public Replies.Status? Status { get; private set; }
			public async Task KickLicenseStatus() => Status ??= await Post<Replies.Status>(
				new($"{Prefix}/status", Body: LicenseBody)
			);

			public Replies.Customer? Customer { get; private set; }
			public async Task KickCustomer() {
				if (Instance.Uzer.User?.LicenseKey != null) return;
				Customer ??= await Post<Replies.Customer>(new($"{Prefix}/customer",
					Body: new { email = Session.Instance.Login!.LoginName })
				);
				
		if (Customer?.Status == true) await Register();
			}
			static object LicenseBody => new { license_key = Session.Instance.Login!.LicenseKey };
		}

		public class Uzer() : Root("db/user") {
			public User? User { get; internal set; }
			public async Task GetUser() => User ??= await Get<User>(new($"{Prefix}/", EnsureSuccess: false));

			public IEnumerable<User>? Users { get; internal set; }
			public async Task GetDBusers() => Users ??= await Get<IEnumerable<User>>(new($"{Prefix}/all"));
			public Task<User?> GetAnyDBuser(string email) => Get<User>(new($"{Prefix}/any",
					Q: $"?email={Uri.EscapeDataString(email)}",
					EnsureSuccess: false
				)
			);
			public Task<IEnumerable<User>?> CreateUser(string email) {
				return Post<IEnumerable<User>>(new($"{Prefix}/", Q: $"?email={Uri.EscapeDataString(email)}"));
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
				await Instance.Uzer.GetDBusers();

				return res;
			}
		}
	}
	#endregion

	#region User's
	public async Task EnsureUser() {
		if (Uzer.User != null) return;
		await Uzer.GetUser();
		await License.KickCustomer();
		await Uzer.GetDBusers();
	}
	public async Task<User?> CreateUser(string email) {
		var res = await Uzer.CreateUser(email);
		await Uzer.GetDBusers();
		return Uzer.Users?.FirstOrDefault((u) => u.Email == email);
	}
	public async Task<User?> DeleteUser(string email) {
		var id = Uzer.Users?.FirstOrDefault(u => u.Email == email)?.Id;
		if (id == null) return null;

		var user = await Delete<User>(new($"{Routes.users}/{id}"));
		await Uzer.GetDBusers();
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
				tenantId = Uzer.User!.TenantId,
				senderId = Uzer.User.UserId,
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
					tenantId = Uzer.User!.TenantId
				}
		));
	}

	public async Task<Tag?> UpdateTag(int id, string name, Dictionary<string, List<string>> items) {
		return await Put<Tag>(new($"{Routes.tags}/{id}",
				Body: new {
					name,
					items = JSON.Serialize(items),
					tenantId = Uzer.User!.TenantId
				}
		));
	}

	public async Task<Tag?> GetTag(int id) {
		return await Get<Tag>(new($"{Routes.tags}/{id}", EnsureSuccess: false));
	}

	public async Task<Tag?> GetTagBy(string name) {
		var tags = await Get<IEnumerable<Tag>>(new($"{Routes.tags}/",
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
				new($"{Routes.itemTags}/", Q: $"?offset={tags.Count}&limit=100")
			);
			if (tag == null || !tag.Any()) break;
			tags.AddRange(tag);
		} while (true);
		return tags;
	}

	public async Task<ItemTag?> CreateItemTag(string tagItemType, string tagItemId, string tagName) {
		return await Post<ItemTag>(new($"{Routes.itemTags}/",
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = Uzer.User!.TenantId
				}
		));
	}

	public async Task<ItemTag?> UpdateItemTag(string tagItemType, string tagItemId, string tagName) {
		return await Put<ItemTag>(new($"{Routes.itemTags}/",
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = Uzer.User!.TenantId
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
					tenantId = Uzer.User!.TenantId
				}
		));
	}

	public async Task<ItemTag?> UpdateItemTagBy(string tagItemType, string tagItemId, string tagName) {
		return await Put<ItemTag>(new($"{Routes.itemTags}/tagItemType/{tagItemType}/tagItemId/{tagItemId}/tag/{tagName}",
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = Uzer.User!.TenantId
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
