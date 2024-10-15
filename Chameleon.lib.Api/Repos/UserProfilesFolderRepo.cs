using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Models.Dto;

using DynamicData;

namespace Chameleon.lib.Api.Repos;

public class UserProfilesFolderRepo : ApiBase<UPFolderDto> {
	private UserProfilesFolderRepo() : base(Consts.Api.FolderEndpoint) { }

	public static Task<UPFolderDto> CreateFolder(string title) => Instance.Create(new
	{
		Title = title
	});

	/// <summary>
	/// Returns a filtered stream of cache changes preceded with the initial filtered state.
	/// </summary>
	/// <param name="predicate">The result will be filtered using the specified predicate.</param>
	/// <param name="suppressEmptyChangeSets">By default, empty change sets are not emitted. Set this value to false to emit empty change sets.</param>
	/// <returns>An observable that emits the change set.</returns>
	public static IObservable<IChangeSet<UPFolderDto, int>> Connect(Func<UPFolderDto, bool>? predicate = null, bool suppressEmptyChangeSets = true)
		=> Instance.ObservableCache.Connect(predicate, suppressEmptyChangeSets);

	public static UserProfilesFolderRepo Instance { get; } = new UserProfilesFolderRepo();
}