using Chameleon.lib.Api;

using DynamicData;

namespace Chameleon.lib.Tests.Api;
public class ApiReposTests : ApiTestsBase {
	[Fact]
	public async Task GetUserProfiles_Succeeds()
	{
		await tcs.Task;
		Assert.NotNull(LoginResponse);
		await UserProfilesRepo.Instance.Load();
		Assert.NotEqual(0, UserProfilesRepo.Instance.SourceCache.Count);
	}

	[Fact]
	public async Task GetUserProfiles_SetFavorite_Succeeds()
	{
		await GetUserProfiles_Succeeds();
		var r = await UserProfilesRepo.SetProfileIsFavorite(UserProfilesRepo.Instance.ObservableCache.Items[UserProfilesRepo.Instance.ObservableCache.Items.Count - 1].id, true);
		Assert.True(r.success);
	}

	[Fact]
	public async Task GetUserProfiles_Delete_Succeeds()
	{
		await GetUserProfiles_Succeeds();
		var r = await UserProfilesRepo.Instance.Delete(UserProfilesRepo.Instance.ObservableCache.Items[UserProfilesRepo.Instance.ObservableCache.Items.Count - 1].id);
		Assert.True(r.success);
	}

	[Fact]
	public async Task GetUserProfiles_Create_Succeeds()
	{
		await GetUserProfiles_Succeeds();
		var trys = 0;
		var title = $"New Profile {UserProfilesRepo.Instance.ObservableCache.Items.Count + trys}";
		do {
			title = $"New Profile {UserProfilesRepo.Instance.ObservableCache.Items.Count + trys++}";
		} while (UserProfilesRepo.Instance.ObservableCache.Items.Any(profile => string.Equals(profile.title, title, StringComparison.InvariantCultureIgnoreCase)));
		var r = await UserProfilesRepo.CreateProfile(title);
		Assert.NotNull(r);
	}

	[Fact]
	public async Task GetUserFolders_Succeeds()
	{
		await tcs.Task;
		Assert.NotNull(LoginResponse);
		await UserProfilesFolderRepo.Instance.Load();
		Assert.NotEqual(0, UserProfilesFolderRepo.Instance.SourceCache.Count);
	}

	[Fact]
	public async Task GetUserFolders_Create_Succeeds()
	{
		await GetUserFolders_Succeeds();
		var title = $"New Folder {UserProfilesFolderRepo.Instance.SourceCache.Count + 1}";
		var r = await UserProfilesFolderRepo.CreateFolder(title);
		Assert.False(r.isFavorite);
	}
}

