using Chameleon.lib.Common.Models.Dto;
using Chameleon.lib.Common.Models.Interfaces;

using DynamicData;

namespace Chameleon.lib.Api.Repos;

public abstract class ApiBase<TDto> where TDto : IHasid {
	public string Endpoint { get; }

	public IObservableCache<TDto, int> ObservableCache { get; }
	public SourceCache<TDto, int> SourceCache { get; }

	public ApiBase(string endpoint)
	{
		Endpoint = endpoint;

		//var cache = new SourceCache<TDto, int>(p => p.id);
		//var cache = ObservableChangeSet.Create<TDto, int>(async list =>
		//{
		//	var items = await GetAll<TDto>();
		//	list.AddOrUpdate(items);
		//}, i => i.id);
		SourceCache = new SourceCache<TDto, int>(p => p.id);
		ObservableCache = SourceCache.AsObservableCache();
	}

	private async Task<TDto> UpdateWithCache(Func<Task<TDto>> @dto)
	{
		var o = await @dto();
		SourceCache.AddOrUpdate(o);
		return o;
	}

	public virtual async Task Load()
	{
		var response = await GetAll<TDto>();

		//_ = Task.Run(() => SourceCache.Edit(innerCache => {
		//	innerCache.Clear();
		//	innerCache.AddOrUpdate(response);
		//}));
		SourceCache.Edit(innerCache => {
			innerCache.Clear();
			innerCache.AddOrUpdate(response);
		});
	}

	public virtual async Task<T[]> GetAll<T>()
	{
		var r = await HttpApiClient.Instance.Get<Result<T>>($"{Endpoint}GetAll?MaxResultCount={int.MaxValue}", new
		{
			MaxResultCount = int.MaxValue
		});
		ArgumentNullException.ThrowIfNull(r, "Response is unreadable");
		r.items ??= [];
		return r.items;
	}

	public virtual Task<TDto> Create(object dto) => UpdateWithCache(() => HttpApiClient.Instance.Post<TDto>($"{Endpoint}Create", dto));
	public virtual Task<TDto> Put(object dto) => UpdateWithCache(() => HttpApiClient.Instance.Put<TDto>($"{Endpoint}Update", dto));

	public virtual async Task<RootResult> Delete(int id)
	{
		var o = await HttpApiClient.Instance.Delete<RootResult>($"{Endpoint}Delete?Id={id}");
		if (o.success) SourceCache.Remove(id);
		return o;
	}

	public virtual Task<T> Get<T>(string path, object? body = default)
	{
		return HttpApiClient.Instance.Get<T>($"{Endpoint}{path}", body);
	}
	public virtual Task<T> Get<T>(int id)
	{
		return HttpApiClient.Instance.Get<T>($"{Endpoint}Get?Id={id}");
	}
	protected virtual Task<RootResult> Post(string path, object? dto = default)
	{
		return HttpApiClient.Instance.Post<RootResult>($"{Endpoint}{path}", dto);
	}
	protected virtual Task<T> Post<T>(string path, object? dto = default)
	{
		return HttpApiClient.Instance.Post<T>($"{Endpoint}{path}", dto);
	}
}