using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Models.Dto;

using DynamicData;

namespace Chameleon.lib.Api.Repos;
public class UPRepo<T>(string endpoint) : ApiBase<T>(endpoint) where T : UP {
	/// <summary>
	/// Returns a filtered stream of cache changes preceded with the initial filtered state.
	/// </summary>
	/// <param name="predicate">The result will be filtered using the specified predicate.</param>
	/// <param name="suppressEmptyChangeSets">By default, empty change sets are not emitted. Set this value to false to emit empty change sets.</param>
	/// <returns>An observable that emits the change set.</returns>
	public IObservable<IChangeSet<T, int>> Connect(Func<T, bool>? predicate = null, bool suppressEmptyChangeSets = true)
		=> ObservableCache.Connect(predicate, suppressEmptyChangeSets);
}

public class UPAdditionalDataRepo {
	private UPAdditionalDataRepo() { }

	//public CountryzRepo<CountryzDto> Countryz { get; } = new(Consts.Api.Endpoints.Country);
	public UPRepo<UPPersonDto> Personz { get; } = new(Consts.Api.Endpoints.Person);
	public UPRepo<UPBusinessDto> Biz { get; } = new(Consts.Api.Endpoints.Business);
	public UPRepo<UPAddressDto> Addrez { get; } = new(Consts.Api.Endpoints.Address);
	public UPRepo<UPLoginDto> Loginz { get; } = new(Consts.Api.Endpoints.Credentia);

	public static async Task<TT> Save<T, TT>(T repo, TT thang) 
		where T : UPRepo<TT>
		where TT : UP
	{
		if (thang.id > 0) {
			return await repo.Put(thang);
		} else {
			var res = await repo.Create(thang);
			ArgumentNullException.ThrowIfNull(res);
			res.id = thang.id;
			return res;
		}
	}
	public static async Task<RootResult?> Delete<T, TT>(T repo, TT thang)
	where T : UPRepo<TT>
	where TT : UP
	{
		return thang.id > 0 ? await repo.Delete(thang.id) : null;
	}

	public bool LoadedIniit { get; private set; }
	public async Task LoadReload(bool force = false)
	{
		if (LoadedIniit && !force)
			return;

		await Task.WhenAll([
			Personz.Load(),
			Loginz.Load(),
			Biz.Load(),
			Addrez.Load()
		]);

		LoadedIniit = true;
	}
	public static UPAdditionalDataRepo Instance { get; } = new UPAdditionalDataRepo();
}
