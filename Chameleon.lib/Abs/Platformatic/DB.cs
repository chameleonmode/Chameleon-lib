using Chameleon.lib.Auth;
using Chameleon.lib.Helpers;
using Chameleon.lib.Util;

namespace Chameleon.lib.Abs.Platformatic;

public record User(object? Id, string UserId, string Email, string? LicenseKey, string TenantId, string Provider, string? ProviderId, DateTime CreatedAt, DateTime UpdatedAt);
public record DataInteraction(int? Id, string InteractionId, string TenantId, string SenderId, string ReceiverId, string DataType, string DataPayload, DateTime CreatedAt);
public class DB : Web {
	public Routes.License License { get; } = new();
	public Routes.Uzer Userz { get; } = new();
	public Routes.Cooky Cooky { get; } = new();
	public Routes.Interactions Interactions { get; } = new();
	DB() { }

	#region  Routes
	public class Routes {
		public const string tags = "/tags";
		public const string itemTags = "/itemTags";

		public class License() : Root("license") {
			public static class Replies {
				public record Data(string License_key, string Purchase_id, int Product_id, int Status, object Guid);
				public record Status(int Valid, int Active, object Guid);
				public record Customer(bool Status, string Secret);
			}
			public static object Body => new {
				license_key = Session.I.Settings.LicenseKey,
				email = Session.I.Settings.LoginName
			};
			public Replies.Customer? Customer { get; private set; }
			public Replies.Data? Data { get; private set; }
			public Replies.Status? Status { get; private set; }
			public User? Registered { get; internal set; }
			public async Task Register() {
				Customer ??= await Post<Replies.Customer>(new($"{Prefix}/customer", Body: Body))
				?? throw new Exception($"Failed to find customer using {Body}.");
				if (!Customer.Status) return;

				Data ??= await Post<Replies.Data>(new($"{Prefix}/data", Body: Body))
				?? throw new Exception($"Failed to get license data using {Body}.");

				Status ??= await Post<Replies.Status>(new($"{Prefix}/status", Body: Body))
				?? throw new Exception($"Failed to get license status using {Body}.");

				I.Userz.Current = Registered ??= await Post<User>(new($"{Prefix}/register", Body: Body))
				?? throw new Exception($"Failed to register user using {Body}.");
			}
		}

		public class Uzer() : Root("db/uzer") {
			public static class Requests {
				public static object Users => new {
					email = Session.I.Settings.LoginName
				};
			}
			public static class Replies {
				public record Got(User Current, IEnumerable<User>? Users);
			}
			public User? Current { get; internal set; }
			public IEnumerable<User>? Users { get; internal set; }
			public async Task Load() {
				await I.License.Register();
				Current ??= await Get<User>(new($"{Prefix}/"));
				Users ??= await Get<IEnumerable<User>>(new($"{Prefix}/users", Body: Requests.Users));
			}
			public async Task<User> Create(string email) {
				return await Post<User>(new($"{Prefix}/", Body: new { email })) ?? throw new Exception("Failed to create user.");
			}
			public async Task<bool> Activate(string email) {
				var res = await Create(email);
				Users = Users?.Append(res);
				return Users?.Any(u => u.Email == email) == true;
			}
			public async Task<bool> Delete(string email) {
				var user = Users?.FirstOrDefault(u => u.Email == email);
				var rep = await Delete<User>(new($"/users/{user?.Id}"));
				Users = Users?.Where(u => u.Email != email).ToList();
				return Users?.Any(u => u.Email == email) == false; // Return true if user was deleted
			}
		}

		public class Cooky() : Root("db/cooky") {
			public static class Replies {
			public record CookyPayload<T>(string ProfileId, T[] CookiesJs);
			}
			public List<DataInteraction> Actions { get; } = [];
			public async Task<IEnumerable<Replies.CookyPayload<T>>?> GetCookies<T>() {
				var cookies = await Get<IEnumerable<DataInteraction>?>(new($"{Prefix}/"))
				?? throw new Exception("Failed to get cookies from server.");
				return cookies.Select(i => JSON.Deserialize<Replies.CookyPayload<T>>(i.DataPayload)).OfType<Replies.CookyPayload<T>>();
			}

			public async Task SendCookies<T>(int profileId, string email, IReadOnlyList<T> cookiesJs) {
				var rep = await Post<DataInteraction>(
					new($"{Prefix}/", Body: new { email, payload = new { profileId = profileId.ToString(), cookiesJs } })
				) ?? throw new Exception("Failed to send cookies to server.");
				Actions.Add(rep);
				Toaster.Success($"Cookies sent successfully ({cookiesJs.Count} cookies)");
			}

			public async Task Delete() {
				await I.Interactions.DeleteDataInteractions(Interactions.Types.cooky);
				Actions.Clear();
				Toaster.Success("Cookies cleared");
			}
		}

		public class Interactions() : Root("dataInteractions") {
			public enum Types { cooky }
			public async Task<IEnumerable<DataInteraction>> GetDataInteractions() {
				return await Get<IEnumerable<DataInteraction>>(new($"{Prefix}/")) ?? [];
			}
			public async Task<IEnumerable<DataInteraction>> GetDataInteractions(Types type) {
				return (await GetDataInteractions())?.Where(i => i.DataType == type.ToString()) ?? [];
			}
			public record PostDataInteractionRequest(string ReceiverId, string DataType, object DataPayload);
			public async Task<DataInteraction?> PostDataInteraction(PostDataInteractionRequest request) {
				return await Post<DataInteraction>(new($"{Prefix}/",
					Body: new {
						interactionId = Guid.NewGuid().ToString(),
						tenantId = I.Userz.Current!.TenantId,
						senderId = I.Userz.Current.UserId,
						receiverId = request.ReceiverId,
						dataType = request.DataType,
						dataPayload = JSON.Serialize(request.DataPayload)
					}
				));
			}
			public async Task DeleteDataInteractions(Types? type = null) {
				var interactions = await GetDataInteractions();
				if (interactions == null) return;
				await interactions.ForEach(async i => {
					if (type?.ToString() is { } dt && i.DataType != dt) return;
					_ = await Delete<object>(new($"{Prefix}/{i.Id}"));
				});
			}
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
					tenantId = Userz.Current!.TenantId
				}
		));
	}

	public async Task<Tag?> UpdateTag(int id, string name, Dictionary<string, List<string>> items) {
		return await Put<Tag>(new($"{Routes.tags}/{id}",
				Body: new {
					name,
					items = JSON.Serialize(items),
					tenantId = Userz.Current!.TenantId
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
					tenantId = Userz.Current!.TenantId
				}
		));
	}

	public async Task<ItemTag?> UpdateItemTag(string tagItemType, string tagItemId, string tagName) {
		return await Put<ItemTag>(new($"{Routes.itemTags}/",
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = Userz.Current!.TenantId
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
					tenantId = Userz.Current!.TenantId
				}
		));
	}

	public async Task<ItemTag?> UpdateItemTagBy(string tagItemType, string tagItemId, string tagName) {
		return await Put<ItemTag>(new($"{Routes.itemTags}/tagItemType/{tagItemType}/tagItemId/{tagItemId}/tag/{tagName}",
				Body: new {
					tagItemType,
					tagItemId,
					tagName,
					tenantId = Userz.Current!.TenantId
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

	public static DB I { get; } = new();
}
