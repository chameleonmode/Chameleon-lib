using Chameleon.lib.Api.Dto;
using Chameleon.lib.Util;

using DynamicData;

namespace Chameleon.lib.Api.Repos;

public class UserProfilesFolderRepo : ApiBase<UPFolderDto> {
	private UserProfilesFolderRepo() : base(Consts.Api.Endpoints.Folder) { }

	public static Task<UPFolderDto> CreateFolder(string? title = null) {
		if (title.Is()) {
		  var count = Instance.ObservableCache.Items.Count;
			do {
				title = $"Folder - {++count}";
			} while (Instance.ObservableCache.Items.Any(i => i.title == title));
		}
		return Instance.Create(new {
			Title = title
		});
	}

	/// <summary>
	/// Returns a filtered stream of cache changes preceded with the initial filtered state.
	/// </summary>
	/// <param name="predicate">The result will be filtered using the specified predicate.</param>
	/// <param name="suppressEmptyChangeSets">By default, empty change sets are not emitted. Set this value to false to emit empty change sets.</param>
	/// <returns>An observable that emits the change set.</returns>
	public static IObservable<IChangeSet<UPFolderDto, int>> Connect(Func<UPFolderDto, bool>? predicate = null, bool suppressEmptyChangeSets = true)
		=> Instance.ObservableCache.Connect(predicate, suppressEmptyChangeSets);

	public override async Task Load() {
		var response = await GetAll<UPFolderDto>();

		SourceCache.Edit(innerCache => {
			innerCache.Clear();
			innerCache.AddOrUpdate(new UPFolderDto());
			innerCache.AddOrUpdate(response);
		});
	}

	public static UserProfilesFolderRepo Instance { get; } = new UserProfilesFolderRepo();
}