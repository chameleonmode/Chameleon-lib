using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Models.Dto;

using DynamicData;

namespace Chameleon.lib.Api.Repos;

public class UserProfilesRepo : ApiBase<UserProfileDto> {
	public event Action<UserProfileDto>? OnProfileChanged;
	private UserProfilesRepo() : base(Consts.Api.Endpoints.Profile) { }

	public static Task<UserProfileDto[]> GetAllByUserId(long userId) => Instance.Get<UserProfileDto[]>($"GetAllByUserId?Id={userId}");

	public static async Task<RootResult> MoveUserProfileToFolder(List<int> profileIds, int? foldeId)
	{
		var o = await Instance.Post("MoveUserProfileToFolder", new
		{
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
	public static async Task<RootResult> SetProfileIsFavorite(int profileId, bool isFavorite)
	{
		var o = await Instance.Post("SetProfileIsFavorite", new
		{
			ProfileId = profileId,
			IsFavorite = isFavorite
		});
		if (o.success) {
			var i = Instance.SourceCache.Items.First(p => p.id == profileId);
			if (i != null) {
				i.isFavourite = isFavorite;
				Instance.SourceCache.AddOrUpdate(i);
			}
		}
		return o;
	}

	public static Task<UserProfileDto> CreateProfile(string title, int? folderId = null) => Instance.Create(new
	{
		Title = title,
		FolderId = folderId,
	});

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
