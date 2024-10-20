using Chameleon.lib.Common.Models.Dto;
using DynamicData;

namespace Chameleon.lib.Api.Repos;
 public class CountryzRepo<T>(string endpoint) : ApiBase<T>(endpoint) where T : UP {
	/// <summary>
	/// Returns a filtered stream of cache changes preceded with the initial filtered state.
	/// </summary>
	/// <param name="predicate">The result will be filtered using the specified predicate.</param>
	/// <param name="suppressEmptyChangeSets">By default, empty change sets are not emitted. Set this value to false to emit empty change sets.</param>
	/// <returns>An observable that emits the change set.</returns>
	public IObservable<IChangeSet<T, int>> Connect(Func<T, bool>? predicate = null, bool suppressEmptyChangeSets = true)
		=> ObservableCache.Connect(predicate, suppressEmptyChangeSets);

	public override async Task Load()
	{
		var response = await HttpApiClient.Instance.Get<T[]>($"{Endpoint}GetAll?MaxResultCount={int.MaxValue}");
		ArgumentNullException.ThrowIfNull(response, "Response is unreadable");

		SourceCache.Edit(innerCache => {
			innerCache.Clear();
			innerCache.AddOrUpdate(response!);
		});
	}
}
