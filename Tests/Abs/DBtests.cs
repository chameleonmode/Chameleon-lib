using Chameleon.lib.Abs;
using Chameleon.lib.Abs.Platformatic;

namespace Tests.Abs;

public record A : IDT {
	public int? Id { get; init; }
	public required string TenantId { get; init; }
	public required string UserId { get; init; }
	public required int TableId { get; init; }
	public required int PermissionId { get; init; }
}

public class Access {
	public class Table<T>(string prefix) : DTO<T>(prefix) where T : IDT { }
	public Table<A> Folders { get; } = new("foldersAccess");
	public Table<A> Profiles { get; } = new("profilesAccess");
	public Table<A> Addressez { get; } = new("addressezAccess");
	public Table<A> Businessez { get; } = new("businessezAccess");
	public Table<A> Personz { get; } = new("personzAccess");
	public Table<A> Loginz { get; } = new("loginzAccess");

	Access() { }
	public static Access I { get; } = new();
}

public class DB_User_Tests : TestSetup {
	const string tenantId = "b6633ec1-138f-4ec6-b9d0-71b0660c0a44"; // Example tenantId, replace with actual logic to get tenantId
	const string userId = "b6633ec1-138f-4ec6-b9d0-71b0660c0a45"; // Example userId, replace with actual logic to get userId
	readonly A aa = new() {
		TenantId = tenantId,
		TableId = 1,
		PermissionId = 1, // Example permissionId, replace with actual logic to get permissionId
		UserId = userId
	};
	public DB_User_Tests() : base(0) { }
	[Fact]
	public async Task DB_EnsureUser() {
		await DB.I.Userz.Load();
		Assert.NotNull(DB.I.Userz.Current);
		Assert.NotNull(DB.I.Userz.Users);
	}
	[Fact]
	public async Task Create_Access() {
		var a = aa with {
			TableId = 1, // Example tableId, replace with actual logic to get tableId
			PermissionId = 1, // Example permissionId, replace with actual logic to get permissionId
		};
		var folder = await Access.I.Folders.Create(a);
		Assert.NotNull(folder);
		Assert.Equal(tenantId, folder.TenantId);
		Assert.Equal(a.TableId, folder.TableId);
		Assert.Equal(a.UserId, folder.UserId);

		var profile = await Access.I.Profiles.Create(a);
		Assert.NotNull(profile);
		Assert.Equal(tenantId, profile.TenantId);
		Assert.Equal(a.TableId, profile.TableId);
		Assert.Equal(a.UserId, profile.UserId);

		var addressez = await Access.I.Addressez.Create(a);
		Assert.NotNull(addressez);
		Assert.Equal(tenantId, addressez.TenantId);
		Assert.Equal(a.TableId, addressez.TableId);
		Assert.Equal(a.UserId, addressez.UserId);
	}
}
