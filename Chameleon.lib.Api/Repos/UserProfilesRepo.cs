using Chameleon.lib.Api.Dto;
using Chameleon.lib.Util;
using DynamicData;

namespace Chameleon.lib.Api.Repos;

public class UserProfilesRepo : ApiBase<UserProfileDto> {
	public event Action<UserProfileDto>? OnProfileChanged;
	private UserProfilesRepo() : base(Consts.Api.Endpoints.Profile) { }

	public static Task<UserProfileDto[]> GetAllByUserId(long userId) => Instance.Get<UserProfileDto[]>($"GetAllByUserId?Id={userId}");

	public static Task<UserProfileDto> GetProfileById(int profileId) {
		return Instance.Get<UserProfileDto>($"Get?Id={profileId}");
	}

	public static async Task<RootResult> MoveUserProfileToFolder(IEnumerable<int> profileIds, int? foldeId) {
		var o = await Instance.Post("MoveUserProfileToFolder", new {
			ProfileIds = profileIds,
			FolderId = foldeId
		});
		if (o.success) {
			foreach (var i in Instance.SourceCache.Items.Where(p => profileIds.Contains(p.id))) {
				i.folderId = foldeId;
				Instance.SourceCache.AddOrUpdate(i);
				Instance.OnProfileChanged?.Invoke(i);
			}
		}
		return o;
	}
	public static async Task<RootResult> SetProfileIsFavorite(UserProfileDto profile) {
		var fave = !profile.isFavourite;
		var o = await Instance.Post("SetProfileIsFavorite", new {
			ProfileId = profile.id,
			IsFavorite = fave
		});
		if (o.success) {
			profile.isFavourite = fave;
			Instance.SourceCache.AddOrUpdate(profile);
		}
		return o;
	}

	public static Task<UserProfileDto> CreateProfile(string? title = null, int? folderId = null) {
		if (title.Is()) {
		  var count = Instance.ObservableCache.Items.Count;
			do {
				title = $"Profile - {++count}";
			} while (Instance.ObservableCache.Items.Any(i => i.title == title));
		}
		return Instance.Create(new {
			Title = title,
			FolderId = folderId,
		});
	}

	/// <summary>
	/// Returns a filtered stream of cache changes preceded with the initial filtered state.
	/// </summary>
	/// <param name="predicate">The result will be filtered using the specified predicate.</param>
	/// <param name="suppressEmptyChangeSets">By default, empty change sets are not emitted. Set this value to false to emit empty change sets.</param>
	/// <returns>An observable that emits the change set.</returns>
	public static IObservable<IChangeSet<UserProfileDto, int>> Connect(Func<UserProfileDto, bool>? predicate = null, bool suppressEmptyChangeSets = true)
		=> Instance.ObservableCache.Connect(predicate, suppressEmptyChangeSets);

	public static UserProfilesRepo Instance { get; } = new UserProfilesRepo();
}
