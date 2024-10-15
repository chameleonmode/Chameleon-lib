using System;
using System.Collections.ObjectModel;

using Chameleon.lib.Api.Repos;
using Chameleon.lib.Common.Models.Dto;
using Chameleon.lib.Common.Models.Interfaces;
using Chameleon.lib.CommunityToolkit.MvvM;

using DynamicData;
using DynamicData.Binding;

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


	public ReadOnlyObservableCollection<UserProfileVim> FProfiles { get; private set; }
	public ReadOnlyObservableCollection<FolderVim> FFolders { get; private set; }
	[Fact]
	public async Task Bind_All_Succeeds()
	{
		_ = UserProfilesRepo.Instance.ObservableCache
		.Connect()
		.Transform(i => new UserProfileVim(i))
		.Bind(out var pbd)
		.Subscribe();
		Profiles = pbd;

		_ = UserProfilesFolderRepo.Instance.ObservableCache
		.Connect()
		.Transform(i => new FolderVim(i))
		.Bind(out var fbd)
		.Subscribe();
		Folders = fbd;

		_ = UserProfilesRepo.Instance.ObservableCache
			.Connect(i => i.isFavourite)
			.Transform(i => new UserProfileVim(i))
			.Bind(out var fpbd)
			.Subscribe();
		FProfiles = fpbd;

		_ = UserProfilesFolderRepo.Instance.ObservableCache
		.Connect()
		.Filter(i => i.isFavorite)
		.Transform(i => new FolderVim(i))
		.Bind(out var ffbd)
		.Subscribe();
		FFolders = ffbd;

		await GetUserFolders_Succeeds();
		await GetUserProfiles_Succeeds();
		await Task.Delay(1000);

		var pc = Profiles.Count;
		var fc = Folders.Count;

		await GetUserFolders_Create_Succeeds();
		await GetUserProfiles_Create_Succeeds();
		await Task.Delay(1000);

		Assert.Equal(pc + 1, Profiles.Count);
		Assert.Equal(fc + 1, Folders.Count);

		Assert.NotEqual(Profiles.Count, FProfiles.Count);
		Assert.NotEqual(Folders.Count, FFolders.Count);
	}

	public ReadOnlyObservableCollection<UserProfileVim> Profiles { get; private set; }
	public ReadOnlyObservableCollection<FolderVim> Folders { get; private set; }
	[Fact]
	public async Task Sort_Succeeds()
	{
		_ = UserProfilesRepo.Instance.ObservableCache
		.Connect(i => i.isFavourite)
		.Transform(i => new UserProfileVim(i))
		.SortAndBind(out var pbd, SortExpressionComparer<UserProfileVim>.Ascending(p => p.Dto.title!))
		.Subscribe();
		Profiles = pbd;

		_ = UserProfilesFolderRepo.Instance.ObservableCache
	.Connect(i => i.isFavorite)
		.Transform(i => new FolderVim(i))
		.Bind(out var fbd)
		.Subscribe(i => {
			foreach (var item in i) {
				var reason = item.Reason;
			}
		});
		Folders = fbd;

		await GetUserFolders_Succeeds();
		await GetUserProfiles_Succeeds();
		await Task.Delay(2000);

		_ = UserProfilesRepo.Instance.ObservableCache
				.Connect()
				.Transform(i => new UserProfileVim(i))
				.SortAndBind(out var des, SortExpressionComparer<UserProfileVim>.Descending(p => p.Dto.title!))
				.Subscribe();
		var d = des;

		_ = UserProfilesFolderRepo.Instance.ObservableCache
				.Connect()
				.Transform(i => new FolderVim(i))
				.SortAndBind(out var _, SortExpressionComparer<FolderVim>.Descending(p => p.Dto.title!))
				.Subscribe();

		// Profiles and Folders are already bound, so they will update automatically

	}

	private readonly SortExpressionComparer<Dto> _descendingComparer = SortExpressionComparer<Dto>.Descending(p => p.title ?? "xxx");
	private readonly SortExpressionComparer<Dto> _ascendingComparer = SortExpressionComparer<Dto>.Ascending(p => p.title ?? "xxx");

	public class UserProfileVim(UserProfileDto dto): Vim<UserProfileDto> {
	}

	public class FolderVim(UPFolderDto dto) 
	{
		public UPFolderDto Dto { get; } = dto;
	}
}
