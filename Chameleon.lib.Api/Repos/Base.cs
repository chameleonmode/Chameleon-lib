using Chameleon.lib.Common.Models.Dto;
using Chameleon.lib.Common.Models.Interfaces;

using DynamicData;

namespace Chameleon.lib.Api.Repos;

public abstract class ApiBase<TDto> where TDto : IHasid {
	private readonly string _endpoint;

	public IObservableCache<TDto, int> ObservableCache { get; }
	public SourceCache<TDto, int> SourceCache { get; }

	public ApiBase(string endpoint)
	{
		_endpoint = endpoint;

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

	public async Task Load()
	{
		var response = await GetAll<TDto>();

		SourceCache.Edit(innerCache => {
			innerCache.Clear();
			innerCache.AddOrUpdate(response);
		});
	}

	protected async Task<T[]> GetAll<T>()
	{
		var r = await HttpApiClient.Instance.Get<Result<T>>($"{_endpoint}GetAll", new
		{
			MaxResultCount = int.MaxValue
		});
		ArgumentNullException.ThrowIfNull(r, "Response is unreadable");
		r.items ??= [];
		return r.items;
	}

	public virtual Task<TDto> Create(object dto) => UpdateWithCache(() => HttpApiClient.Instance.Post<TDto>($"{_endpoint}Create", dto));
	public virtual Task<TDto> Put(object dto) => UpdateWithCache(() => HttpApiClient.Instance.Put<TDto>($"{_endpoint}Update", dto));

	public virtual async Task<RootResult> Delete(int id)
	{
		var o = await HttpApiClient.Instance.Delete<RootResult>($"{_endpoint}Delete?Id={id}");
		if (o.success) SourceCache.Remove(id);
		return o;
	}

	public virtual Task<T> Get<T>(string path, object? body = default)
	{
		return HttpApiClient.Instance.Get<T>($"{_endpoint}{path}", body);
	}
	public virtual Task<T> Get<T>(int id)
	{
		return HttpApiClient.Instance.Get<T>($"{_endpoint}Get?Id={id}");
	}
	protected virtual Task<RootResult> Post(string path, object dto)
	{
		return HttpApiClient.Instance.Post<RootResult>($"{_endpoint}{path}", dto);
	}
	protected virtual Task<T> Post<T>(string path, object dto)
	{
		return HttpApiClient.Instance.Post<T>($"{_endpoint}{path}", dto);
	}
}